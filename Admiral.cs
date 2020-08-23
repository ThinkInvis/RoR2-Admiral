using RoR2;
using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using UnityEngine;
using Mono.Cecil.Cil;
using System;
using BepInEx.Configuration;
using R2API;
using System.Reflection;
using Path = System.IO.Path;
using RoR2.Skills;

namespace ThinkInvisible.Admiral {
    
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI), nameof(UnlockablesAPI), nameof(R2API.Networking.NetworkingAPI))]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "1.5.3";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        internal static BepInEx.Logging.ManualLogSource logger;

        internal static ConfigFile cfgFile;

        public const float BeaconCDRInfluence = 1f/2f;
        public const float BeaconCDRBaseIncrease = 1f/BeaconCDRInfluence-1;

        public bool legacyBeacons {get; private set;}

        public void Awake() {
            logger = Logger;
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Admiral.admiral_assets")) {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@Admiral", bundle);
                ResourcesAPI.AddProvider(provider);
            }
            
            var cfgLegacyBeacons = cfgFile.Bind(new ConfigDefinition("Admiral", "LegacyBeacons"), false, new ConfigDescription(
                "If true, all changes to Orbital Supply Beacon (except usability in Hidden Realms) are disabled. MUST HAVE SAME VALUE ON BOTH SERVER AND CLIENTS IN MULTIPLAYER!"));
            legacyBeacons = cfgLegacyBeacons.Value;

            //Override CanUseOrbitalSkills to only return false in bazaar, and not in other hidden realms
            var origCUOSGet = typeof(RoR2.CaptainSupplyDropController).GetMethodCached("get_canUseOrbitalSkills");
            var newCUOSGet = typeof(AdmiralPlugin).GetMethodCached(nameof(Hook_Get_CanUseOrbitalSkills));
            var CUOSHook = new Hook(origCUOSGet, newCUOSGet);

            if(!legacyBeacons) {
                IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides += IL_CSDCUpdateSkillOverrides;
            
                LanguageAPI.Add("CAPTAIN_SPECIAL_DESCRIPTION", "Request one of two <style=cIsUtility>temporary</style> Supply Beacons. Both beacons have <style=cIsUtility>independent cooldowns</style>.");

                //TODO: these seem to be set as needed or something?? find out where the hell these are actually defined. assuming 4 sec for now because it's close enough
                //CaptainBeaconDecayer.lifetimeDropAdjust = EntityStates.CaptainSupplyDrop.EntryState.baseDuration + EntityStates.CaptainSupplyDrop.HitGroundState.baseDuration + EntityStates.CaptainSupplyDrop.DeployState.baseDuration;
            
                //Show energy indicator on all beacons
                var origNrgGet = typeof(EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState).GetMethodCached("get_shouldShowEnergy");
                var newNrgGet = typeof(AdmiralPlugin).GetMethodCached(nameof(Hook_Get_ShouldShowEnergy));
                var NrgHook = new Hook(origNrgGet, newNrgGet);

                //Change cooldown reduction/ammo pack stock increase behavior on all beacons
                On.RoR2.GenericSkill.CalculateFinalRechargeInterval += On_GSCalculateFinalRechargeInterval;
                On.RoR2.GenericSkill.RecalculateMaxStock += On_GSRecalculateMaxStock;
                On.RoR2.GenericSkill.AddOneStock += On_GSAddOneStock;
                On.RoR2.GenericSkill.RunRecharge += On_GSRunRecharge;
                On.RoR2.GenericSkill.FixedUpdate += On_GSFixedUpdate;
            }

            //Load modules
            ItemWard.Patch();
            ShotgunOverride.Patch();
            ShockStatusTweaks.Patch();
            if(!legacyBeacons) {
                HealOverride.Patch();
                EquipmentRestockOverride.Patch();
                HackOverride.Patch();
                ShockOverride.Patch();
            }
            OrbitalJumpPadSkill.Patch();
            CatalyzerDartSkill.Patch();
        }

        private bool SkillIsCaptainBeacon(GenericSkill skill) {
            var skfn = SkillCatalog.GetSkillFamilyName(skill.skillFamily.catalogIndex);
            return skfn == "CaptainSupplyDrop1SkillFamily" || skfn == "CaptainSupplyDrop2SkillFamily";
            //return skfn == "";
            //return skill.skillNameToken == "CAPTAIN_SUPPLY_HEAL_NAME" || skill.skillNameToken == "CAPTAIN_SUPPLY_SHOCKING_NAME" || skill.skillNameToken == "CAPTAIN_SUPPLY_HACKING_NAME" || skill.skillNameToken == "CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_NAME";
        }

        private void On_GSFixedUpdate(On.RoR2.GenericSkill.orig_FixedUpdate orig, GenericSkill self) {
            if(SkillIsCaptainBeacon(self))
                self.RunRecharge(Time.fixedDeltaTime*BeaconCDRBaseIncrease);
            orig(self);
        }

        private void On_GSRunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill self, float dt) {
            if(SkillIsCaptainBeacon(self))
                orig(self,dt*BeaconCDRInfluence);
            else
                orig(self,dt);
        }

        private void On_GSAddOneStock(On.RoR2.GenericSkill.orig_AddOneStock orig, GenericSkill self) {
            if(SkillIsCaptainBeacon(self)) self.rechargeStopwatch += self.finalRechargeInterval * BeaconCDRInfluence;
            else orig(self);
        }

        private void On_GSRecalculateMaxStock(On.RoR2.GenericSkill.orig_RecalculateMaxStock orig, GenericSkill self) {
            orig(self);
            if(SkillIsCaptainBeacon(self)) self.maxStock = 1;
        }

        private float On_GSCalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self) {
            var retv = orig(self);
            if(SkillIsCaptainBeacon(self)) return self.baseRechargeInterval * (1 - BeaconCDRInfluence) + retv * BeaconCDRInfluence;
            return retv;
        }

        private static bool Hook_Get_ShouldShowEnergy(EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState self) => true;

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
                Logger.LogError("CSDCUpdateSkillOverrides: Failed to apply IL patch");
            }
        }

        private static bool Hook_Get_CanUseOrbitalSkills(CaptainSupplyDropController self) => SceneCatalog.mostRecentSceneDef.baseSceneName != "bazaar";
    }

    public class CaptainBeaconDecayer : MonoBehaviour {
        public static float lifetimeDropAdjust {get; internal set;} = 4f;

        public float lifetime = 15f;
        public bool silent = false;
        private float stopwatch = 0f;

        private GenericEnergyComponent energyCpt;

        private void Awake() {
            energyCpt = gameObject.GetComponent<GenericEnergyComponent>();
        }

        private void FixedUpdate() {
            stopwatch += Time.fixedDeltaTime;
            if(energyCpt) {
                energyCpt.capacity = lifetime;
                energyCpt.energy = lifetime - stopwatch + lifetimeDropAdjust;
            }
            if(stopwatch >= lifetime + lifetimeDropAdjust) {
                if(!silent) {
                    EffectManager.SpawnEffect(Resources.Load<GameObject>("prefabs/effects/omnieffect/OmniExplosionVFXEngiTurretDeath"),
                        new EffectData {
                            origin = transform.position,
                            scale = 5f
                        }, true);
                }

                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}