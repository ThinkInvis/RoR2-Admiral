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

namespace ThinkInvisible.Admiral {
    
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI))]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "1.3.1";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        internal static ConfigFile cfgFile;

        public void Awake() {
            cfgFile = new ConfigFile(Paths.ConfigPath + "\\" + ModGuid + ".cfg", true);

            //Override CanUseOrbitalSkills to only return false in bazaar, and not in other hidden realms
            var origCUOSGet = typeof(RoR2.CaptainSupplyDropController).GetMethodCached("get_canUseOrbitalSkills");
            var newCUOSGet = typeof(AdmiralPlugin).GetMethodCached(nameof(Hook_Get_CanUseOrbitalSkills));
            var CUOSHook = new Hook(origCUOSGet, newCUOSGet);

            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides += IL_CSDCUpdateSkillOverrides;
            
            LanguageAPI.Add("CAPTAIN_SPECIAL_DESCRIPTION", "Request one of two <style=cIsUtility>temporary</style> Supply Beacons. Both beacons have <style=cIsUtility>independent cooldowns</style>.");

            //TODO: make this untrue
            LanguageAPI.Add("CAPTAIN_SUPPLY_HACKING_DESCRIPTION", "<style=cIsUtility>Hack</style> all nearby purchasables to a cost of <style=cIsUtility>$0</style> over time. Only usable <style=cIsUtility>once per stage</style>.");

            //TODO: these seem to be set as needed or something?? find out where the hell these are actually defined. assuming 4 sec for now because it's close enough
            //CaptainBeaconDecayer.lifetimeDropAdjust = EntityStates.CaptainSupplyDrop.EntryState.baseDuration + EntityStates.CaptainSupplyDrop.HitGroundState.baseDuration + EntityStates.CaptainSupplyDrop.DeployState.baseDuration;
            
            //Show energy indicator on all beacons
            var origNrgGet = typeof(EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState).GetMethodCached("get_shouldShowEnergy");
            var newNrgGet = typeof(AdmiralPlugin).GetMethodCached(nameof(Hook_Get_ShouldShowEnergy));
            var NrgHook = new Hook(origCUOSGet, newCUOSGet);

            //Apply individual skill patches (separated for purposes of organization)
            ItemWard.Patch();
            ShotgunOverride.Patch();
            HealOverride.Patch();
            EquipmentRestockOverride.Patch();
            HackOverride.Patch();
            ShockOverride.Patch();
            OrbitalJumpPadSkill.Patch();
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