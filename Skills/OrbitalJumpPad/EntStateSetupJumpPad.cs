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
    public class EntStateSetupJumpPad : SetupAirstrike {
        public override void OnEnter() {
            var oldSkillDef = primarySkillDef;
            primarySkillDef = OrbitalJumpPadSkill.callSkillDef;
            base.OnEnter();
            primarySkillDef = oldSkillDef;
        }
        public override void OnExit() {
            var oldSkillDef = primarySkillDef;
            primarySkillDef = OrbitalJumpPadSkill.callSkillDef;
            base.OnExit();
            primarySkillDef = oldSkillDef;
        }
    }
}
