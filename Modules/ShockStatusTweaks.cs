﻿using RoR2;
using System.Collections.Generic;
using RoR2.Orbs;
using TILER2;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using UnityEngine;
using R2API;

namespace ThinkInvisible.Admiral {
    internal class ShockedOrb : LightningOrb {
        public GameObject shockVictim;
    }

    public class ShockStatusTweaks : T2Module<ShockStatusTweaks> {
        [AutoConfigRoOSlider("{0:P2}", 0f, 1f)]
        [AutoConfig("Chance per frame to shock a nearby ally.",
            AutoConfigFlags.None, 0f, 1f)]
        public float shockChance {get; private set;} = 0.033f;

        [AutoConfigRoOSlider("{0:P1}", 0f, 2f)]
        [AutoConfig("Percentage of attacker max health dealt in damage per shock orb.",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float shockDamageFrac {get; private set;} = 0.02f;

        [AutoConfigRoOSlider("{0:N0} m", 0f, 100f)]
        [AutoConfig("Range within which Shocked can damage allies.",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float shockRadius {get; private set;} = 15f;

        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        [AutoConfig("Proc coefficient of Shocked arcs.",
            AutoConfigFlags.None, 0f, 1f)]
        public float shockProcCoef {get; private set;} = 0.1f;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If true, the damage threshold for breaking an enemy out of Shocked will be increased to ridiculous levels.",
            AutoConfigFlags.PreventNetMismatch)]
        public bool doThresholdTweak {get; private set;} = true;

        public override string enabledConfigDescription => "Removes the health threshold from the Shocked status and causes it to deal AoE damage based on victim max health.";
        public override AutoConfigUpdateActionTypes enabledConfigUpdateActionTypes => AutoConfigUpdateActionTypes.InvalidateLanguage;

        public override void InstallLanguage() {
            base.InstallLanguage();
            languageOverlays.Add(LanguageAPI.AddOverlay("KEYWORD_SHOCKING", "<style=cKeywordName>Shocking</style><style=cSub>Interrupts enemies and temporarily stuns them. A victim of Shocking will <style=cIsDamage>damage their nearby allies</style> for a fraction of their own maximum health per second."));
        }

        public override void Install() {
            base.Install();
            On.EntityStates.ShockState.OnEnter += On_ShockStateOnEnter;
            On.EntityStates.ShockState.FixedUpdate += On_ShockStateFixedUpdate;
            IL.RoR2.SetStateOnHurt.OnTakeDamageServer += IL_SSOHTakeDamageServer;
        }

        public override void Uninstall() {
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
            c.EmitDelegate<Action<SetStateOnHurt, DamageReport>>((ssoh,dr) => {
                var shockHelper = ssoh.targetStateMachine.gameObject.GetComponent<ShockHelper>();
                if(!shockHelper) shockHelper = ssoh.targetStateMachine.gameObject.AddComponent<ShockHelper>();
                shockHelper.currentAttacker = dr.attacker;
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
            if(rng.nextNormalizedFloat < shockChance) { //works out as roughly 10/sec
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
                var victim = rng.NextElementUniform(teamMembers);
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
					lightningType = LightningOrb.LightningType.Tesla,
					origin = tpos,
					procChainMask = default,
					procCoefficient = shockProcCoef,
					target = victim.body.mainHurtBox,
					teamIndex = TeamIndex.None,
                    shockVictim = self.gameObject
				});
            }
        }
    }

    public class ShockHelper : MonoBehaviour {
        public GameObject currentAttacker;
        public int shockKills = 0; //for Catalyzer Dart achievement tracking
    }
}
