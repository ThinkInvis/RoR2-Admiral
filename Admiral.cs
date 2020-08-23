using BepInEx;
using R2API.Utils;
using UnityEngine;
using BepInEx.Configuration;
using R2API;
using System.Reflection;
using Path = System.IO.Path;
using TILER2;

namespace ThinkInvisible.Admiral {
    
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency(TILER2Plugin.ModGuid)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI), nameof(UnlockablesAPI), nameof(R2API.Networking.NetworkingAPI))]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "1.5.3";
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
            
            BaseAdmiralSubmodule.InitAll("Admiral", cfgFile);

            foreach(var module in BaseAdmiralSubmodule.allModules) {
                module.Setup();
            }
            
            //BaseAdmiralSubmodule is inherited directly for dependents on other modules and shouldn't be installed directly during this stage
            foreach(var baseModule in BaseAdmiralSubmodule.allModules) {
                if(baseModule is AdmiralSubmodule module)
                    module.Install();
                else if(baseModule is RuntimeAdmiralSubmodule runtimeModule)
                    runtimeModule.Install();
            }
        }
    }
}