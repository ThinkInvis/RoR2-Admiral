﻿using EntityStates;
using EntityStates.Captain.Weapon;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using TILER2;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public class SKGunSkill : AdmiralModule<SKGunSkill> {
        public const float recoveryTime = 0.5f;
        public const float fullReloadTime = 1.25f;

        [AutoItemConfig("If false, Valiant Blaster reload rate will not increase with attack speed.",
            AutoItemConfigFlags.PreventNetMismatch)]
        public bool attackSpeedAffectsReload {get; private set;} = true;

        [AutoItemConfig("Minimum time required to fully charge Valiant Blaster (does not affect base charge time, only attack speed scaling).",
            AutoItemConfigFlags.PreventNetMismatch)]
        public float minChargeTime {get; private set;} = 0.5f;

        public override string configDescription => "Adds the Valiant Blaster primary skill variant.";
        public override AutoItemConfigFlags enabledConfigFlags => AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferForever;

        internal BuffIndex slowSkillDebuff;

        internal SkillDef skillDef;
        internal RoR2.Stats.StatDef shotgunKillsStatDef;

        internal GameObject projectilePrefab;
        internal GameObject chargedProjectilePrefab;

        internal override void Setup() {
            base.Setup();
            
            LoadoutAPI.AddSkill(typeof(EntStateChargeSKGun));
            LoadoutAPI.AddSkill(typeof(EntStateFireSKGun));

            var nametoken = "ADMIRAL_SKGUN_SKILL_NAME";
            var desctoken = "ADMIRAL_SKGUN_SKILL_DESC";
            var namestr = "Valiant Blaster";
            LanguageAPI.Add(nametoken, namestr);
            //todo: update this from config
            LanguageAPI.Add(desctoken, "Fire a rapid combo of up to 3 slow-moving explosive orbs for <style=cIsDamage>1x500%, 1x500%, and 1x800% damage</style>. <style=cIsUtility>Fully charge</style> to fire a faster, heavier round for <style=cIsDamage>1x2400% damage</style>. Must <style=cDeath>stand still to reload</style> after firing a 3rd or charged shot -- cancel the combo to stay mobile.");
            
            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;
            
            var projPfbPfb = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/VagrantCannon"));
            projPfbPfb.GetComponent<ProjectileSimple>().velocity = 150f;
            projPfbPfb.GetComponent<ProjectileSimple>().lifetime = 3f;
            projPfbPfb.GetComponent<ProjectileSimple>().enableVelocityOverLifetime = false;
            projPfbPfb.GetComponent<SphereCollider>().radius = 0.5f;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().blastRadius = 12f;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().blastDamageCoefficient = 1f;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().lifetime = 3f;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().bonusBlastForce = Vector3.zero;
            var eff = PrefabAPI.InstantiateClone(projPfbPfb.GetComponent<ProjectileImpactExplosion>().impactEffect, "CaptainSkGunChargedImpactEffect", false);
            foreach(Transform c in eff.transform) {
                c.localScale *= 12f/8f;
            }
            EffectAPI.AddEffect(eff);
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().impactEffect = eff;
            chargedProjectilePrefab = PrefabAPI.InstantiateClone(projPfbPfb, "CaptainSkGunChargedProjectile");
            
            projPfbPfb.GetComponent<ProjectileSimple>().velocity = 35f;
            projPfbPfb.GetComponent<SphereCollider>().radius = 0.25f;
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().blastRadius = 5f;
            eff = PrefabAPI.InstantiateClone(projPfbPfb.GetComponent<ProjectileImpactExplosion>().impactEffect, "CaptainSkGunImpactEffect", false);
            foreach(Transform c in eff.transform) {
                c.localScale *= 5f/8f;
            }
            EffectAPI.AddEffect(eff);
            projPfbPfb.GetComponent<ProjectileImpactExplosion>().impactEffect = eff;
            var ghost = PrefabAPI.InstantiateClone(projPfbPfb.GetComponent<ProjectileController>().ghostPrefab, "CaptainSkGunProjectileGhost", false);
            GameObject.Destroy(ghost.transform.Find("Mesh").GetComponent<ObjectScaleCurve>());
            ghost.transform.Find("Mesh").localScale = Vector3.one * 2f;
            ghost.transform.Find("Spit, World").localScale = Vector3.one * 0.5f;
            projPfbPfb.GetComponent<ProjectileController>().ghostPrefab = ghost;
            projectilePrefab = PrefabAPI.InstantiateClone(projPfbPfb, "CaptainSkGunProjectile");


            skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateChargeSKGun>();
            skillDef.interruptPriority = InterruptPriority.Skill;
            skillDef.baseRechargeInterval = recoveryTime;
            skillDef.baseMaxStock = 3;
            skillDef.rechargeStock = 3;
            skillDef.isBullets = false;
            skillDef.shootDelay = 0.2f;
            skillDef.beginSkillCooldownOnSkillEnd = true;
            skillDef.requiredStock = 1;
            skillDef.stockToConsume = 1;
            skillDef.isCombatSkill = true;
            skillDef.noSprint = true;
            skillDef.canceledFromSprinting = false;
            skillDef.mustKeyPress = true;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = true;

            skillDef.skillName = namestr;
            skillDef.skillNameToken = nametoken;
            skillDef.skillDescriptionToken = desctoken;
            skillDef.icon = Resources.Load<Sprite>("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralSKGunSkill.png");

            LoadoutAPI.AddSkillDef(skillDef);

            UnlockablesAPI.AddUnlockable<AdmiralSKGunAchievement>(false);
            LanguageAPI.Add("ADMIRAL_SKGUN_ACHIEVEMENT_NAME", "Captain: Well-Seasoned");
            LanguageAPI.Add("ADMIRAL_SKGUN_ACHIEVEMENT_DESCRIPTION", "As Captain, hit with Vulcan Shotgun 600 TOTAL times.");

            shotgunKillsStatDef = RoR2.Stats.StatDef.Register("admiralSKGunAchievementProgress", RoR2.Stats.StatRecordType.Sum, RoR2.Stats.StatDataType.ULong, 0);

            slowSkillDebuff = BuffAPI.Add(new CustomBuff("AdmiralHeavyWeaponDebuff", "@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralHeavyWeaponDebuff.png", Color.yellow, true, false));
        }

        internal override void Install() {
            base.Install();

            Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainPrimarySkillFamily").AddVariant(skillDef, "ADMIRAL_SKGUN_UNLOCKABLE_ID");

            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        internal override void Uninstall() {
            base.Uninstall();

            Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainPrimarySkillFamily").RemoveVariant(skillDef);

            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        }
        
        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) {
            orig(self);
            if(self.HasBuff(slowSkillDebuff)) {
                self.moveSpeed = 0f;
                self.acceleration = 80f;
                self.jumpPower = 0;
                self.maxJumpHeight = 0;
            }
        }

        private void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(projectilePrefab);
            entries.Add(chargedProjectilePrefab);
        }
    }

    public class AdmiralSKGunAchievement : ModdedUnlockableAndAchievement<CustomSpriteProvider> {
        public override string AchievementIdentifier => "ADMIRAL_SKGUN_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => "ADMIRAL_SKGUN_UNLOCKABLE_ID";
        public override string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public override string AchievementNameToken => "ADMIRAL_SKGUN_ACHIEVEMENT_NAME";
        public override string AchievementDescToken => "ADMIRAL_SKGUN_ACHIEVEMENT_DESCRIPTION";
        public override string UnlockableNameToken => "ADMIRAL_SKGUN_SKILL_NAME";
        protected override CustomSpriteProvider SpriteProvider => new CustomSpriteProvider("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralSKGunSkill.png");

        public override bool wantsBodyCallbacks => true;

        public override int LookUpRequiredBodyIndex() {
            return BodyCatalog.FindBodyIndex("CaptainBody");
        }

        public override float ProgressForAchievement() {
            return userProfile.statSheet.GetStatValueULong(SKGunSkill.instance.shotgunKillsStatDef)/600f;
        }

        public override void OnInstall() {
            base.OnInstall();
            On.RoR2.BulletAttack.ProcessHitList += BulletAttack_ProcessHitList;
        }

        public override void OnUninstall() {
            base.OnUninstall();
            On.RoR2.BulletAttack.ProcessHitList -= BulletAttack_ProcessHitList;
        }

        private GameObject BulletAttack_ProcessHitList(On.RoR2.BulletAttack.orig_ProcessHitList orig, BulletAttack self, List<BulletAttack.BulletHit> hits, ref Vector3 endPosition, List<GameObject> ignoreList) {
            var retv = orig(self, hits, ref endPosition, ignoreList);
            if(retv == null) return retv;
            var ownerBody = self.owner ? self.owner.GetComponent<CharacterBody>() : null;
            if(!ownerBody || ownerBody.bodyIndex != LookUpRequiredBodyIndex()) return retv;
            var hc = retv.GetComponent<HealthComponent>();
            if(!hc) return retv;
            var tc = retv.GetComponent<TeamComponent>();
            if(!tc || !ownerBody.teamComponent || tc.teamIndex == ownerBody.teamComponent.teamIndex) return retv;
            Debug.Log($"hit! {userProfile.statSheet.GetStatValueULong(SKGunSkill.instance.shotgunKillsStatDef)}");
            userProfile.statSheet.PushStatValue(SKGunSkill.instance.shotgunKillsStatDef, 1UL);
            if(ProgressForAchievement() >= 1.0f)
                Grant();
            return retv;
        }
    }
}