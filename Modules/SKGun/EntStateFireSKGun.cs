using RoR2;
using EntityStates.Captain.Weapon;
using EntityStates;
using RoR2.Projectile;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public class EntStateFireSKGun : BaseState {
        float duration;
        float firingDelay;
        bool isCharged;
        bool usedAllAmmo;

        public override void OnEnter() {
            isCharged = characterBody.spreadBloomAngle <= 2f;
            base.OnEnter();

            if(isCharged || skillLocator.primary.stock == 0) {
                usedAllAmmo = true;

                skillLocator.primary.stock = 0;
                skillLocator.primary.rechargeStopwatch = SKGunSkill.recoveryTime - SKGunSkill.fullReloadTime / attackSpeedStat;
                duration = SKGunSkill.fullReloadTime / attackSpeedStat;
                firingDelay = (isCharged ? 0.35f : 0.2f) / attackSpeedStat;
                outer.commonComponents.characterBody.AddTimedBuff(SKGunSkill.instance.slowSkillDebuff, duration);
            } else {
                skillLocator.primary.rechargeStopwatch = SKGunSkill.recoveryTime * (1f - 1f / attackSpeedStat);
                duration = SKGunSkill.recoveryTime / attackSpeedStat;
                firingDelay = 0f;
            }

            firingDelay = Mathf.Min(firingDelay, duration);

            var aimRay = GetAimRay();
			StartAimMode(aimRay, duration + 2f, false);
        }

        bool hasFired = false;
        public override void FixedUpdate() {
			base.FixedUpdate();
            if(isAuthority && fixedAge >= firingDelay && !hasFired) {
                Fire();
                hasFired = true;
            }
			if(fixedAge >= duration) {
			    PlayAnimation("Gesture, Override", "BufferEmpty");
                outer.SetNextStateToMain();
            }
		}

        private void Fire() {
            var copyState = new FireCaptainShotgun();
			PlayAnimation("Gesture, Additive", "FireCaptainShotgun");
            PlayAnimation("Gesture, Override", "FireCaptainShotgun");

			Util.PlaySound(isCharged ? FireCaptainShotgun.tightSoundString : FireCaptainShotgun.wideSoundString, gameObject);
            if(FindModelChild(copyState.muzzleName)) EffectManager.SimpleMuzzleFlash(copyState.muzzleFlashPrefab, gameObject, copyState.muzzleName, false);

            var aimRay = GetAimRay();

            if(usedAllAmmo && outer.commonComponents.characterMotor)
                outer.commonComponents.characterMotor.ApplyForce(outer.commonComponents.characterMotor.mass * (isCharged ? -35f : -20f) * aimRay.direction, true, false);
            
            if(!isAuthority) return;

            ProjectileManager.instance.FireProjectile(isCharged ? SKGunSkill.instance.chargedProjectilePrefab : SKGunSkill.instance.projectilePrefab,
                aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction),
                gameObject,
                damageStat * (isCharged ? 18f : (usedAllAmmo ? 8f : 5f)),
                isCharged ? 2f : 10f, Util.CheckRoll(critStat, characterBody.master));
        }

        public override InterruptPriority GetMinimumInterruptPriority() => usedAllAmmo ? InterruptPriority.PrioritySkill : InterruptPriority.Skill;
    }
}
