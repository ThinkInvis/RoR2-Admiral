using UnityEngine;
using RoR2.Skills;
using RoR2;
using System.Collections.Generic;
using RoR2.Orbs;
using R2API;

namespace ThinkInvisible.Admiral {
    public static class ShockOverride {
        internal static void Patch() {
            var shockSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropShocking");
            shockSkillDef.rechargeStock = 1;
            shockSkillDef.baseRechargeInterval = 30f;

            var shockPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking");
            var shockDecayer = shockPrefab.AddComponent<CaptainBeaconDecayer>();
            shockDecayer.lifetime = 8f;
            shockPrefab.GetComponent<GenericEnergyComponent>().enabled = true;

            On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.OnEnter += ShockZoneMainState_OnEnter;
        }

        private static void ShockZoneMainState_OnEnter(On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.orig_OnEnter orig, EntityStates.CaptainSupplyDrop.ShockZoneMainState self) {
            orig(self);
            EntityStates.CaptainSupplyDrop.ShockZoneMainState.shockFrequency = 1f/1.5f;
        }
    }
}
