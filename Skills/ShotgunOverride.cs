using RoR2;
using BepInEx.Configuration;
using R2API.Utils;

namespace ThinkInvisible.Admiral {
    public static class ShotgunOverride {
        public static float fireDelayDynamic {get; private set;}
        public static float fireDelayFixed {get; private set;}

        internal static void Patch() {
            var cfgFireDelayDynamic = AdmiralPlugin.cfgFile.Bind(new ConfigDefinition("Admiral", "FireDelayDynamic"), 0.2f, new ConfigDescription(
                "Time, in fraction of total charge time, to wait before autofiring Vulcan Shotgun after reaching full charge. Set both this and FireDelayFixed to 0 to disable autofire.",
                new AcceptableValueRange<float>(0f,float.MaxValue)));
            fireDelayDynamic = 1f + cfgFireDelayDynamic.Value;

            var cfgFireDelayFixed = AdmiralPlugin.cfgFile.Bind(new ConfigDefinition("Admiral", "FireDelayFixed"), 0f, new ConfigDescription(
                "Absolute minimum time, in seconds, to wait before autofiring Vulcan Shotgun after reaching full charge. Set both this and FireDelayDynamic to 0 to disable autofire.",
                new AcceptableValueRange<float>(0f,float.MaxValue)));
            fireDelayFixed = cfgFireDelayFixed.Value;

            if(fireDelayFixed > 0f || fireDelayDynamic > 0f)
                On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate += On_CapChargeShotgunFixedUpdate;
        }

        private static void On_CapChargeShotgunFixedUpdate(On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.orig_FixedUpdate orig, EntityStates.Captain.Weapon.ChargeCaptainShotgun self) {
            if(Util.HasEffectiveAuthority(self.outer.networkIdentity)) {
                var fixedAge = (float)typeof(EntityStates.EntityState).GetPropertyCached("fixedAge").GetValue(self);
                var chargeDuration = self.GetFieldValue<float>("chargeDuration");
                if(fixedAge / chargeDuration > fireDelayDynamic && fixedAge - chargeDuration > fireDelayFixed) self.SetFieldValue<bool>("released", true);
            }
            orig(self);
        }
    }
}
