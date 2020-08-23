using EntityStates.Captain.Weapon;

namespace ThinkInvisible.Admiral {
    public class EntStateSetupJumpPad : SetupAirstrike {
        public override void OnEnter() {
            var oldSkillDef = primarySkillDef;
            primarySkillDef = OrbitalJumpPadSkill.instance.callSkillDef;
            base.OnEnter();
            primarySkillDef = oldSkillDef;
        }
        public override void OnExit() {
            var oldSkillDef = primarySkillDef;
            primarySkillDef = OrbitalJumpPadSkill.instance.callSkillDef;
            base.OnExit();
            primarySkillDef = oldSkillDef;
        }
    }
}
