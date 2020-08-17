using UnityEngine;
using RoR2.Skills;

namespace ThinkInvisible.Admiral {
    public static class HealOverride {
        internal static void Patch() {
            var healSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropHealing");
            healSkillDef.rechargeStock = 1;
            healSkillDef.baseRechargeInterval = 40f;

            var healPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Healing");
            var healDecayer = healPrefab.AddComponent<CaptainBeaconDecayer>();
            healDecayer.lifetime = 20f;
        }
    }
}
