using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShockBeacon : BaseAdmiralSubmodule<ShockBeacon> {
        [AutoItemConfig("Lifetime of the Beacon: Shocking deployable.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 8f;

        [AutoItemConfig("Cooldown of Beacon: Shocking.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 24f;
        
        [AutoItemConfig("Fire rate of Beacon: Shocking.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float shockRate {get; private set;} = 0.95f;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        internal override void Setup() {
            base.Setup();
            skillFamily1 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropShocking");
            skillDef = MiscUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropShocking";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_SHOCKING_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_SHOCKING_DESCRIPTION";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallSupplyDropShocking>();
            skillDef.icon = origSkillDef.icon;

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Shocking");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsDamage>Shock</style> all nearby enemies rapidly for a short time.");

            LoadoutAPI.AddSkillDef(skillDef);

            var beaconPrefabPrefab = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking"));
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = LoadoutAPI.StateTypeOf<EntStateShockingMainState>();
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Shocking");
            GameObject.Destroy(beaconPrefabPrefab);
        }

        internal override void Install() {
            base.Install();
            skillFamily1.OverrideVariant(origSkillDef, skillDef);
            skillFamily2.OverrideVariant(origSkillDef, skillDef);
        }

        internal override void Uninstall() {
            base.Uninstall();
            skillFamily1.OverrideVariant(skillDef, origSkillDef);
            skillFamily2.OverrideVariant(skillDef, origSkillDef);
        }

        public class EntStateCallSupplyDropShocking : EntityStates.Captain.Weapon.CallSupplyDropShocking {
            public override void OnEnter() {
                supplyDropPrefab = ShockBeacon.instance.beaconPrefab;
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
