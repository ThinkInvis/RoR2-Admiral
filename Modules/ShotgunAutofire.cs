using RoR2;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShotgunAutofire : T2Module<ShotgunAutofire> {
        [AutoConfig("Time, in fraction of total charge time, to wait before autofiring Vulcan Shotgun after reaching full charge.",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float fireDelayDynamic {get; private set;} = 0.2f;

        [AutoConfig("Absolute minimum time, in seconds, to wait before autofiring Vulcan Shotgun after reaching full charge.",
            AutoConfigFlags.None, 0f, float.MaxValue)]
        public float fireDelayFixed {get; private set;} = 0f;

        public override bool managedEnable => true;
        public override string enabledConfigDescription => "Causes Vulcan Shotgun to autofire. Client-side.";
        public override AutoConfigFlags enabledConfigFlags => AutoConfigFlags.None;

        public override void Install() {
            base.Install();
            On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate += On_CapChargeShotgunFixedUpdate;
        }

        public override void Uninstall() {
            base.Uninstall();
            On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate -= On_CapChargeShotgunFixedUpdate;
        }

        private void On_CapChargeShotgunFixedUpdate(On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.orig_FixedUpdate orig, EntityStates.Captain.Weapon.ChargeCaptainShotgun self) {
            if(Util.HasEffectiveAuthority(self.outer.networkIdentity)) {
                if(self.fixedAge / self.chargeDuration > fireDelayDynamic + 1f && self.fixedAge - self.chargeDuration > fireDelayFixed) self.released = true;
            }
            orig(self);
        }
    }
}
