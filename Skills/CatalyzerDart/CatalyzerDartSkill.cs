using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public static class CatalyzerDartSkill {
        internal static SkillDef skillDef;

        internal static GameObject projectilePrefab;

        public class MalevolentCleanseOnHit : MonoBehaviour {}

        internal static void Patch() {
            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;
            var projPfbPfb = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainTazer"));
            projPfbPfb.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().enabled = false;
            projPfbPfb.AddComponent<MalevolentCleanseOnHit>();
            projectilePrefab = PrefabAPI.InstantiateClone(projPfbPfb, "CaptainCatalyzerProjectile");

            var nametoken = "ADMIRAL_CATALYZER_SKILL_NAME";
            var desctoken = "ADMIRAL_CATALYZER_SKILL_DESC";
            var namestr = "Catalyzer Dart";
            LanguageAPI.Add(nametoken, namestr);
            LanguageAPI.Add(desctoken, "Fire a fast dart which <style=cIsHealing>catalyzes all debuffs</style>, converting them to <style=cIsDamage>damage</style>: <style=cIsDamage>125%</style> of the remaining total for DoTs, <style=cIsDamage>1x200%</style> otherwise.");
            
            skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateFireCatalyzer>();
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
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
            skillDef.icon = Resources.Load<SkillDef>("skilldefs/captainbody/PrepSupplyDrop").icon;

            LoadoutAPI.AddSkillDef(skillDef);

            var csdf = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSecondarySkillFamily");
            Array.Resize(ref csdf.variants, csdf.variants.Length + 1);
            csdf.variants[csdf.variants.Length - 1] = new SkillFamily.Variant {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(nametoken, false, null),
                unlockableName = "ADMIRAL_CATALYZER_UNLOCKABLE_ID"
            };

            RoR2.GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
        }

        private static void GlobalEventManager_onServerDamageDealt(DamageReport obj) {
            if(obj.victimBody && obj.damageInfo.inflictor.GetComponent<MalevolentCleanseOnHit>()) {
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
                        totalDotDamage += stack.damage * Mathf.Floor(stack.timer / stack.dotDef.interval);
                    }
			    }

                obj.victimBody.healthComponent.TakeDamage(new DamageInfo {
                    attacker = obj.attacker,
                    crit = false,
                    damage = totalCleansed * obj.attackerBody.damage * 2f + totalDotDamage * 1.25f,
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
