using UnityEngine;
using RoR2.Skills;
using R2API;
using RoR2;
using R2API.Utils;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;

namespace ThinkInvisible.Admiral {
    public static class HackOverride {
        internal static void Patch() {
            var hackSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHacking");
            hackSkillDef.rechargeStock = 1;
            hackSkillDef.baseRechargeInterval = 40f;

            var hackPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking");
            var hackDecayer = hackPrefab.AddComponent<CaptainBeaconDecayer>();
            hackDecayer.lifetime = 20f;
            hackPrefab.GetComponent<GenericEnergyComponent>().enabled = false;

            var inv = hackPrefab.AddComponent<Inventory>();

            var itemWard = hackPrefab.AddComponent<ItemWard>();
            itemWard.radius = 10f;

            On.EntityStates.CaptainSupplyDrop.HackingMainState.FixedUpdate += On_HMSFixedUpdate;
            On.EntityStates.CaptainSupplyDrop.HackingMainState.OnEnter += On_HMSOnEnter;

            LanguageAPI.Add("CAPTAIN_SUPPLY_HACKING_NAME","Beacon: Special Order");
            LanguageAPI.Add("CAPTAIN_SUPPLY_HACKING_DESCRIPTION","Requisition a pack of <style=cIsUtility>random, temporary items</style> from your trusty quartermaster. All allies standing near the beacon receive these items.");
        }

        private static void On_HMSOnEnter(On.EntityStates.CaptainSupplyDrop.HackingMainState.orig_OnEnter orig, EntityStates.CaptainSupplyDrop.HackingMainState self) {
            orig(self);

            var itemWard = self.outer.gameObject.GetComponent<ItemWard>();

            WeightedSelection<List<PickupIndex>> itemSelection = new WeightedSelection<List<PickupIndex>>(8);
            itemSelection.AddChoice(Run.instance.availableTier1DropList, 0.8f);
            itemSelection.AddChoice(Run.instance.availableTier2DropList, 0.2f);
            itemSelection.AddChoice(Run.instance.availableTier3DropList, 0.01f);
            for(int i = 0; i < 5 + Run.instance.stageClearCount; i++) {
                var list = itemSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                var pickup = Run.instance.treasureRng.NextElementUniform(list);
                itemWard.AddItem(PickupCatalog.GetPickupDef(pickup).itemIndex);
            }
        }

        private static void On_HMSFixedUpdate(On.EntityStates.CaptainSupplyDrop.HackingMainState.orig_FixedUpdate orig, EntityStates.CaptainSupplyDrop.HackingMainState self) {
            if(Util.HasEffectiveAuthority(self.outer.networkIdentity)) self.SetFieldValue("scanTimer", 999f); //disable interactable scanning
            orig(self);
        }
    }
}
