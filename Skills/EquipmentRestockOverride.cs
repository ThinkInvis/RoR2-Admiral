using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API.Utils;
using MonoMod.RuntimeDetour;
using R2API;

namespace ThinkInvisible.Admiral {
    public static class EquipmentRestockOverride {
        public static BuffIndex stimmedBuffIndex {get; private set;}

        internal static void Patch() {
            //Register stimmed buff
            stimmedBuffIndex = BuffAPI.Add(new CustomBuff("Stimmed", "textures/itemicons/texSyringeIcon", Color.red, false, false));
            On.RoR2.Skills.SkillDef.OnFixedUpdate += On_SkillDefFixedUpdate;


            var eqprestSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropEquipmentRestock");
            eqprestSkillDef.rechargeStock = 1;
            eqprestSkillDef.baseRechargeInterval = 60f; //assumes the user will stand in the AoE and recharge this skill faster

            var eqprestPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock");
            var eqprestDecayer = eqprestPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = 20f;

            eqprestPrefab.GetComponent<ProxyInteraction>().enabled = false;
            eqprestPrefab.GetComponent<GenericEnergyComponent>().enabled = false;
            var eqprestWard = eqprestPrefab.AddComponent<BuffWard>();
            eqprestWard.buffType = stimmedBuffIndex;
            eqprestWard.buffDuration = 1f;
            eqprestWard.radius = 7f;
            eqprestWard.interval = 1f;
            //TODO: find out how to add the area indicator thingy the other beacons have

            //Hide interact option
            On.EntityStates.CaptainSupplyDrop.EquipmentRestockMainState.GetInteractability += On_MainStateGetInteractibility;

            //Hide energy indicator
            var origCUOSGet = typeof(EntityStates.CaptainSupplyDrop.EquipmentRestockMainState).GetMethodCached("get_shouldShowEnergy");
            var newCUOSGet = typeof(EquipmentRestockOverride).GetMethodCached(nameof(Hook_Get_ShouldShowEnergy));
            var CUOSHook = new Hook(origCUOSGet, newCUOSGet);
            

            //broken until next R2API release
            //LanguageAPI.Add("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_NAME","Beacon: Rejuvenator");
            //LanguageAPI.Add("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_DESCRIPTION","<style=cIsUtility>Buff</style> all nearby allies with <style=cIsUtility>+50% skill recharge rate</style>.");
        }

        private static void On_SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot) {
            if(skillSlot.characterBody.HasBuff(stimmedBuffIndex))
                skillSlot.RunRecharge(Time.fixedDeltaTime * 0.5f);
            orig(self, skillSlot);
        }

        private static bool Hook_Get_ShouldShowEnergy(EntityStates.CaptainSupplyDrop.EquipmentRestockMainState self) => false;

        private static Interactability On_MainStateGetInteractibility(On.EntityStates.CaptainSupplyDrop.EquipmentRestockMainState.orig_GetInteractability orig, EntityStates.CaptainSupplyDrop.EquipmentRestockMainState self, Interactor activator) {
            return Interactability.Disabled;
        }
    }
}
