using RoR2;
using BepInEx.Configuration;
using R2API.Utils;
using R2API;

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

            if(fireDelayFixed > 0f || fireDelayDynamic > 1f)
                On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.FixedUpdate += On_CapChargeShotgunFixedUpdate;

            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet += On_FCSModifyBullet;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ctor += On_FireCaptainShotgunCtor;

            LanguageAPI.Add("CAPTAIN_PRIMARY_DESCRIPTION", "Fire a blast of pellets that deal <style=cIsDamage>6x120% damage</style> with no falloff. Charging the attack narrows the <style=cIsUtility>spread</style>.");
        }

        private static void On_FCSModifyBullet(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_ModifyBullet orig, EntityStates.Captain.Weapon.FireCaptainShotgun self, BulletAttack bulletAttack) {
            orig(self, bulletAttack);
            bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
        }

        private static void On_FireCaptainShotgunCtor(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_ctor orig, EntityStates.Captain.Weapon.FireCaptainShotgun self) {
            orig(self);
            self.bulletCount = 6;
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
