using EntityStates;
using EntityStates.Captain.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public static class CatalyzerDartSkill {
        internal static SkillDef skillDef;

        internal static GameObject projectilePrefab;

        public class MalevolentCleanseOnHit : MonoBehaviour {}

        internal static void Patch() {
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
            LanguageAPI.Add(desctoken, "Fire a fast dart which <style=cIsHealing>catalyzes all debuffs</style>, converting them to <style=cIsDamage>damage</style>: <style=cIsDamage>150%</style> of the remaining total for DoTs, <style=cIsDamage>1x200%</style> otherwise.");
            
            skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateFireCatalyzer>();
            skillDef.interruptPriority = InterruptPriority.Skill;
            skillDef.baseRechargeInterval = 10f;
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

            var csdf = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSecondarySkillFamily");
            Array.Resize(ref csdf.variants, csdf.variants.Length + 1);
            csdf.variants[csdf.variants.Length - 1] = new SkillFamily.Variant {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(nametoken, false, null),
                unlockableName = "ADMIRAL_CATALYZER_UNLOCKABLE_ID"
            };

            RoR2.GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            On.EntityStates.Captain.Weapon.FireTazer.Fire += FireTazer_Fire;
        }

        private static void FireTazer_Fire(On.EntityStates.Captain.Weapon.FireTazer.orig_Fire orig, FireTazer self) {
            if(!(self is EntStateFireCatalyzer)) {orig(self); return;}
            var oldPrefab = FireTazer.projectilePrefab;
            FireTazer.projectilePrefab = CatalyzerDartSkill.projectilePrefab;
            orig(self);
            FireTazer.projectilePrefab = oldPrefab;
        }

        private static void GlobalEventManager_onServerDamageDealt(DamageReport obj) {
            if(obj.victimBody && obj.damageInfo.inflictor?.GetComponent<MalevolentCleanseOnHit>()) {
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
			    DotController dotController;
			    if(DotController.dotControllerLocator.TryGetValue(obj.victimBody.gameObject.GetInstanceID(), out dotController)) {
                    var stacks = dotController.dotStackList;
                    foreach(var stack in stacks) {
                        totalDotDamage += stack.damage * Mathf.Ceil(stack.timer / stack.dotDef.interval);
                    }
			    }

                obj.victimBody.healthComponent.TakeDamage(new DamageInfo {
                    attacker = obj.attacker,
                    crit = false,
                    damage = totalCleansed * obj.attackerBody.damage * 2f + totalDotDamage * 1.5f,
                    damageType = DamageType.Generic,
                    procCoefficient = 0f
                });

                Util.CleanseBody(obj.victimBody, true, false, true, true, false);
            }
        }

        private static void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(projectilePrefab);
        }
    }
}
