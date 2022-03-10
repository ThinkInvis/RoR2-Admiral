using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using UnityEngine.Networking;
using TILER2;
using static TILER2.SkillUtil;

namespace ThinkInvisible.Admiral {
    public class StasisBeacon : T2Module<StasisBeacon> {
        [AutoConfig("Lifetime of the Beacon: Stasis deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 15f;

        [AutoConfig("Cooldown of Beacon: Stasis.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        public override string enabledConfigDescription => "Contains config for the Beacon: Stasis submodule of Modules.BeaconRebalance.";
        public override bool managedEnable => false;

        private const float _STASIS_INTERVAL = 2f;

        private GameObject stasisWardPrefab;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;
        public BuffDef stasisDebuff {get; private set;}

        public override void SetupAttributes() {
            base.SetupAttributes();

            var callDropState = ContentAddition.AddEntityState<EntStateCallSupplyDropStasis>(out _);
            var mainState = ContentAddition.AddEntityState<EntStateStasisMainState>(out _);
            skillFamily1 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            var origSkillDef = LegacyResourcesAPI.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropEquipmentRestock");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropStasis";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_STASIS_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_STASIS_DESCRIPTION";
            skillDef.icon = AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralHeavyWeaponDebuff.png");
            skillDef.activationState = callDropState;

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Stasis");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsUtility>Time-freeze</style> all nearby enemies, preventing them from <style=cIsUtility>moving, dealing damage, or taking damage</style>.");

            ContentAddition.AddSkillDef(skillDef);

            stasisDebuff = ScriptableObject.CreateInstance<BuffDef>();
            stasisDebuff.name = "Stasis";
            stasisDebuff.iconSprite = AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralHeavyWeaponDebuff.png");
            stasisDebuff.buffColor = Color.red;
            stasisDebuff.canStack = false;
            stasisDebuff.isDebuff = true;
            ContentAddition.AddBuffDef(stasisDebuff);

            //need to InstantiateClone because letting the prefabprefab wake up breaks some effects (animation curve components)
            var beaconPrefabPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<ProxyInteraction>().enabled = false;
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = mainState;
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Stasis", true);
            GameObject.Destroy(beaconPrefabPrefab);

            //Cobble together an indicator ring from the healing ward prefab
            var chwPrefab = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard"));
            chwPrefab.GetComponent<HealingWard>().enabled = false;
            var indic = chwPrefab.transform.Find("Indicator");
            var wardDecayer = chwPrefab.AddComponent<CaptainBeaconDecayer>();
            wardDecayer.lifetime = skillLifetime - CaptainBeaconDecayer.lifetimeDropAdjust; //ward appears after drop
            wardDecayer.silent = true;
            var eqprestWard = chwPrefab.AddComponent<BuffWard>();
            eqprestWard.invertTeamFilter = true;
            eqprestWard.buffDef = stasisDebuff;
            eqprestWard.buffDuration = _STASIS_INTERVAL;
            eqprestWard.radius = 15f;
            eqprestWard.interval = _STASIS_INTERVAL;
            eqprestWard.rangeIndicator = indic;

            indic.Find("IndicatorRing").GetComponent<MeshRenderer>().material.SetColor("_TintColor", new Color(0.5f, 0.5f, 1f, 1f));
            var chwHsPsRen = indic.Find("HealingSymbols").GetComponent<ParticleSystemRenderer>();
            chwHsPsRen.material.SetTexture("_MainTex", AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralHeavyWeaponDebuff.png").texture);
            chwHsPsRen.material.SetColor("_TintColor", new Color(0.05f, 0.05f, 2f, 1f));
            chwHsPsRen.trailMaterial.SetColor("_TintColor", new Color(0.05f, 0.05f, 2f, 1f));

            var chwFlashPsMain = indic.Find("Flashes").GetComponent<ParticleSystem>().main;
            chwFlashPsMain.startColor = new Color(0.25f, 0.25f, 0.5f, 1f);
            
            stasisWardPrefab = chwPrefab.InstantiateClone("CaptainStasisWard", true);
            GameObject.Destroy(chwPrefab);
        }

        public override void Install() {
            base.Install();
            skillFamily1.AddVariant(skillDef);
            skillFamily2.AddVariant(skillDef);
            On.RoR2.CharacterBody.AddBuff_BuffIndex += CharacterBody_AddBuff_BuffIndex;
            On.EntityStates.FrozenState.FixedUpdate += FrozenState_FixedUpdate;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.EntityStates.FrozenState.OnEnter += FrozenState_OnEnter;
        }

        public override void Uninstall() {
            base.Uninstall();
            skillFamily1.RemoveVariant(skillDef);
            skillFamily2.RemoveVariant(skillDef);
            On.RoR2.CharacterBody.AddBuff_BuffIndex -= CharacterBody_AddBuff_BuffIndex;
            On.EntityStates.FrozenState.FixedUpdate -= FrozenState_FixedUpdate;
            On.RoR2.HealthComponent.TakeDamage -= HealthComponent_TakeDamage;
        }

        private static void CharacterBody_AddBuff_BuffIndex(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType) {
            if(buffType == StasisBeacon.instance.stasisDebuff.buffIndex) {
                var sSOHComponent = self.GetComponent<SetStateOnHurt>();
                if(sSOHComponent) {
                    if(!sSOHComponent.canBeFrozen) return;
                    sSOHComponent.SetFrozen(_STASIS_INTERVAL);
                } else return;
            }
            orig(self, buffType);
        }

        private static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
            if(self.body != null && self.body.HasBuff(StasisBeacon.instance.stasisDebuff))
                return;
            orig(self, damageInfo);
        }

        private void FrozenState_OnEnter(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self) {
            if(!self.characterBody || !self.characterBody.HasBuff(StasisBeacon.instance.stasisDebuff) || !self.sfxLocator) {
                orig(self);
                return;
            }

            string origBark = self.sfxLocator.barkSound;
            self.sfxLocator.barkSound = "";
            orig(self);
            self.sfxLocator.barkSound = origBark;
        }

        private static void FrozenState_FixedUpdate(On.EntityStates.FrozenState.orig_FixedUpdate orig, EntityStates.FrozenState self) {
            orig(self);
            //continuously zero velocity during timefreeze; normal frozen state only zeroes at start
            if(self.characterBody && self.characterBody.HasBuff(StasisBeacon.instance.stasisDebuff)) {
                if(self.rigidbody && !self.rigidbody.isKinematic) {
                    self.rigidbody.velocity = Vector3.zero;
                    if(self.rigidbodyMotor) {
                        self.rigidbodyMotor.moveVector = Vector3.zero;
                    }
                }
                if(self.characterMotor)
                    self.characterMotor.velocity = Vector3.zero;
            }
        }

        public class EntStateCallSupplyDropStasis : EntityStates.Captain.Weapon.CallSupplyDropEquipmentRestock {
            public override void OnEnter() {
                supplyDropPrefab = StasisBeacon.instance.beaconPrefab;
                muzzleflashEffect = BeaconRebalance.instance.muzzleFlashPrefab;
                base.OnEnter();
            }
        }

        public class EntStateStasisMainState : EquipmentRestockMainState {
            public override void OnEnter() {
                base.OnEnter();
                if(!NetworkServer.active) return;
			    var buffZoneInstance = UnityEngine.Object.Instantiate<GameObject>(StasisBeacon.instance.stasisWardPrefab, outer.commonComponents.transform.position, outer.commonComponents.transform.rotation);
			    buffZoneInstance.GetComponent<TeamFilter>().teamIndex = teamFilter.teamIndex;
			    NetworkServer.Spawn(buffZoneInstance);
            }

            public override Interactability GetInteractability(Interactor activator) {
                return Interactability.Disabled;
            }

            public override bool shouldShowEnergy => true;
        }
    }
}
