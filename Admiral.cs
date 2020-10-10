using BepInEx;
using R2API.Utils;
using UnityEngine;
using BepInEx.Configuration;
using R2API;
using System.Reflection;
using Path = System.IO.Path;
using TILER2;

namespace ThinkInvisible.Admiral {
    
    [BepInDependency("com.bepis.r2api", "2.5.14")]
    [BepInDependency(TILER2Plugin.ModGuid, "2.2.3")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI), nameof(UnlockablesAPI), nameof(R2API.Networking.NetworkingAPI), nameof(EffectAPI))]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "2.2.2";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        internal static BepInEx.Logging.ManualLogSource logger;

        internal static ConfigFile cfgFile;

        public void Awake() {
            logger = Logger;
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Admiral.admiral_assets")) {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@Admiral", bundle);
                ResourcesAPI.AddProvider(provider);
            }
            
            AdmiralModule.InitAll(cfgFile);

            foreach(var module in AdmiralModule.allModules) {
                module.Setup();
            }
        }

        public void Start() {
            foreach(var module in AdmiralModule.allModules) {
                if(module.enabled && module.managedEnable) {
                    module.Install();
                    module.InstallLang();
                }
            }
            RoR2.Language.CCLanguageReload(new RoR2.ConCommandArgs());
        }
    }
}