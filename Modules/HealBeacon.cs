using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class HealBeacon : AdmiralModule<HealBeacon> {
        [AutoItemConfig("Lifetime of the Beacon: Healing deployable.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoItemConfig("Cooldown of Beacon: Healing.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        public override string configDescription => "Contains config for the Beacon: Healing submodule of Modules.BeaconRebalance.";
        public override bool managedEnable => false;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        internal override void Setup() {
            base.Setup();

            LoadoutAPI.AddSkill(typeof(EntStateCallSupplyDropHealing));
            LoadoutAPI.AddSkill(typeof(EntStateHealingMainState));

            skillFamily1 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHealing");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropHealing";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_HEALING_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_HEALING_DESCRIPTION";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallSupplyDropHealing>();

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Healing");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsHealing>Heal</style> all nearby allies for <style=cIsHealing>10%</style> of their <style=cIsHealing>maximum health</style> every second.");

            LoadoutAPI.AddSkillDef(skillDef);

            var beaconPrefabPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Healing").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = LoadoutAPI.StateTypeOf<EntStateHealingMainState>();
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Healing");
            GameObject.Destroy(beaconPrefabPrefab);
        }

        internal override void Install() {
            base.Install();

            skillFamily1.ReplaceVariant(origSkillDef, skillDef);
            skillFamily2.ReplaceVariant(origSkillDef, skillDef);
        }

        internal override void Uninstall() {
            base.Uninstall();

            skillFamily1.ReplaceVariant(skillDef, origSkillDef);
            skillFamily2.ReplaceVariant(skillDef, origSkillDef);
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