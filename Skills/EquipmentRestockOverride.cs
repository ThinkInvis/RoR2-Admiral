using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API.Utils;
using MonoMod.RuntimeDetour;
using R2API;
using EntityStates.CaptainSupplyDrop;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public static class EquipmentRestockOverride {
        public static BuffIndex stimmedBuffIndex {get; private set;}
        internal static GameObject rejuvWardPrefab;

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
            

            //Cobble together an indicator ring from the healing ward prefab
            var chwPrefab = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard"));
            chwPrefab.GetComponent<HealingWard>().enabled = false;
            var indic = chwPrefab.transform.Find("Indicator");
            var wardDecayer = chwPrefab.AddComponent<CaptainBeaconDecayer>();
            wardDecayer.lifetime = eqprestDecayer.lifetime - CaptainBeaconDecayer.lifetimeDropAdjust; //ward appears after drop
            wardDecayer.silent = true;
            var eqprestWard = chwPrefab.AddComponent<BuffWard>();
            eqprestWard.buffType = stimmedBuffIndex;
            eqprestWard.buffDuration = 1f;
            eqprestWard.radius = 10f;
            eqprestWard.interval = 1f;
            eqprestWard.rangeIndicator = indic;

            indic.Find("IndicatorRing").GetComponent<MeshRenderer>().material.SetColor("_TintColor", new Color(1f, 0.5f, 0f, 1f));
            var chwHsPsRen = indic.Find("HealingSymbols").GetComponent<ParticleSystemRenderer>();
            chwHsPsRen.material.SetTexture("_MainTex", Resources.Load<Texture>("textures/bufficons/texBuffTeslaIcon"));
            chwHsPsRen.material.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));
            chwHsPsRen.trailMaterial.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));

            var chwFlashPsMain = indic.Find("Flashes").GetComponent<ParticleSystem>().main;
            chwFlashPsMain.startColor = new Color(0.5f, 0.25f, 0f, 1f);
            
            rejuvWardPrefab = chwPrefab.InstantiateClone("CaptainRejuvWard");
            UnityEngine.Object.Destroy(chwPrefab);

            //Hide interact option
            On.EntityStates.CaptainSupplyDrop.EquipmentRestockMainState.GetInteractability += On_MainStateGetInteractibility;

            //Instantiate buff zone
            On.EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState.OnEnter += On_BaseSDS_OnEnter;

            //Override lang tokens
            LanguageAPI.Add("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_NAME","Beacon: Rejuvenator");
            LanguageAPI.Add("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_DESCRIPTION","<style=cIsUtility>Buff</style> all nearby allies with <style=cIsUtility>+50% skill recharge rate</style>.");
        }

        private static void On_BaseSDS_OnEnter(On.EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState.orig_OnEnter orig, BaseCaptainSupplyDropState self) {
            orig(self);
            if(self is EquipmentRestockMainState && NetworkServer.active) {
				var buffZoneInstance = UnityEngine.Object.Instantiate<GameObject>(rejuvWardPrefab, self.outer.commonComponents.transform.position, self.outer.commonComponents.transform.rotation);
				buffZoneInstance.GetComponent<TeamFilter>().teamIndex = self.GetFieldValue<TeamFilter>("teamFilter").teamIndex;
				NetworkServer.Spawn(buffZoneInstance);
            }
        }

        private static void On_SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot) {
            if(skillSlot.characterBody.HasBuff(stimmedBuffIndex))
                skillSlot.RunRecharge(Time.fixedDeltaTime * 0.5f);
            orig(self, skillSlot);
        }

        private static Interactability On_MainStateGetInteractibility(On.EntityStates.CaptainSupplyDrop.EquipmentRestockMainState.orig_GetInteractability orig, EntityStates.CaptainSupplyDrop.EquipmentRestockMainState self, Interactor activator) {
            return Interactability.Disabled;
        }
    }
}
