using R2API;
using RoR2;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShotgunRebalance : AdmiralModule<ShotgunRebalance> {
        [AutoItemConfig("Pellet count for Vulcan Shotgun.",
            AutoItemConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
        public int pelletCount {get; private set;} = 6;

        public override string configDescription => "Reduces Vulcan Shotgun pellet count.";
        public override bool invalidatesLanguage => true;

        internal override void InstallLang() {
            base.InstallLang();
            languageOverlays.Add(LanguageAPI.AddOverlay("CAPTAIN_PRIMARY_DESCRIPTION", "Fire a blast of pellets that deal <style=cIsDamage>6x120% damage</style> with no falloff. Charging the attack narrows the <style=cIsUtility>spread</style>."));
        }

        internal override void Install() {
            base.Install();
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet += On_FCSModifyBullet;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ctor += On_FireCaptainShotgunCtor;
        }
        
        internal override void Uninstall() {
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
