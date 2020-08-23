using RoR2;
using System.Collections.Generic;
using RoR2.Orbs;
using TILER2;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    internal class ShockedOrb : LightningOrb {}

    public class ShockStatusTweaks : RuntimeAdmiralSubmodule<ShockStatusTweaks> {
        [AutoItemConfig("Chance per frame to shock a nearby ally.",
            AutoItemConfigFlags.None, 0f, 1f)]
        public float shockChance {get; private set;} = 0.033f;

        [AutoItemConfig("Percentage of attacker max health dealt in damage per shock orb.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float shockDamageFrac {get; private set;} = 0.02f;
        
        [AutoItemConfig("Range within which Shocked can damage allies.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float shockRadius {get; private set;} = 15f;

        [AutoItemConfig("If true, the damage threshold for breaking an enemy out of Shocked will be increased to ridiculous levels.",
            AutoItemConfigFlags.PreventNetMismatch)]
        public bool doThresholdTweak {get; private set;} = true;

        internal Xoroshiro128Plus shockRng;

        internal override void Setup() {
            base.Setup();
            shockRng = new Xoroshiro128Plus(0u);

            //TODO: InstallLang
            //LanguageAPI.Add("KEYWORD_SHOCKING", "<style=cKeywordName>Shocking</style><style=cSub>Interrupts enemies and temporarily stuns them. A victim of Shocking will <style=cIsDamage>damage their nearby allies</style> for a fraction of their own maximum health per second.");
        }

        internal override void Install() {
            base.Install();
            On.EntityStates.ShockState.OnEnter += On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate += On_ShockStateFixedUpdate;
        }

        internal override void Uninstall() {
            base.Uninstall();
            On.EntityStates.ShockState.OnEnter -= On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate -= On_ShockStateFixedUpdate;
        }

        private void On_ShockStateOnEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self) {
            orig(self);
            if(doThresholdTweak)
                EntityStates.ShockState.healthFractionToForceExit = 100f;
        }

        private void On_ShockStateFixedUpdate(On.EntityStates.ShockState.orig_FixedUpdate orig, EntityStates.ShockState self) {
            orig(self);
            if(!NetworkServer.active) return;
            if(shockRng.nextNormalizedFloat < shockChance) { //works out as roughly 10/sec
                var teamFilter = self.outer.commonComponents.teamComponent;
			    List<TeamComponent> teamMembers = new List<TeamComponent>();
			    bool isFF = FriendlyFireManager.friendlyFireMode != FriendlyFireManager.FriendlyFireMode.Off;
			    if(isFF || teamFilter.teamIndex == TeamIndex.Monster) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Monster));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Neutral) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Neutral));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Player) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Player));
                //todo: cache this on config changes
			    float sqrad = shockRadius * shockRadius;
                var tpos = self.outer.commonComponents.characterBody.transform.position;
			    teamMembers.Remove(teamFilter);
                teamMembers.RemoveAll(x => (x.transform.position - tpos).sqrMagnitude > sqrad || !x.body || !x.body.mainHurtBox || !x.body.isActiveAndEnabled);
                if(teamMembers.Count == 0) return;
                var victim = shockRng.NextElementUniform(teamMembers);
				OrbManager.instance.AddOrb(new ShockedOrb {
                    attacker = self.gameObject,
					bouncesRemaining = 0,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.AOE,
					damageValue = self.outer.commonComponents.characterBody.maxHealth * shockDamageFrac,
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
