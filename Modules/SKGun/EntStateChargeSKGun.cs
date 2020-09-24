using UnityEngine;
using EntityStates.Captain.Weapon;
using EntityStates;
using RoR2;

namespace ThinkInvisible.Admiral {
    public class EntStateChargeSKGun : ChargeCaptainShotgun {
		public override void OnEnter() {
			base.OnEnter();
			this.chargeDuration = Mathf.Max(this.chargeDuration, SKGunSkill.instance.minChargeTime);
		}

		public override void FixedUpdate() {
			fixedAge += Time.fixedDeltaTime;
			characterBody.SetAimTimer(1f);
			characterBody.isSprinting = false; //slow down please
			if(fixedAge >= chargeDuration) {
				if(chargeupVfxGameObject) {
					EntityState.Destroy(chargeupVfxGameObject);
					chargeupVfxGameObject = null;
				}
				if(!holdChargeVfxGameObject && muzzleTransform)
					holdChargeVfxGameObject = GameObject.Instantiate(holdChargeVfxPrefab, muzzleTransform);
			}
			if(!isAuthority) return;
			if(!released && !(inputBank?.skill1.down ?? false)) released = true;
			/*if(ShotgunAutofire.instance.enabled) {
				if(Util.HasEffectiveAuthority(outer.networkIdentity)) {
					if(fixedAge / chargeDuration > ShotgunAutofire.instance.fireDelayDynamic + 1f && fixedAge - chargeDuration > ShotgunAutofire.instance.fireDelayFixed) released = true;
				}
			}*/
			if(released) outer.SetNextState(new EntStateFireSKGun());
		}
    }
}
