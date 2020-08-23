using UnityEngine;
using EntityStates.Captain.Weapon;
using EntityStates;

namespace ThinkInvisible.Admiral {
    public class EntStateCallJumpPad : BaseSkillState {
		public override void OnEnter() {
			base.OnEnter();
			if(base.isAuthority) {
				switch(this.activatorSkillSlot.stock) {
				case 0:
					var ns2 = new CallAirstrike2();
					ns2.projectilePrefab = OrbitalJumpPadSkill.instance.jumpPadPrefabProj2;
					ns2.maxDistance = 100;
					this.outer.SetNextState(ns2);
					return;
				case 1:
					var ns1 = new CallAirstrike1();
					ns1.projectilePrefab = OrbitalJumpPadSkill.instance.jumpPadPrefabProj1;
					ns1.maxDistance = 100;
					this.outer.SetNextState(ns1);
					return;
				default:
					Debug.LogError("Admiral: jump pad skill has invalid stock count!");
					break;
				}
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority() {
			return InterruptPriority.PrioritySkill;
		}
	}
}