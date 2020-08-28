using EntityStates;
using EntityStates.Captain.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using TILER2;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public class CatalyzerDartSkill : AdmiralModule<CatalyzerDartSkill> {
        [AutoItemConfig("Cooldown of Catalyzer Dart.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 8f;

        [AutoItemConfig("Fraction of remaining DoT damage dealt by malevolent cleanses.",
            AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float evilCleanseDoTDamage {get; private set;} = 3f;

        [AutoItemConfig("Fraction of base damage dealt per non-DoT debuff by malevolent cleanses.",
            AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float evilCleanseNonDoTDamage {get; private set;} = 5f;

        public override string configDescription => "Adds the Catalyzer Dart secondary skill variant.";
        public override AutoItemConfigFlags enabledConfigFlags => AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferForever;

        internal SkillDef skillDef;

        internal GameObject projectilePrefab;

        public class MalevolentCleanseOnHit : MonoBehaviour {}

        internal override void Setup() {
            base.Setup();

            LoadoutAPI.AddSkill(typeof(EntStateFireCatalyzer));

            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;
            var projPfbPfb = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainTazer"));
            projPfbPfb.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().blastRadius = 1f;
            projPfbPfb.AddComponent<MalevolentCleanseOnHit>();
            projectilePrefab = PrefabAPI.InstantiateClone(projPfbPfb, "CaptainCatalyzerProjectile");

            var nametoken = "ADMIRAL_CATALYZER_SKILL_NAME";
            var desctoken = "ADMIRAL_CATALYZER_SKILL_DESC";
            var namestr = "Catalyzer Dart";
            LanguageAPI.Add(nametoken, namestr);
            //todo: update this from config
            LanguageAPI.Add(desctoken, "Fire a fast dart which <style=cIsHealing>catalyzes all debuffs</style>, converting them to <style=cIsDamage>damage</style>: <style=cIsDamage>300%</style> of the remaining total for DoTs, <style=cIsDamage>1x500%</style> otherwise.");
            
            skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateFireCatalyzer>();
            skillDef.interruptPriority = InterruptPriority.Skill;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.baseMaxStock = 1;
            skillDef.rechargeStock = 1;
            skillDef.isBullets = false;
            skillDef.shootDelay = 0.3f;
            skillDef.beginSkillCooldownOnSkillEnd = false;
            skillDef.requiredStock = 1;
            skillDef.stockToConsume = 1;
            skillDef.isCombatSkill = true;
            skillDef.noSprint = true;
            skillDef.canceledFromSprinting = false;
            skillDef.mustKeyPress = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;

            skillDef.skillName = namestr;
            skillDef.skillNameToken = nametoken;
            skillDef.skillDescriptionToken = desctoken;
            skillDef.icon = Resources.Load<Sprite>("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralCatalyzerSkill.png");

            LoadoutAPI.AddSkillDef(skillDef);

            UnlockablesAPI.AddUnlockable<AdmiralCatalyzerAchievement>(false);
            LanguageAPI.Add("ADMIRAL_CATALYZER_ACHIEVEMENT_NAME", "Captain: Hoist By Their Own Petard");
            LanguageAPI.Add("ADMIRAL_CATALYZER_ACHIEVEMENT_DESCRIPTION", "As Captain, kill 6 other enemies by Shocking the same one.");
        }

        internal override void Install() {
            base.Install();

            //todo: unlockable dependent on whether shock module is loaded
            Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSecondarySkillFamily").AddVariant(skillDef, "ADMIRAL_CATALYZER_UNLOCKABLE_ID");

            RoR2.GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            On.EntityStates.Captain.Weapon.FireTazer.Fire += FireTazer_Fire;    
        }

        internal override void Uninstall() {
            base.Uninstall();

            Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSecondarySkillFamily").RemoveVariant(skillDef);
        }

        private void FireTazer_Fire(On.EntityStates.Captain.Weapon.FireTazer.orig_Fire orig, FireTazer self) {
            if(!(self is EntStateFireCatalyzer)) {orig(self); return;}
            var oldPrefab = FireTazer.projectilePrefab;
            FireTazer.projectilePrefab = projectilePrefab;
            orig(self);
            FireTazer.projectilePrefab = oldPrefab;
        }

        private void GlobalEventManager_onServerDamageDealt(DamageReport obj) {
            if(obj.victimBody && obj.damageInfo.inflictor && obj.damageInfo.inflictor.GetComponent<MalevolentCleanseOnHit>()) {
                int totalCleansed = 0;
                for(BuffIndex i = 0; i < (BuffIndex)BuffCatalog.buffCount; i++) {
					var buffDef = BuffCatalog.GetBuffDef(i);
					if(buffDef.isDebuff) {
                        totalCleansed += obj.victimBody.GetBuffCount(i);
					}
                }

                var tsm = obj.victimBody.GetComponent<SetStateOnHurt>()?.targetStateMachine;
			    if (tsm && (tsm.state is FrozenState || tsm.state is StunState || tsm.state is ShockState))
                    totalCleansed++;
                
                float totalDotDamage = 0f;
			    if(DotController.dotControllerLocator.TryGetValue(obj.victimBody.gameObject.GetInstanceID(), out DotController dotController)) {
                    var stacks = dotController.dotStackList;
                    foreach(var stack in stacks) {
                        totalDotDamage += stack.damage * Mathf.Ceil(stack.timer / stack.dotDef.interval);
                    }
			    }

                obj.victimBody.healthComponent.TakeDamage(new DamageInfo {
                    attacker = obj.attacker,
                    crit = false,
                    damage = totalCleansed * obj.attackerBody.damage * evilCleanseNonDoTDamage + totalDotDamage * evilCleanseDoTDamage,
                    damageType = DamageType.Generic,
                    procCoefficient = 0f
                });

                Util.CleanseBody(obj.victimBody, true, false, true, true, false);
            }
        }

        private void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(projectilePrefab);
        }
    }

    public class AdmiralCatalyzerAchievement : ModdedUnlockableAndAchievement<CustomSpriteProvider> {
        public override string AchievementIdentifier => "ADMIRAL_CATALYZER_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => "ADMIRAL_CATALYZER_UNLOCKABLE_ID";
        public override string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public override string AchievementNameToken => "ADMIRAL_CATALYZER_ACHIEVEMENT_NAME";
        public override string AchievementDescToken => "ADMIRAL_CATALYZER_ACHIEVEMENT_DESCRIPTION";
        public override string UnlockableNameToken => "ADMIRAL_CATALYZER_SKILL_NAME";
        protected override CustomSpriteProvider SpriteProvider => new CustomSpriteProvider("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralCatalyzerSkill.png");

        public override bool wantsBodyCallbacks => true;

        public override int LookUpRequiredBodyIndex() {
            return BodyCatalog.FindBodyIndex("CaptainBody");
        }

        public override void OnInstall() {
            base.OnInstall();
            On.RoR2.Orbs.LightningOrb.OnArrival += LightningOrb_OnArrival;
        }

        public override void OnUninstall() {
            base.OnUninstall();
            On.RoR2.Orbs.LightningOrb.OnArrival -= LightningOrb_OnArrival;
        }

        private void LightningOrb_OnArrival(On.RoR2.Orbs.LightningOrb.orig_OnArrival orig, RoR2.Orbs.LightningOrb self) {
            orig(self);
            if(self is ShockedOrb shockedOrb && !self.failedToKill && shockedOrb.shockVictim) {
                var skt = shockedOrb.shockVictim.GetComponent<ShockHelper>();
                if(skt) {
                    skt.shockKills++;
                    if(skt.shockKills >= 6)
                        Grant();
                }
            }
        }
    }
}
