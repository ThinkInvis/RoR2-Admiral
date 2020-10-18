using RoR2;
using R2API.Utils;
using MonoMod.RuntimeDetour;
using RoR2.Skills;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class OrbitalSkillsAnywhere : T2Module<OrbitalSkillsAnywhere> {
        Hook CUOSHook;
        public override void SetupBehavior() {
            base.SetupBehavior();
            var origCUOSGet = typeof(CaptainOrbitalSkillDef).GetMethodCached("get_isAvailable");
            var newCUOSGet = typeof(OrbitalSkillsAnywhere).GetMethodCached(nameof(Hook_Get_IsAvailable));
            CUOSHook = new Hook(origCUOSGet, newCUOSGet, new HookConfig{ManualApply=true});
        }

        public override string enabledConfigDescription => "Allows orbital skills to be used anywhere except Bazaar.";

        public override void Install() {
            base.Install();
            CUOSHook.Apply();
        }

        public override void Uninstall() {
            base.Uninstall();
            CUOSHook.Undo();
        }
        
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0060:Remove unused parameters", Justification = "Used by MonoMod.RuntimeDetour")]
        private static bool Hook_Get_IsAvailable(CaptainOrbitalSkillDef self) => SceneCatalog.mostRecentSceneDef.baseSceneName != "bazaar";
    }
}
