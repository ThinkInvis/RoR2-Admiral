using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;
using System.Collections.Generic;
using R2API.Networking;
using UnityEngine.Networking;
using System.Linq;

namespace ThinkInvisible.Admiral {
    public class HackBeacon : T2Module<HackBeacon> {
        [AutoConfig("Lifetime of the Beacon: Special Order deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoConfig("Cooldown of Beacon: Special Order.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        [AutoConfig("Radius of the Item Ward emitted by Beacon: Special Order.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float wardRadius {get; private set;} = 10f;
        
        [AutoConfig("Items provided by Beacon: Special Order on the first stage.",
            AutoConfigFlags.None, 0, int.MaxValue)]
        public int baseItems {get; private set;} = 5;
        
        [AutoConfig("Items provided by Beacon: Special Order per stage cleared.",
            AutoConfigFlags.None, 0, int.MaxValue)]
        public int itemsPerStage {get; private set;} = 1;

        [AutoConfig("Selection weight for white items (defaults to identical to T1 chest).",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier1Chance {get; private set;} = 0.8f;

        [AutoConfig("Selection weight for green items (defaults to identical to T1 chest).",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier2Chance {get; private set;} = 0.2f;

        [AutoConfig("Selection weight for red items (defaults to identical to T1 chest).",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier3Chance {get; private set;} = 0.01f;
        
        public override string enabledConfigDescription => "Contains config for the Beacon: Hacking submodule of Modules.BeaconRebalance.";
        public override bool managedEnable => false;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        public override void SetupAttributes() {
            base.SetupAttributes();
            
            LoadoutAPI.AddSkill(typeof(EntStateCallSupplyDropSpecialOrder));
            LoadoutAPI.AddSkill(typeof(EntStateSpecialOrderMainState));

            skillFamily1 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHacking");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropSpecialOrder";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_SPECIALORDER_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_SPECIALORDER_DESCRIPTION";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallSupplyDropSpecialOrder>();
            skillDef.icon = origSkillDef.icon;

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Special Order");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. Requisition a pack of <style=cIsUtility>random, disposable items</style> from your trusty quartermaster. All allies standing near the beacon receive these items until it runs out of energy.");

            LoadoutAPI.AddSkillDef(skillDef);

            var beaconPrefabPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = LoadoutAPI.StateTypeOf<EntStateSpecialOrderMainState>();

            beaconPrefabPrefab.AddComponent<Inventory>();

            var itemWard = beaconPrefabPrefab.AddComponent<ItemWard>();
            itemWard.radius = wardRadius;
            var rngInd = beaconPrefabPrefab.transform.Find("ModelBase").Find("captain supply drop").Find("Indicator");
            rngInd.gameObject.GetComponent<ObjectScaleCurve>().enabled = false;
            rngInd.GetChild(0).localScale /= 1.5f;
            itemWard.rangeIndicator = rngInd;

            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, SpecialOrder");
            GameObject.Destroy(beaconPrefabPrefab);
        }

        public override void Install() {
            base.Install();

            skillFamily1.ReplaceVariant(origSkillDef, skillDef);
            skillFamily2.ReplaceVariant(origSkillDef, skillDef);
        }

        public override void Uninstall() {
            base.Uninstall();

            skillFamily1.ReplaceVariant(skillDef, origSkillDef);
            skillFamily2.ReplaceVariant(skillDef, origSkillDef);
        }

        public class EntStateCallSupplyDropSpecialOrder : EntityStates.Captain.Weapon.CallSupplyDropHacking {
            public override void OnEnter() {
                supplyDropPrefab = HackBeacon.instance.beaconPrefab;
                muzzleflashEffect = BeaconRebalance.instance.muzzleFlashPrefab;
                base.OnEnter();
            }
        }

        public class EntStateSpecialOrderMainState : HackingMainState {
            public override bool shouldShowEnergy => true;

            public override void OnEnter() {
                base.OnEnter();
                if(!NetworkServer.active) return;

                var itemWard = outer.gameObject.GetComponent<ItemWard>();

                WeightedSelection<List<PickupIndex>> itemSelection = new WeightedSelection<List<PickupIndex>>(8);
                itemSelection.AddChoice(Run.instance.availableTier1DropList.Where(x => !FakeInventory.blacklist.Contains(PickupCatalog.GetPickupDef(x).itemIndex)).ToList(), HackBeacon.instance.itemTier1Chance);
                itemSelection.AddChoice(Run.instance.availableTier2DropList.Where(x => !FakeInventory.blacklist.Contains(PickupCatalog.GetPickupDef(x).itemIndex)).ToList(), HackBeacon.instance.itemTier2Chance);
                itemSelection.AddChoice(Run.instance.availableTier3DropList.Where(x => !FakeInventory.blacklist.Contains(PickupCatalog.GetPickupDef(x).itemIndex)).ToList(), HackBeacon.instance.itemTier3Chance);
                for(int i = 0; i < HackBeacon.instance.baseItems + HackBeacon.instance.itemsPerStage * Run.instance.stageClearCount; i++) {
                    var list = itemSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                    var pickup = Run.instance.treasureRng.NextElementUniform(list);
                    itemWard.ServerAddItem(PickupCatalog.GetPickupDef(pickup).itemIndex);
                }
            }

            public override void FixedUpdate() {
                //base.FixedUpdate();
                fixedAge += Time.fixedDeltaTime;
            }
        }
    }
}