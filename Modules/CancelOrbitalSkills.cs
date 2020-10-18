using RoR2;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class CancelOrbitalSkills : T2Module<CancelOrbitalSkills> {
        public override string enabledConfigDescription => "Allows orbital skills to be cancelled by reactivating the skill.";

        public override void Install() {
            base.Install();
            On.RoR2.GenericSkill.ExecuteIfReady += GenericSkill_ExecuteIfReady;
        }

        public override void Uninstall() {
            base.Uninstall();
            On.RoR2.GenericSkill.ExecuteIfReady -= GenericSkill_ExecuteIfReady;
        }

        private bool GenericSkill_ExecuteIfReady(On.RoR2.GenericSkill.orig_ExecuteIfReady orig, GenericSkill self) {
            var retv = orig(self);
            if(retv || !self.stateMachine || self.stateMachine.HasPendingState()) return retv;

            if(self.stateMachine.state.GetType() == self.activationState.stateType &&
                (self.stateMachine.state is EntityStates.Captain.Weapon.SetupAirstrike || self.stateMachine.state is EntityStates.Captain.Weapon.SetupSupplyDrop))
                self.stateMachine.SetNextStateToMain();

            return false;
        }
    }
}
