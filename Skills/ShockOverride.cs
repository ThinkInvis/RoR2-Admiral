using UnityEngine;
using RoR2.Skills;
using RoR2;
using System.Collections.Generic;
using RoR2.Orbs;
using R2API;

namespace ThinkInvisible.Admiral {
    public static class ShockOverride {
        internal static Xoroshiro128Plus shockRng;

        internal static void Patch() {
            var shockSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropShocking");
            shockSkillDef.rechargeStock = 1;
            shockSkillDef.baseRechargeInterval = 30f;

            var shockPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking");
            var shockDecayer = shockPrefab.AddComponent<CaptainBeaconDecayer>();
            shockDecayer.lifetime = 8f;
            shockPrefab.GetComponent<GenericEnergyComponent>().enabled = true;

            shockRng = new Xoroshiro128Plus(0u);
            On.EntityStates.ShockState.OnEnter += On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate += On_ShockStateFixedUpdate;

            LanguageAPI.Add("KEYWORD_SHOCKING", "<style=cKeywordName>Shocking</style><style=cSub>Interrupts enemies and temporarily stuns them. A victim of Shocking will <style=cIsDamage>damage their nearby allies</style> for a fraction of their own maximum health per second.");
        }

        private static void On_ShockStateOnEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self) {
            orig(self);
            EntityStates.ShockState.healthFractionToForceExit = 100f;
        }

        private static void On_ShockStateFixedUpdate(On.EntityStates.ShockState.orig_FixedUpdate orig, EntityStates.ShockState self) {
            orig(self);
            if(shockRng.nextNormalizedFloat < 0.033f) { //works out as roughly 10/sec
                var teamFilter = self.outer.commonComponents.teamComponent;
			    List<TeamComponent> teamMembers = new List<TeamComponent>();
			    bool isFF = FriendlyFireManager.friendlyFireMode != FriendlyFireManager.FriendlyFireMode.Off;
			    if(isFF || teamFilter.teamIndex == TeamIndex.Monster) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Monster));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Neutral) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Neutral));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Player) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Player));
			    float sqrad = 15f * 15f;
                var tpos = self.outer.commonComponents.characterBody.transform.position;
			    teamMembers.Remove(teamFilter);
                teamMembers.RemoveAll(x => (x.transform.position - tpos).sqrMagnitude > sqrad || !x.body || !x.body.mainHurtBox || !x.body.isActiveAndEnabled);
                if(teamMembers.Count == 0) return;
                var victim = shockRng.NextElementUniform(teamMembers);
				OrbManager.instance.AddOrb(new LightningOrb {
					bouncesRemaining = 0,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.AOE,
					damageValue = self.outer.commonComponents.characterBody.maxHealth * 0.02f, // ~= 20% maxhealth/sec total dps
					isCrit = false,
					lightningType = LightningOrb.LightningType.Tesla,
					origin = tpos,
					procChainMask = default,
					procCoefficient = 1f,
					target = victim.body.mainHurtBox,
					teamIndex = TeamIndex.None
				});
            }
        }

    }
}
