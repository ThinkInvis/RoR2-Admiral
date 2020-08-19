using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;
using EntityStates.Captain.Weapon;
using EntityStates;

namespace ThinkInvisible.Admiral {
    public class EntStateCallJumpPad : BaseSkillState {
		public override void OnEnter() {
			base.OnEnter();
			if(base.isAuthority) {
				switch(this.activatorSkillSlot.stock) {
				case 0:
					this.outer.SetNextState(new EntStateJumpPad2());
					return;
				case 1:
					this.outer.SetNextState(new EntStateJumpPad1());
					return;
				default:
					Debug.LogError("Admiral: jump pad skill has invalid stock count!");
					this.outer.SetNextState(new EntStateJumpPad1());
					break;
				}
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority() {
			return InterruptPriority.PrioritySkill;
		}
	}

	public class EntStateJumpPad2 : CallAirstrike2 {}
	public class EntStateJumpPad1 : CallAirstrike1 {}
}