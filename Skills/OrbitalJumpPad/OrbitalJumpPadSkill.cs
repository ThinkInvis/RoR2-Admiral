using R2API;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public static class OrbitalJumpPadSkill {
        internal static SkillDef setupSkillDef;
        internal static SkillDef callSkillDef;

        internal static void Patch() {
            On.EntityStates.Captain.Weapon.CallAirstrikeBase.ModifyProjectile += On_CABModifyProjectile;

            var nametoken = "ADMIRAL_JUMPPAD_NAME";
            var desctoken = "ADMIRAL_JUMPPAD_DESC";
            var namestr = "Orbital Jump Pad";
            LanguageAPI.Add(nametoken, namestr);
            LanguageAPI.Add(desctoken, "Request an Orbital Jump Pad from the <style=cIsUtility>UES Safe Travels</style>. Fire once to set the jump pad, then again to set its target.");
            
            setupSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            setupSkillDef.activationStateMachineName = "Skillswap";
            setupSkillDef.activationState = LoadoutAPI.StateTypeOf<EntStateSetupJumpPad>();
            setupSkillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            setupSkillDef.baseRechargeInterval = 20f;
            setupSkillDef.baseMaxStock = 1;
            setupSkillDef.rechargeStock = 1;
            setupSkillDef.isBullets = false;
            setupSkillDef.shootDelay = 0f;
            setupSkillDef.beginSkillCooldownOnSkillEnd = true;
            setupSkillDef.requiredStock = 1;
            setupSkillDef.stockToConsume = 1;
            setupSkillDef.isCombatSkill = false;
            setupSkillDef.noSprint = true;
            setupSkillDef.canceledFromSprinting = true;
            setupSkillDef.mustKeyPress = true;
            setupSkillDef.fullRestockOnAssign = true;

            setupSkillDef.skillName = namestr;
            setupSkillDef.skillNameToken = nametoken;
            setupSkillDef.skillDescriptionToken = desctoken;
            setupSkillDef.icon = Resources.Load<Sprite>("");

            LoadoutAPI.AddSkillDef(setupSkillDef);
            

            callSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            callSkillDef.activationStateMachineName = "Weapon";
            callSkillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallJumpPad>();
            callSkillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            callSkillDef.baseRechargeInterval = 0f;
            callSkillDef.baseMaxStock = 2;
            callSkillDef.rechargeStock = 0;
            callSkillDef.isBullets = false;
            callSkillDef.shootDelay = 0.3f;
            callSkillDef.beginSkillCooldownOnSkillEnd = true;
            callSkillDef.requiredStock = 1;
            callSkillDef.stockToConsume = 1;
            callSkillDef.isCombatSkill = false;
            callSkillDef.noSprint = true;
            callSkillDef.canceledFromSprinting = true;
            callSkillDef.mustKeyPress = true;
            callSkillDef.fullRestockOnAssign = true;
            callSkillDef.dontAllowPastMaxStocks = true;

            callSkillDef.skillName = namestr;
            callSkillDef.skillNameToken = nametoken;
            callSkillDef.skillDescriptionToken = desctoken;
            callSkillDef.icon = Resources.Load<Sprite>("");

            LoadoutAPI.AddSkillDef(callSkillDef);
        }

        private static void On_CABModifyProjectile(On.EntityStates.Captain.Weapon.CallAirstrikeBase.orig_ModifyProjectile orig, EntityStates.Captain.Weapon.CallAirstrikeBase self, ref RoR2.Projectile.FireProjectileInfo fireProjectileInfo) {
            orig(self, ref fireProjectileInfo);
            bool is1 = self is EntStateJumpPad1;
            bool is2 = self is EntStateJumpPad2;
            if(!(is1 || is2)) return;
            //fireProjectileInfo.projectilePrefab = 
            fireProjectileInfo.damage /= 6f;
        }
    }
}
