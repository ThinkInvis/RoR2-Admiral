using RoR2;
using System.Collections.Generic;
using RoR2.Orbs;
using TILER2;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    internal class ShockedOrb : LightningOrb {}

    public class ShockStatusTweaks : AdmiralModule<ShockStatusTweaks> {
        [AutoItemConfig("Chance per frame to shock a nearby ally.",
            AutoItemConfigFlags.None, 0f, 1f)]
        public float shockChance {get; private set;} = 0.033f;

        [AutoItemConfig("Percentage of attacker max health dealt in damage per shock orb.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float shockDamageFrac {get; private set;} = 0.02f;
        
        [AutoItemConfig("Range within which Shocked can damage allies.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float shockRadius {get; private set;} = 15f;

        [AutoItemConfig("Proc coefficient of Shocked arcs.",
            AutoItemConfigFlags.None, 0f, 1f)]
        public float shockProcCoef {get; private set;} = 0.1f;

        [AutoItemConfig("If true, the damage threshold for breaking an enemy out of Shocked will be increased to ridiculous levels.",
            AutoItemConfigFlags.PreventNetMismatch)]
        public bool doThresholdTweak {get; private set;} = true;

        public override string configDescription => "Removes the health threshold from the Shocked status and causes it to deal AoE damage based on victim max health.";
        public override bool invalidatesLanguage => true;

        internal Xoroshiro128Plus shockRng;

        internal override void Setup() {
            base.Setup();
            shockRng = new Xoroshiro128Plus(0u);
        }

        internal override void InstallLang() {
            base.InstallLang();
            languageOverlays.Add(LanguageAPI.AddOverlay("KEYWORD_SHOCKING", "<style=cKeywordName>Shocking</style><style=cSub>Interrupts enemies and temporarily stuns them. A victim of Shocking will <style=cIsDamage>damage their nearby allies</style> for a fraction of their own maximum health per second."));
        }

        internal override void Install() {
            base.Install();
            On.EntityStates.ShockState.OnEnter += On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate += On_ShockStateFixedUpdate;
            IL.RoR2.SetStateOnHurt.OnTakeDamageServer += IL_SSOHTakeDamageServer;
        }

        internal override void Uninstall() {
            base.Uninstall();
            On.EntityStates.ShockState.OnEnter -= On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate -= On_ShockStateFixedUpdate;
            IL.RoR2.SetStateOnHurt.OnTakeDamageServer -= IL_SSOHTakeDamageServer;
        }

        private void IL_SSOHTakeDamageServer(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<SetStateOnHurt>("SetShock"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<SetStateOnHurt, DamageInfo>>((ssoh,di) => {
                var shockHelper = ssoh.targetStateMachine.gameObject.GetComponent<ShockHelper>();
                if(!shockHelper) shockHelper = ssoh.targetStateMachine.gameObject.AddComponent<ShockHelper>();
                shockHelper.currentAttacker = di.attacker;
            });
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
                GameObject attackerObj = null;
                var shockHelper = self.GetComponent<ShockHelper>();
                if(shockHelper) attackerObj = shockHelper.currentAttacker;
				OrbManager.instance.AddOrb(new ShockedOrb {
                    attacker = attackerObj,
					bouncesRemaining = 0,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.AOE,
					damageValue = self.outer.commonComponents.characterBody.maxHealth * shockDamageFrac,
					isCrit = false,
                    inflictor = self.gameObject,
					lightningType = LightningOrb.LightningType.Tesla,
					origin = tpos,
					procChainMask = default,
					procCoefficient = shockProcCoef,
					target = victim.body.mainHurtBox,
					teamIndex = TeamIndex.None
				});
            }
        }
    }

    public class ShockHelper : MonoBehaviour {
        public GameObject currentAttacker;
        public int shockKills = 0; //for Catalyzer Dart achievement tracking
    }
}
