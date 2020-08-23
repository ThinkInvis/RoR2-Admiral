using RoR2;
using BepInEx.Configuration;
using R2API.Utils;
using R2API;
using MonoMod.RuntimeDetour;

namespace ThinkInvisible.Admiral {
    public class OrbitalSkillsAnywhere : AdmiralSubmodule<OrbitalSkillsAnywhere> {
        Hook CUOSHook;
        internal override void Setup() {
            base.Setup();
            var origCUOSGet = typeof(RoR2.CaptainSupplyDropController).GetMethodCached("get_canUseOrbitalSkills");
            var newCUOSGet = typeof(OrbitalSkillsAnywhere).GetMethodCached(nameof(Hook_Get_CanUseOrbitalSkills));
            CUOSHook = new Hook(origCUOSGet, newCUOSGet, new HookConfig{ManualApply=true});
        }

        internal override void Install() {
            base.Install();
            CUOSHook.Apply();
        }

        internal override void Uninstall() {
            base.Uninstall();
            CUOSHook.Undo();
        }

        private static bool Hook_Get_CanUseOrbitalSkills(CaptainSupplyDropController self) => SceneCatalog.mostRecentSceneDef.baseSceneName != "bazaar";
    }
}
