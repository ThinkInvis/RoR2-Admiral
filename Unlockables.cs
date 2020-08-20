using RoR2;
using BepInEx.Configuration;
using R2API.Utils;
using R2API;
using UnityEngine;
using RoR2.Achievements;
using System.Collections.Generic;

namespace ThinkInvisible.Admiral {
    public static class Unlockables {
        internal static void Patch() {
            UnlockablesAPI.AddUnlockable<AdmiralJumpPadAchievement>(false);
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME", "Damn the Torpedoes");
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION", "As Captain, nail a very speedy target with an Orbital Probe.");

            LanguageAPI.Add("ADMIRAL_CATALYZER_ACHIEVEMENT_NAME", "Well-Seasoned");
            LanguageAPI.Add("ADMIRAL_CATALYZER_ACHIEVEMENT_DESCRIPTION", "As Captain, inflict 10 debuff stacks at once.");
        }
    }

    public class AdmiralJumpPadAchievement : ModdedUnlockableAndAchievement<VanillaSpriteProvider> {
        public override string AchievementIdentifier => "ADMIRAL_JUMPPAD_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => "ADMIRAL_JUMPPAD_UNLOCKABLE_ID";
        public override string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public override string AchievementNameToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME";
        public override string AchievementDescToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION";
        public override string UnlockableNameToken => "ADMIRAL_JUMPPAD_SKILL_NAME";
        protected override VanillaSpriteProvider SpriteProvider => new VanillaSpriteProvider("textures/bufficons/texBuffWeakIcon");

        public override bool wantsBodyCallbacks => true;

        int projTestInd1 = -1;
        int projTestInd2 = -1;
        int projTestInd3 = -1;

        public override int LookUpRequiredBodyIndex() {
            return BodyCatalog.FindBodyIndex("CaptainBody");
        }

        public override void OnInstall() {
            base.OnInstall();
            RoR2.GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            On.RoR2.CharacterBody.Awake += CharacterBody_Awake;
			projTestInd1 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile1");
            projTestInd2 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile2");
            projTestInd3 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile3");
        }

        private void CharacterBody_Awake(On.RoR2.CharacterBody.orig_Awake orig, CharacterBody self) {
            orig(self);
            self.gameObject.AddComponent<AverageSpeedTracker>();
        }

        public override void OnUninstall() {
            base.OnUninstall();
            RoR2.GlobalEventManager.onServerDamageDealt -= GlobalEventManager_onServerDamageDealt;
            On.RoR2.CharacterBody.Awake -= CharacterBody_Awake;
        }
        private void GlobalEventManager_onServerDamageDealt(DamageReport obj) {
            if(!meetsBodyRequirement) return;
            if(!obj.victimBody || obj.damageInfo.attacker != NetworkUser.readOnlyLocalPlayersList[0].GetCurrentBody().gameObject) return;
            var projInd = ProjectileCatalog.GetProjectileIndex(obj.damageInfo.inflictor);
            if(projInd != projTestInd1 && projInd != projTestInd2 && projInd != projTestInd3) return;
            var vel = obj.victimBody.GetComponent<AverageSpeedTracker>().QuerySpeed();
            var projdist = (obj.damageInfo.position - obj.damageInfo.inflictor.transform.position).magnitude;
            if(vel > 20f && projdist < 3f)
                base.Grant();
        }
    }
    
    public class AdmiralCatalyzerAchievement : ModdedUnlockableAndAchievement<VanillaSpriteProvider> {
        public override string AchievementIdentifier => "ADMIRAL_CATALYZER_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => "ADMIRAL_CATALYZER_UNLOCKABLE_ID";
        public override string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public override string AchievementNameToken => "ADMIRAL_CATALYZER_ACHIEVEMENT_NAME";
        public override string AchievementDescToken => "ADMIRAL_CATALYZER_ACHIEVEMENT_DESCRIPTION";
        public override string UnlockableNameToken => "ADMIRAL_CATALYZER_SKILL_NAME";
        protected override VanillaSpriteProvider SpriteProvider => new VanillaSpriteProvider("textures/bufficons/texBuffWeakIcon");

        public override bool wantsBodyCallbacks => true;

        public override int LookUpRequiredBodyIndex() {
            return BodyCatalog.FindBodyIndex("CaptainBody");
        }

        public override void OnInstall() {
            base.OnInstall();
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.CharacterBody.Awake += CharacterBody_Awake;
        }

        public override void OnUninstall() {
            base.OnUninstall();
            On.RoR2.HealthComponent.TakeDamage -= HealthComponent_TakeDamage;
            On.RoR2.CharacterBody.Awake -= CharacterBody_Awake;
        }

        private void CharacterBody_Awake(On.RoR2.CharacterBody.orig_Awake orig, CharacterBody self) {
            orig(self);
            if(self.bodyIndex != this.requiredBodyIndex) return;
            self.gameObject.AddComponent<DebuffsInflictedTracker>();
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
            if(!meetsBodyRequirement) return;
            if(!self.body || damageInfo.attacker != NetworkUser.readOnlyLocalPlayersList[0].GetCurrentBody().gameObject) return;

            int debuffsPre = 0;
            for(BuffIndex i = 0; i < (BuffIndex)BuffCatalog.buffCount; i++) {
				var buffDef = BuffCatalog.GetBuffDef(i);
				if(buffDef.isDebuff) {
                    debuffsPre += self.body.GetBuffCount(i);
				}
            }

            orig(self, damageInfo);
            
            int debuffsPost = 0;
            for(BuffIndex i = 0; i < (BuffIndex)BuffCatalog.buffCount; i++) {
				var buffDef = BuffCatalog.GetBuffDef(i);
				if(buffDef.isDebuff) {
                    debuffsPost += self.body.GetBuffCount(i);
				}
            }

            var dit = damageInfo.attacker.GetComponent<DebuffsInflictedTracker>();
            dit.debuffCount += Mathf.Max(debuffsPost - debuffsPre, 0);
            Debug.Log("DebuffCount changed by " + (debuffsPost - debuffsPre) + " -- now " + dit.debuffCount);
            if(dit.debuffCount > 10)
                base.Grant();
        }
    }

    public class DebuffsInflictedTracker : MonoBehaviour {
        public int debuffCount = 0;
        public float decayRate = 0.5f;

        private float stopwatch = 0f;

        private void FixedUpdate() {
            if(debuffCount > 0)
                stopwatch += Time.fixedDeltaTime;
            if(stopwatch > decayRate) {
                debuffCount = 0;
                stopwatch = 0f;
            }
        }
    }

    public class AverageSpeedTracker : MonoBehaviour {
        private List<Vector3> positions = new List<Vector3>();
        private List<float> deltas = new List<float>();
        public float pollingRate = 0.2f;
        private uint _history = 5;
        public uint history {get{return _history;} set{positions.Clear();deltas.Clear();_history=value;} }

        private float stopwatch = 0f;

        private void FixedUpdate() {
            stopwatch += Time.fixedDeltaTime;
            if(stopwatch > pollingRate) {
                positions.Add(transform.position);
                deltas.Add(stopwatch);
                if(positions.Count >= _history) {
                    positions.RemoveAt(0);
                    deltas.RemoveAt(0);
                }
                stopwatch = 0f;
            }
        }
        public float QuerySpeed() {
            float totalVel = 0f;
            for(var i = 0; i < positions.Count - 1; i++) {
                totalVel += (positions[i+1] - positions[i]).magnitude / deltas[i+1];
            }
            return totalVel;
        }
    }
}
