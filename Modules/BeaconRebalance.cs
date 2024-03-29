﻿using RoR2;
using MonoMod.Cil;
using UnityEngine;
using System;
using Mono.Cecil.Cil;
using TILER2;
using R2API;
using RoR2.Skills;

namespace ThinkInvisible.Admiral {
    public class BeaconRebalance : T2Module<BeaconRebalance> {
        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        [AutoConfig("Fractional influence of cooldown reduction, e.g. Alien Head and unknown cooldown sources from other mods, on temporary beacons (0 = no effect, 1 = full effect).",
            AutoConfigFlags.DeferUntilNextStage | AutoConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float beaconCDRInfluence {get; private set;} = 0.5f;

        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        [AutoConfig("Fractional influence of restock, e.g. Bandolier, on temporary beacons (0 = no effect, 1 = full effect).",
            AutoConfigFlags.DeferUntilNextStage | AutoConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float beaconRestockInfluence { get; private set; } = 0.5f;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If false, the original beacon skills will not be replaced; temporary beacons will instead be provided as alternates.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch)]
        public bool removeOriginals { get; private set; } = true;


        public override string enabledConfigDescription => "Changes all Beacon skills to have cooldown and lifetime, and replaces some variants which are incompatible with this model.";
        public override AutoConfigUpdateActionTypes enabledConfigUpdateActionTypes => AutoConfigUpdateActionTypes.InvalidateLanguage;
        public override AutoConfigFlags enabledConfigFlags => AutoConfigFlags.PreventNetMismatch | AutoConfigFlags.DeferUntilNextStage;

        internal GameObject muzzleFlashPrefab;

        public override void SetupAttributes() {
            base.SetupAttributes();
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashSupplyDrop, Healing");
        }

        public override void InstallLanguage() {
            base.InstallLanguage();
            languageOverlays.Add(LanguageAPI.AddOverlay("CAPTAIN_SPECIAL_DESCRIPTION", "Request one of two <style=cIsUtility>temporary</style> Supply Beacons. Both beacons have <style=cIsUtility>independent cooldowns</style>."));
        }

        public override void Install() {
            base.Install();
            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides += IL_CSDCUpdateSkillOverrides;
            On.RoR2.CaptainSupplyDropController.SetSkillOverride += On_CSDCSetSkillOverride;
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval += On_GSCalculateFinalRechargeInterval;
            On.RoR2.GenericSkill.RecalculateMaxStock += On_GSRecalculateMaxStock;
            On.RoR2.GenericSkill.AddOneStock += On_GSAddOneStock;
            On.RoR2.GenericSkill.RunRecharge += On_GSRunRecharge;
            On.RoR2.GenericSkill.FixedUpdate += On_GSFixedUpdate;
            EquipBeacon.instance.Install();
            HackBeacon.instance.Install();
            HealBeacon.instance.Install();
            ShockBeacon.instance.Install();
            StasisBeacon.instance.Install();
        }

        public override void Uninstall() {
            base.Uninstall();
            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides -= IL_CSDCUpdateSkillOverrides;
            On.RoR2.CaptainSupplyDropController.SetSkillOverride -= On_CSDCSetSkillOverride;
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval -= On_GSCalculateFinalRechargeInterval;
            On.RoR2.GenericSkill.RecalculateMaxStock -= On_GSRecalculateMaxStock;
            On.RoR2.GenericSkill.AddOneStock -= On_GSAddOneStock;
            On.RoR2.GenericSkill.RunRecharge -= On_GSRunRecharge;
            On.RoR2.GenericSkill.FixedUpdate -= On_GSFixedUpdate;
            EquipBeacon.instance.Uninstall();
            HackBeacon.instance.Uninstall();
            HealBeacon.instance.Uninstall();
            ShockBeacon.instance.Uninstall();
            StasisBeacon.instance.Uninstall();
        }
        
        private bool SkillIsTemporaryBeacon(SkillDef skillDef) {
            return skillDef == EquipBeacon.instance.skillDef
                || skillDef == HackBeacon.instance.skillDef
                || skillDef == HealBeacon.instance.skillDef
                || skillDef == ShockBeacon.instance.skillDef
                || skillDef == StasisBeacon.instance.skillDef;
        }
        private bool SkillIsTemporaryBeacon(GenericSkill skill) {
            return SkillIsTemporaryBeacon(skill.skillDef);
        }

        bool isNormalRechargeRunning = false;
        private void On_GSFixedUpdate(On.RoR2.GenericSkill.orig_FixedUpdate orig, GenericSkill self) {
            isNormalRechargeRunning = true;
            orig(self);
            isNormalRechargeRunning = false;
        }

        private void On_GSRunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill self, float dt) {
            if(SkillIsTemporaryBeacon(self) && !isNormalRechargeRunning)
                dt *= beaconCDRInfluence;
            orig(self,dt);
        }

        private void On_GSAddOneStock(On.RoR2.GenericSkill.orig_AddOneStock orig, GenericSkill self) {
            if(SkillIsTemporaryBeacon(self)) self.rechargeStopwatch += self.finalRechargeInterval * beaconRestockInfluence;
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

        private void On_CSDCSetSkillOverride(On.RoR2.CaptainSupplyDropController.orig_SetSkillOverride orig, CaptainSupplyDropController self, ref SkillDef currentSkillDef, SkillDef newSkillDef, GenericSkill component) {
            if(SkillIsTemporaryBeacon(currentSkillDef)) return;
            orig(self, ref currentSkillDef, newSkillDef, component);
        }

        private void IL_CSDCUpdateSkillOverrides(ILContext il) {
            //prevent skills from being replaced with usedUpSkillDef once stock runs out -- we'll be using a cooldown instead
            var c = new ILCursor(il);

            int maskLocIndex = -1;
            bool ILFound = c.TryGotoNext(
                x=>x.MatchStloc(out maskLocIndex),
                x=>x.MatchLdloc(maskLocIndex),
                x=>x.MatchLdarg(out _),
                x=>x.MatchLdfld<CaptainSupplyDropController>("authorityEnabledSkillsMask"),
                x=>x.MatchBeq(out _));

            if(ILFound) {
                c.Index = 0;
                c.GotoNext(x => x.MatchStloc(maskLocIndex));
                c.EmitDelegate<Func<byte, byte>>((orig) => {
                    return 3;
                });
            } else {
                AdmiralPlugin.logger.LogError("BeaconRebalance/CSDCUpdateSkillOverrides: Failed to apply IL patch (target instructions not found)");
            }
        }
    }
}
