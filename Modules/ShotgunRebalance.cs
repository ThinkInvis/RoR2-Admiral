using R2API;
using RoR2;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShotgunRebalance : T2Module<ShotgunRebalance> {
        [AutoConfigRoOIntSlider("{0:N0}", 1, 30)]
        [AutoConfig("Pellet count for Vulcan Shotgun.",
            AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
        public int pelletCount {get; private set;} = 6;

        public override bool managedEnable => true;
        public override string enabledConfigDescription => "Reduces Vulcan Shotgun pellet count.";
        public override AutoConfigUpdateActionTypes enabledConfigUpdateActionTypes => AutoConfigUpdateActionTypes.InvalidateLanguage;

        public override void InstallLanguage() {
            genericLanguageTokens["CAPTAIN_PRIMARY_DESCRIPTION"] = "Fire a blast of pellets that deal <style=cIsDamage>6x120% damage</style> with no falloff. Charging the attack narrows the <style=cIsUtility>spread</style>.";
            base.InstallLanguage();
        }

        public override void Install() {
            base.Install();
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet += On_FCSModifyBullet;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ctor += On_FireCaptainShotgunCtor;
        }

        public override void Uninstall() {
            base.Uninstall();
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet -= On_FCSModifyBullet;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ctor -= On_FireCaptainShotgunCtor;
        }

        private void On_FCSModifyBullet(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_ModifyBullet orig, EntityStates.Captain.Weapon.FireCaptainShotgun self, BulletAttack bulletAttack) {
            orig(self, bulletAttack);
            bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
        }

        private void On_FireCaptainShotgunCtor(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_ctor orig, EntityStates.Captain.Weapon.FireCaptainShotgun self) {
            orig(self);
            self.bulletCount = pelletCount;
        }
    }
}
