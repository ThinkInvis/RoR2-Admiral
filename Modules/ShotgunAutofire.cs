using RoR2;
using TILER2;

namespace ThinkInvisible.Admiral {
    public class ShotgunAutofire : RuntimeAdmiralSubmodule<ShotgunAutofire> {
        [AutoItemConfig("Time, in fraction of total charge time, to wait before autofiring Vulcan Shotgun after reaching full charge.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float fireDelayDynamic {get; private set;} = 0.2f;

        [AutoItemConfig("Absolute minimum time, in seconds, to wait before autofiring Vulcan Shotgun after reaching full charge.",
            AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float fireDelayFixed {get; private set;} = 0f;

        internal override void Install() {
            base.Install();
            On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate += On_CapChargeShotgunFixedUpdate;
        }
        
        internal override void Uninstall() {
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
