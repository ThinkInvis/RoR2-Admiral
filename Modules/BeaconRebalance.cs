﻿using RoR2;
using MonoMod.Cil;
using UnityEngine;
using System;
using Mono.Cecil.Cil;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class BeaconRebalance : AdmiralModule<BeaconRebalance> {
        [AutoItemConfig("Fractional influence of cooldown reduction/restock on temporary beacons (0 = no effect, 1 = full effect).",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float beaconCDRInfluence {get; private set;} = 0.5f;
        
        public override string configDescription => "Changes all Beacon skills to have cooldown and lifetime, and replaces some variants which are incompatible with this model.";
        public override bool invalidatesLanguage => true;
        public override AutoItemConfigFlags enabledConfigFlags => AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferUntilNextStage;

        internal override void Setup() {
            base.Setup();
        }

        internal override void InstallLang() {
            base.InstallLang();
            languageOverlays.Add(LanguageAPIBleedingEdge.AddOverlay("CAPTAIN_SPECIAL_DESCRIPTION", "Request one of two <style=cIsUtility>temporary</style> Supply Beacons. Both beacons have <style=cIsUtility>independent cooldowns</style>."));
        }

        internal override void Install() {
            base.Install();
            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides += IL_CSDCUpdateSkillOverrides;
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval += On_GSCalculateFinalRechargeInterval;
            On.RoR2.GenericSkill.RecalculateMaxStock += On_GSRecalculateMaxStock;
            On.RoR2.GenericSkill.AddOneStock += On_GSAddOneStock;
            On.RoR2.GenericSkill.RunRecharge += On_GSRunRecharge;
            On.RoR2.GenericSkill.FixedUpdate += On_GSFixedUpdate;
            EquipBeacon.instance.Install();
            HackBeacon.instance.Install();
            HealBeacon.instance.Install();
            ShockBeacon.instance.Install();
        }

        internal override void Uninstall() {
            base.Uninstall();
            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides -= IL_CSDCUpdateSkillOverrides;
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval -= On_GSCalculateFinalRechargeInterval;
            On.RoR2.GenericSkill.RecalculateMaxStock -= On_GSRecalculateMaxStock;
            On.RoR2.GenericSkill.AddOneStock -= On_GSAddOneStock;
            On.RoR2.GenericSkill.RunRecharge -= On_GSRunRecharge;
            On.RoR2.GenericSkill.FixedUpdate -= On_GSFixedUpdate;
            EquipBeacon.instance.Uninstall();
            HackBeacon.instance.Uninstall();
            HealBeacon.instance.Uninstall();
            ShockBeacon.instance.Uninstall();
        }
        
        private bool SkillIsTemporaryBeacon(GenericSkill skill) {
            return skill.skillDef == EquipBeacon.instance.skillDef
                || skill.skillDef == HackBeacon.instance.skillDef
                || skill.skillDef == HealBeacon.instance.skillDef
                || skill.skillDef == ShockBeacon.instance.skillDef;
        }

        private void On_GSFixedUpdate(On.RoR2.GenericSkill.orig_FixedUpdate orig, GenericSkill self) {
            if(SkillIsTemporaryBeacon(self))
                self.RunRecharge(Time.fixedDeltaTime*(1f / beaconCDRInfluence - 1f));
            orig(self);
        }

        private void On_GSRunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill self, float dt) {
            if(SkillIsTemporaryBeacon(self))
                orig(self,dt*beaconCDRInfluence);
            else
                orig(self,dt);
        }

        private void On_GSAddOneStock(On.RoR2.GenericSkill.orig_AddOneStock orig, GenericSkill self) {
            if(SkillIsTemporaryBeacon(self)) self.rechargeStopwatch += self.finalRechargeInterval * beaconCDRInfluence;
            else orig(self);
        }

        private void On_GSRecalculateMaxStock(On.RoR2.GenericSkill.orig_RecalculateMaxStock orig, GenericSkill self) {
            orig(self);
            if(SkillIsTemporaryBeacon(self)) self.maxStock = 1;
        }

        private float On_GSCalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self) {
            var retv = orig(self);
            if(SkillIsTemporaryBeacon(self)) return self.baseRechargeInterval * (1 - beaconCDRInfluence) + retv * beaconCDRInfluence;
            return retv;
        }

        private void IL_CSDCUpdateSkillOverrides(ILContext il) {
            //prevent skills from being replaced with usedUpSkillDef once stock runs out -- we'll be using a cooldown instead
            var c = new ILCursor(il);

            int maskLocIndex = -1;
            bool ILFound = c.TryGotoNext(
                x=>x.MatchLdloc(out maskLocIndex),
                x=>x.MatchLdarg(out _),
                x=>x.MatchLdfld<CaptainSupplyDropController>("authorityEnabledSkillsMask"),
                x=>x.MatchBeq(out _));

            if(ILFound) {
                c.Index++;
                c.EmitDelegate<Func<byte, byte>>(orig => 3);
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stloc, maskLocIndex);
            } else {
                AdmiralPlugin.logger.LogError("BeaconRebalance/CSDCUpdateSkillOverrides: Failed to apply IL patch (target instructions not found)");
            }
        }
    }
}