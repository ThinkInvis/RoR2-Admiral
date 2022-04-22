using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class HealBeacon : T2Module<HealBeacon> {
        [AutoConfigRoOSlider("{0:N0} s", 0f, 120f)]
        [AutoConfig("Lifetime of the T.Beacon: Healing deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoConfigRoOSlider("{0:N0} s", 0f, 120f)]
        [AutoConfig("Cooldown of T.Beacon: Healing.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        public override string enabledConfigDescription => "Contains config for the T.Beacon: Healing submodule of Modules.BeaconRebalance. Replaces Beacon: Healing.";
        public override bool managedEnable => false;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        public override void SetupAttributes() {
            base.SetupAttributes();

            var callDropEntState = ContentAddition.AddEntityState<EntStateCallSupplyDropHealing>(out _);
            var mainEntState = ContentAddition.AddEntityState<EntStateHealingMainState>(out _);

            skillFamily1 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = LegacyResourcesAPI.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHealing");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropHealing";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_HEALING_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_HEALING_DESCRIPTION";
            skillDef.activationState = callDropEntState;

            LanguageAPI.Add(skillDef.skillNameToken, "T.Beacon: Healing");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsHealing>Heal</style> all nearby allies for <style=cIsHealing>10%</style> of their <style=cIsHealing>maximum health</style> every second.");

            ContentAddition.AddSkillDef(skillDef);

            var beaconPrefabPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Healing").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = mainEntState;
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Healing", true);
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

        public class EntStateCallSupplyDropHealing : EntityStates.Captain.Weapon.CallSupplyDropHealing {
            public override void OnEnter() {
                supplyDropPrefab = HealBeacon.instance.beaconPrefab;
                muzzleflashEffect = BeaconRebalance.instance.muzzleFlashPrefab;
                base.OnEnter();
            }
        }

        public class EntStateHealingMainState : HealZoneMainState {
            public override bool shouldShowEnergy => true;
        }
    }
}