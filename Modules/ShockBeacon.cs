using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShockBeacon : T2Module<ShockBeacon> {
        [AutoConfig("Lifetime of the Beacon: Shocking deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 8f;

        [AutoConfig("Cooldown of Beacon: Shocking.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 24f;
        
        [AutoConfig("Fire rate of Beacon: Shocking.",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float shockRate {get; private set;} = 0.95f;
        
        public override string enabledConfigDescription => "Contains config for the Beacon: Shocking submodule of Modules.BeaconRebalance.";
        public override bool managedEnable => false;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        public override void SetupAttributes() {
            base.SetupAttributes();
            
            var callDropEntState = ContentAddition.AddEntityState<EntStateCallSupplyDropShocking>(out _);
            var mainEntState = ContentAddition.AddEntityState<EntStateShockingMainState>(out _);

            skillFamily1 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = LegacyResourcesAPI.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropShocking");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropShocking";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_SHOCKING_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_SHOCKING_DESCRIPTION";
            skillDef.activationState = callDropEntState;

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Shocking");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsDamage>Shock</style> all nearby enemies rapidly for a short time.");

            ContentAddition.AddSkillDef(skillDef);

            var beaconPrefabPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = mainEntState;
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Shocking", true);
            GameObject.Destroy(beaconPrefabPrefab);
        }

        public override void Install() {
            base.Install();
            if(BeaconRebalance.instance.removeOriginals) {
                skillFamily1.ReplaceVariant(origSkillDef, skillDef);
                skillFamily2.ReplaceVariant(origSkillDef, skillDef);
            } else {
                skillFamily1.AddVariant(skillDef);
                skillFamily2.AddVariant(skillDef);
            }
        }

        public override void Uninstall() {
            base.Uninstall();
            if(BeaconRebalance.instance.removeOriginals) {
                skillFamily1.ReplaceVariant(skillDef, origSkillDef);
                skillFamily2.ReplaceVariant(skillDef, origSkillDef);
            } else {
                skillFamily1.RemoveVariant(skillDef);
                skillFamily2.RemoveVariant(skillDef);
            }
        }

        public class EntStateCallSupplyDropShocking : EntityStates.Captain.Weapon.CallSupplyDropShocking {
            public override void OnEnter() {
                supplyDropPrefab = ShockBeacon.instance.beaconPrefab;
                muzzleflashEffect = BeaconRebalance.instance.muzzleFlashPrefab;
                base.OnEnter();
            }
        }

        public class EntStateShockingMainState : ShockZoneMainState {
		    public override void FixedUpdate() {
			    fixedAge += Time.fixedDeltaTime;
			    shockTimer += Time.fixedDeltaTime;
                if(shockTimer > ShockBeacon.instance.shockRate) {
                    shockTimer -= ShockBeacon.instance.shockRate;
                    Shock();
                }
		    }

            public override bool shouldShowEnergy => true;
        }
    }
}
