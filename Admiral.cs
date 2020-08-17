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
using System.Collections.Generic;
using RoR2.Orbs;

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

        public static BuffIndex stimmedBuffIndex {get; private set;}

        internal static Xoroshiro128Plus shockRng;

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
            

            //Register stimmed buff
            stimmedBuffIndex = BuffAPI.Add(new CustomBuff("Stimmed", "textures/itemicons/texSyringeIcon", Color.red, false, false));
            On.RoR2.Skills.SkillDef.OnFixedUpdate += On_SkillDefFixedUpdate;


            //Apply beacon patches
            HealOverride.Patch();
            EquipmentRestockOverride.Patch();
            ShockOverride.Patch();

            //Apply shock patch
            shockRng = new Xoroshiro128Plus(0u);
            EntityStates.ShockState.healthFractionToForceExit = 1f;
            On.EntityStates.ShockState.FixedUpdate += On_ShockStateFixedUpdate;
        }

        private void On_ShockStateFixedUpdate(On.EntityStates.ShockState.orig_FixedUpdate orig, EntityStates.ShockState self) {
            orig(self);
            if(shockRng.nextNormalizedFloat < 0.033f) { //works out as roughly 10/sec
                var teamFilter = self.outer.commonComponents.teamComponent;
			    List<TeamComponent> teamMembers = new List<TeamComponent>();
			    bool isFF = FriendlyFireManager.friendlyFireMode != FriendlyFireManager.FriendlyFireMode.Off;
			    if(isFF || teamFilter.teamIndex == TeamIndex.Monster) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Monster));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Neutral) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Neutral));
			    if(isFF || teamFilter.teamIndex == TeamIndex.Player) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Player));
			    float sqrad = 7f * 7f;
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

        private void On_SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot) {
            if(skillSlot.characterBody.HasBuff(stimmedBuffIndex))
                skillSlot.RunRecharge(Time.fixedDeltaTime * 0.5f);
            orig(self, skillSlot);
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