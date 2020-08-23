﻿using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using TILER2;
using System.Collections.Generic;
using R2API.Networking;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public class HackBeacon : BaseAdmiralSubmodule<HackBeacon> {
        [AutoItemConfig("Lifetime of the Beacon: Special Order deployable.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoItemConfig("Cooldown of Beacon: Special Order.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        [AutoItemConfig("Radius of the Item Ward emitted by Beacon: Special Order.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float wardRadius {get; private set;} = 10f;
        
        [AutoItemConfig("Items provided by Beacon: Special Order on the first stage.",
            AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int baseItems {get; private set;} = 5;
        
        [AutoItemConfig("Items provided by Beacon: Special Order per stage cleared.",
            AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int itemsPerStage {get; private set;} = 1;

        [AutoItemConfig("Selection weight for white items (defaults to identical to T1 chest).",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier1Chance {get; private set;} = 0.8f;

        [AutoItemConfig("Selection weight for green items (defaults to identical to T1 chest).",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier2Chance {get; private set;} = 0.2f;

        [AutoItemConfig("Selection weight for red items (defaults to identical to T1 chest).",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float itemTier3Chance {get; private set;} = 0.01f;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        internal override void Setup() {
            base.Setup();

            skillFamily1 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHacking");
            skillDef = TILER2.MiscUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropSpecialOrder";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_SPECIALORDER_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_SPECIALORDER_DESCRIPTION";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallSupplyDropSpecialOrder>();

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Special Order");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. Requisition a pack of <style=cIsUtility>random, disposable items</style> from your trusty quartermaster. All allies standing near the beacon receive these items until it runs out of energy.");

            LoadoutAPI.AddSkillDef(skillDef);

            var beaconPrefabPrefab = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking"));
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = LoadoutAPI.StateTypeOf<EntStateSpecialOrderMainState>();

            var inv = beaconPrefabPrefab.AddComponent<Inventory>();

            var itemWard = beaconPrefabPrefab.AddComponent<ItemWard>();
            itemWard.radius = wardRadius;
            var rngInd = beaconPrefabPrefab.transform.Find("ModelBase").Find("captain supply drop").Find("Indicator");
            rngInd.gameObject.GetComponent<ObjectScaleCurve>().enabled = false;
            itemWard.rangeIndicator = rngInd;

            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, SpecialOrder");
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

        public class EntStateCallSupplyDropSpecialOrder : EntityStates.Captain.Weapon.CallSupplyDropHacking {
            public override void OnEnter() {
                supplyDropPrefab = HackBeacon.instance.beaconPrefab;
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
                itemSelection.AddChoice(Run.instance.availableTier1DropList, HackBeacon.instance.itemTier1Chance);
                itemSelection.AddChoice(Run.instance.availableTier2DropList, HackBeacon.instance.itemTier2Chance);
                itemSelection.AddChoice(Run.instance.availableTier3DropList, HackBeacon.instance.itemTier3Chance);
                for(int i = 0; i < HackBeacon.instance.baseItems + HackBeacon.instance.itemsPerStage * Run.instance.stageClearCount; i++) {
                    var list = itemSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                    var pickup = Run.instance.treasureRng.NextElementUniform(list);
                    itemWard.ServerAddItem(PickupCatalog.GetPickupDef(pickup).itemIndex);
                }
            }

            public override void FixedUpdate() {
                base.FixedUpdate();
                if(Util.HasEffectiveAuthority(outer.networkIdentity)) scanTimer = 999f;
            }
        }
    }
}