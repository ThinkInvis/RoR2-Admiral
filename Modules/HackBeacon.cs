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
        [AutoConfig("Lifetime of the T.Beacon: Special Order deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoConfig("Cooldown of T.Beacon: Special Order.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 40f;

        [AutoConfig("Radius of the Item Ward emitted by T.Beacon: Special Order.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float wardRadius {get; private set;} = 10f;
        
        [AutoConfig("Items provided by T.Beacon: Special Order on the first stage.",
            AutoConfigFlags.None, 0, int.MaxValue)]
        public int baseItems {get; private set;} = 5;
        
        [AutoConfig("Items provided by T.Beacon: Special Order per stage cleared.",
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

        [AutoConfig("If true, items will be evenly split between the 3 categories (Damage, Healing, Utility & Uncategorized).",
            AutoConfigFlags.None)]
        public bool splitDHU { get; private set; } = true;

        public override string enabledConfigDescription => "Contains config for the T.Beacon: Special Order submodule of Modules.BeaconRebalance. Replaces Beacon: Hacking.";
        public override bool managedEnable => false;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;

        public override void SetupAttributes() {
            base.SetupAttributes();
            
            var callDropEntState = ContentAddition.AddEntityState<EntStateCallSupplyDropSpecialOrder>(out _);
            var mainEntState = ContentAddition.AddEntityState<EntStateSpecialOrderMainState>(out _);

            skillFamily1 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = LegacyResourcesAPI.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHacking");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropSpecialOrder";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_SPECIALORDER_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_SPECIALORDER_DESCRIPTION";
            skillDef.activationState = callDropEntState;
            skillDef.icon = origSkillDef.icon;

            LanguageAPI.Add(skillDef.skillNameToken, "T.Beacon: Special Order");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. Requisition a pack of <style=cIsUtility>random, disposable items</style> from your trusty quartermaster. All allies standing near the beacon receive these items until it runs out of energy.");

            ContentAddition.AddSkillDef(skillDef);

            var beaconPrefabPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking").InstantiateClone("TempSetup, BeaconPrefabPrefab", false);
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = mainEntState;

            beaconPrefabPrefab.AddComponent<Inventory>();

            var itemWard = beaconPrefabPrefab.AddComponent<ItemWard>();
            itemWard.radius = wardRadius;
            var rngInd = beaconPrefabPrefab.transform.Find("ModelBase").Find("captain supply drop").Find("Indicator");
            rngInd.gameObject.GetComponent<ObjectScaleCurve>().enabled = false;
            rngInd.GetChild(0).localScale /= 1.5f;
            itemWard.rangeIndicator = rngInd;

            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, SpecialOrder", true);
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

                var tier1Items = Run.instance.availableTier1DropList
                    .Select(x => ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x).itemIndex))
                    .Where(x => !FakeInventory.blacklist.Contains(x))
                    .ToList();
                var tier2Items = Run.instance.availableTier2DropList
                    .Select(x => ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x).itemIndex))
                    .Where(x => !FakeInventory.blacklist.Contains(x))
                    .ToList();
                var tier3Items = Run.instance.availableTier3DropList
                    .Select(x => ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x).itemIndex))
                    .Where(x => !FakeInventory.blacklist.Contains(x))
                    .ToList();

                if(HackBeacon.instance.splitDHU) {
                    var itemSelectionD = new WeightedSelection<List<ItemDef>>(8);
                    itemSelectionD.AddChoice(tier1Items.Where(x => x.ContainsTag(ItemTag.Damage)).ToList(), HackBeacon.instance.itemTier1Chance);
                    itemSelectionD.AddChoice(tier2Items.Where(x => x.ContainsTag(ItemTag.Damage)).ToList(), HackBeacon.instance.itemTier2Chance);
                    itemSelectionD.AddChoice(tier3Items.Where(x => x.ContainsTag(ItemTag.Damage)).ToList(), HackBeacon.instance.itemTier3Chance);

                    var itemSelectionH = new WeightedSelection<List<ItemDef>>(8);
                    itemSelectionH.AddChoice(tier1Items.Where(x => x.ContainsTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier1Chance);
                    itemSelectionH.AddChoice(tier2Items.Where(x => x.ContainsTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier2Chance);
                    itemSelectionH.AddChoice(tier3Items.Where(x => x.ContainsTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier3Chance);

                    var itemSelectionU = new WeightedSelection<List<ItemDef>>(8);
                    itemSelectionU.AddChoice(tier1Items.Where(x => x.DoesNotContainTag(ItemTag.Damage) && x.DoesNotContainTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier1Chance);
                    itemSelectionU.AddChoice(tier2Items.Where(x => x.DoesNotContainTag(ItemTag.Damage) && x.DoesNotContainTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier2Chance);
                    itemSelectionU.AddChoice(tier3Items.Where(x => x.DoesNotContainTag(ItemTag.Damage) && x.DoesNotContainTag(ItemTag.Healing)).ToList(), HackBeacon.instance.itemTier3Chance);

                    int startWhich = HackBeacon.instance.rng.RangeInt(0, 3);

                    for(int i = 0; i < HackBeacon.instance.baseItems + HackBeacon.instance.itemsPerStage * Run.instance.stageClearCount; i++) {
                        var subind = (i + startWhich) % 3;
                        WeightedSelection<List<ItemDef>> which;
                        if(subind == 0)
                            which = itemSelectionD;
                        else if(subind == 1)
                            which = itemSelectionH;
                        else
                            which = itemSelectionU;
                        var list = which.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                        var item = Run.instance.treasureRng.NextElementUniform(list);
                        itemWard.ServerAddItem(item.itemIndex);
                    }
                } else {
                    var itemSelection = new WeightedSelection<List<ItemDef>>(8);
                    itemSelection.AddChoice(tier1Items, HackBeacon.instance.itemTier1Chance);
                    itemSelection.AddChoice(tier2Items, HackBeacon.instance.itemTier2Chance);
                    itemSelection.AddChoice(tier3Items, HackBeacon.instance.itemTier3Chance);
                    for(int i = 0; i < HackBeacon.instance.baseItems + HackBeacon.instance.itemsPerStage * Run.instance.stageClearCount; i++) {
                        var list = itemSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                        var item = Run.instance.treasureRng.NextElementUniform(list);
                        itemWard.ServerAddItem(item.itemIndex);
                    }
                }
            }

            public override void FixedUpdate() {
                //base.FixedUpdate();
                fixedAge += Time.fixedDeltaTime;
            }
        }
    }
}