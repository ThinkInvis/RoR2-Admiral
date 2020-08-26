using RoR2;
using R2API.Utils;
using MonoMod.RuntimeDetour;

namespace ThinkInvisible.Admiral {
    public class OrbitalSkillsAnywhere : AdmiralModule<OrbitalSkillsAnywhere> {
        Hook CUOSHook;
        internal override void Setup() {
            base.Setup();
            var origCUOSGet = typeof(RoR2.CaptainSupplyDropController).GetMethodCached("get_canUseOrbitalSkills");
            var newCUOSGet = typeof(OrbitalSkillsAnywhere).GetMethodCached(nameof(Hook_Get_CanUseOrbitalSkills));
            CUOSHook = new Hook(origCUOSGet, newCUOSGet, new HookConfig{ManualApply=true});
        }

        public override string configDescription => "Allows orbital skills to be used anywhere except Bazaar.";

        internal override void Install() {
            base.Install();
            CUOSHook.Apply();
        }

        internal override void Uninstall() {
            base.Uninstall();
            CUOSHook.Undo();
        }
        
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0060:Remove unused parameters", Justification = "Used by MonoMod.RuntimeDetour")]
        private static bool Hook_Get_CanUseOrbitalSkills(CaptainSupplyDropController self) => SceneCatalog.mostRecentSceneDef.baseSceneName != "bazaar";
    }
}
