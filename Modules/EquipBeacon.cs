﻿using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using UnityEngine.Networking;
using TILER2;
using static TILER2.SkillUtil;

namespace ThinkInvisible.Admiral {
    public class EquipBeacon : T2Module<EquipBeacon> {
        [AutoConfigRoOSlider("{0:N0} s", 0f, 120f)]
        [AutoConfig("Lifetime of the T.Beacon: Rejuvenator deployable and buff.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoConfigRoOSlider("{0:N0} s", 0f, 120f)]
        [AutoConfig("Cooldown of T.Beacon: Rejuvenator.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 50f;

        [AutoConfigRoOSlider("{0:P0}", 0f, 5f)]
        [AutoConfig("Additional fraction of skill recharge rate to provide from the Stimmed buff.",
            AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float rechargeRate {get; private set;} = 0.5f;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If true, buff is granted by interacting with the beacon and consuming a once-per-player charge. If false, buff is granted continuously in an area.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch)]
        public bool useInteractable { get; private set; } = true;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If true and UseInteractable is true, only 3 charges will be provided for all players to share. If false, the beacon will have unlimited charges instead, but players who already have the buff will still not be able to stack or renew it (effectively limiting uses to once per player per beacon cooldown).",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch)]
        public bool interactableLimited { get; private set; } = false;

        public override string enabledConfigDescription => "Contains config for the T.Beacon: Resupply submodule of Modules.BeaconRebalance. Replaces Beacon: Equipment.";
        public override bool managedEnable => false;

        private GameObject rejuvWardPrefab;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;
        public BuffDef stimmedBuff {get; private set;}

        public override void SetupAttributes() {
            base.SetupAttributes();

            var callSupplyDropRejuvenatorState = ContentAddition.AddEntityState<EntStateCallSupplyDropRejuvenator>(out _);
            var rejuvenatorMainState = ContentAddition.AddEntityState<EntStateRejuvenatorMainState>(out _);
            skillFamily1 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = LegacyResourcesAPI.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropEquipmentRestock");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropRejuvenator";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_REJUVENATOR_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_REJUVENATOR_DESCRIPTION";
            skillDef.activationState = callSupplyDropRejuvenatorState;

            LanguageAPI.Add(skillDef.skillNameToken, "T.Beacon: Rejuvenator");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsUtility>Buff</style> all nearby allies with <style=cIsUtility>+50% skill recharge rate</style>.");

            ContentAddition.AddSkillDef(skillDef);

            stimmedBuff = ScriptableObject.CreateInstance<BuffDef>();
            stimmedBuff.name = "Stimmed";
            stimmedBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/itemicons/texSyringeIcon");
            stimmedBuff.buffColor = Color.red;
            stimmedBuff.canStack = false;
            stimmedBuff.isDebuff = false;
            ContentAddition.AddBuffDef(stimmedBuff);

            LanguageAPI.Add("ADMIRAL_SUPPLY_REJUVENATOR_CONTEXT", "Take stim charge");

            //need to InstantiateClone because letting the prefabprefab wake up breaks some effects (animation curve components)
            var beaconPrefabPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);

            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;

            if(!useInteractable) {
                beaconPrefabPrefab.GetComponent<ProxyInteraction>().enabled = false;
                beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            }

            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = rejuvenatorMainState;

            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Rejuvenator", true);
            GameObject.Destroy(beaconPrefabPrefab);

            //Cobble together an indicator ring from the healing ward prefab
            var chwPrefab = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard"));
            chwPrefab.GetComponent<HealingWard>().enabled = false;
            var indic = chwPrefab.transform.Find("Indicator");
            var wardDecayer = chwPrefab.AddComponent<CaptainBeaconDecayer>();
            wardDecayer.lifetime = skillLifetime - CaptainBeaconDecayer.lifetimeDropAdjust; //ward appears after drop
            wardDecayer.silent = true;
            var eqprestWard = chwPrefab.AddComponent<BuffWard>();
            eqprestWard.buffDef = stimmedBuff;
            eqprestWard.buffDuration = 1f;
            eqprestWard.radius = 10f;
            eqprestWard.interval = 1f;
            eqprestWard.rangeIndicator = indic;

            indic.Find("IndicatorRing").GetComponent<MeshRenderer>().material.SetColor("_TintColor", new Color(1f, 0.5f, 0f, 1f));
            var chwHsPsRen = indic.Find("HealingSymbols").GetComponent<ParticleSystemRenderer>();
            chwHsPsRen.material.SetTexture("_MainTex", LegacyResourcesAPI.Load<Texture>("textures/bufficons/texBuffTeslaIcon"));
            chwHsPsRen.material.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));
            chwHsPsRen.trailMaterial.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));

            var chwFlashPsMain = indic.Find("Flashes").GetComponent<ParticleSystem>().main;
            chwFlashPsMain.startColor = new Color(0.5f, 0.25f, 0f, 1f);
            
            rejuvWardPrefab = chwPrefab.InstantiateClone("CaptainRejuvWard", true);
            GameObject.Destroy(chwPrefab);
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
            On.RoR2.Skills.SkillDef.OnFixedUpdate += On_SkillDefFixedUpdate;
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
            On.RoR2.Skills.SkillDef.OnFixedUpdate -= On_SkillDefFixedUpdate;
        }

        private void On_SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot) {
            if(skillSlot.characterBody.HasBuff(stimmedBuff))
                skillSlot.RunRecharge(Time.fixedDeltaTime * rechargeRate);
            orig(self, skillSlot);
        }

        public class EntStateCallSupplyDropRejuvenator : EntityStates.Captain.Weapon.CallSupplyDropEquipmentRestock {
            public override void OnEnter() {
                supplyDropPrefab = EquipBeacon.instance.beaconPrefab;
                muzzleflashEffect = BeaconRebalance.instance.muzzleFlashPrefab;
                base.OnEnter();
            }
        }

        public class EntStateRejuvenatorMainState : EquipmentRestockMainState {
            public override void OnEnter() {
                base.OnEnter();
                if(!NetworkServer.active) return;
                if(!EquipBeacon.instance.useInteractable) {
			        var buffZoneInstance = UnityEngine.Object.Instantiate<GameObject>(EquipBeacon.instance.rejuvWardPrefab, outer.commonComponents.transform.position, outer.commonComponents.transform.rotation);
			        buffZoneInstance.GetComponent<TeamFilter>().teamIndex = teamFilter.teamIndex;
			        NetworkServer.Spawn(buffZoneInstance);
                }
            }

            public override void OnInteractionBegin(Interactor activator) {
                if(!activator) return;
                var abody = activator.GetComponent<CharacterBody>();
                if(!abody || abody.HasBuff(EquipBeacon.instance.stimmedBuff)) return;
                if(EquipBeacon.instance.interactableLimited) energyComponent.TakeEnergy(activationCost);
                abody.AddTimedBuff(EquipBeacon.instance.stimmedBuff, EquipBeacon.instance.skillLifetime);
            }

            public override Interactability GetInteractability(Interactor activator) {
                if(!EquipBeacon.instance.useInteractable)
                    return Interactability.Disabled;
                if(!activator) return Interactability.Disabled;
                var abody = activator.GetComponent<CharacterBody>();
                if(!abody) return Interactability.Disabled;
                if(EquipBeacon.instance.interactableLimited && activationCost >= energyComponent.energy)
                    return Interactability.ConditionsNotMet;
                if(abody.HasBuff(EquipBeacon.instance.stimmedBuff))
                    return Interactability.ConditionsNotMet;
                return Interactability.Available;
            }

            public override string GetContextString(Interactor activator) {
                return Language.GetString("ADMIRAL_SUPPLY_REJUVENATOR_CONTEXT");
            }

            public override bool shouldShowEnergy => EquipBeacon.instance.useInteractable && EquipBeacon.instance.interactableLimited;
        }
    }
}
