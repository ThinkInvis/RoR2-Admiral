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
        public const string ModVer = "1.0.0";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        public static float fireDelayDynamic {get; private set;}
        public static float fireDelayFixed {get; private set;}


        public void Awake() {
            ConfigFile cfgFile = new ConfigFile(Paths.ConfigPath + "\\" + ModGuid + ".cfg", true);

            var cfgFireDelayDynamic = cfgFile.Bind(new ConfigDefinition("Admiral", "FireDelayDynamic"), 0.2f, new ConfigDescription(
                "Time, in fraction of total charge time, to wait before autofiring Vulcan Shotgun after reaching full charge. Set both this and FireDelayFixed to 0 to disable autofire.",
                new AcceptableValueRange<float>(0f,float.MaxValue)));
            fireDelayDynamic = 1f + cfgFireDelayDynamic.Value;

            var cfgFireDelayFixed = cfgFile.Bind(new ConfigDefinition("Admiral", "FireDelayFixed"), 0f, new ConfigDescription(
                "Absolute minimum time, in seconds, to wait before autofiring Vulcan Shotgun after reaching full charge. Set both this and FireDelayDynamic to 0 to disable autofire.",
                new AcceptableValueRange<float>(0f,float.MaxValue)));
            fireDelayFixed = cfgFireDelayFixed.Value;


            //Override CanUseOrbitalSkills to only return false in bazaar, and not in other hidden realms
            var origCUOSGet = typeof(RoR2.CaptainSupplyDropController).GetMethodCached("get_canUseOrbitalSkills");
            var newCUOSGet = typeof(AdmiralPlugin).GetMethodCached(nameof(Hook_Get_CanUseOrbitalSkills));
            var CUOSHook = new Hook(origCUOSGet, newCUOSGet);

            IL.RoR2.CaptainSupplyDropController.UpdateSkillOverrides += IL_CSDCUpdateSkillOverrides;
            if(fireDelayFixed > 0f || fireDelayDynamic > 0f)
                On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate += On_CapChargeShotgunFixedUpdate;
            
            LanguageAPI.Add("CAPTAIN_SPECIAL_DESCRIPTION", "Request one of two <style=cIsUtility>temporary</style> Supply Beacons. Both beacons have <style=cIsUtility>independent cooldowns</style>.");

            //TODO: make this untrue
            LanguageAPI.Add("CAPTAIN_SUPPLY_HACKING_DESCRIPTION", "<style=cIsUtility>Hack</style> all nearby purchasables to a cost of <style=cIsUtility>$0</style> over time. Only usable <style=cIsUtility>once per stage</style>.");

            //Apply beacon patches
            HealOverride.Patch();
            EquipmentRestockOverride.Patch();
            ShockOverride.Patch();
        }

        private void On_CapChargeShotgunFixedUpdate(On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.orig_FixedUpdate orig, EntityStates.Captain.Weapon.ChargeCaptainShotgun self) {
            if(Util.HasEffectiveAuthority(self.outer.networkIdentity)) {
                var fixedAge = (float)typeof(EntityStates.EntityState).GetPropertyCached("fixedAge").GetValue(self);
                var chargeDuration = self.GetFieldValue<float>("chargeDuration");
                if(fixedAge / chargeDuration > fireDelayDynamic && fixedAge - chargeDuration > fireDelayFixed) self.SetFieldValue<bool>("released", true);
            }
            orig(self);
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
                Logger.LogError("CSDCUpdateSkillOverrides: Failed to apply IL patch");
            }
        }

        private static bool Hook_Get_CanUseOrbitalSkills(CaptainSupplyDropController self) => SceneCatalog.mostRecentSceneDef.baseSceneName != "bazaar";
    }

    public class CaptainBeaconDecayer : MonoBehaviour {
        public float lifetime = 15f;
        private float stopwatch = 0f;

        private void FixedUpdate() {
            stopwatch += Time.fixedDeltaTime;
            if(stopwatch >= lifetime) {
                EffectManager.SpawnEffect(Resources.Load<GameObject>("prefabs/effects/omnieffect/OmniExplosionVFXEngiTurretDeath"),
                    new EffectData {
                        origin = this.transform.position,
                        scale = 5f
                    }, true);

                UnityEngine.Object.Destroy(this.gameObject);
            }
        }
    }
}