using UnityEngine;
using RoR2.Skills;

namespace ThinkInvisible.Admiral {
    public static class ShockOverride {
        internal static void Patch() {
            var shockSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropShocking");
            shockSkillDef.rechargeStock = 1;
            shockSkillDef.baseRechargeInterval = 30f;

            var shockPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking");
            var shockDecayer = shockPrefab.AddComponent<CaptainBeaconDecayer>();
            shockDecayer.lifetime = 8f;
        }
    }
}
