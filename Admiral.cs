using BepInEx;
using R2API.Utils;
using UnityEngine;
using BepInEx.Configuration;
using R2API;
using System.Reflection;
using Path = System.IO.Path;
using TILER2;
using static TILER2.MiscUtil;

namespace ThinkInvisible.Admiral {
    
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, TILER2Plugin.ModVer)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(LoadoutAPI), nameof(R2API.Networking.NetworkingAPI), nameof(UnlockableAPI))]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "2.5.1";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        internal static BepInEx.Logging.ManualLogSource logger;
        internal static AssetBundle resources;

        internal static ConfigFile cfgFile;

        FilingDictionary<T2Module> allModules;

        public void Awake() {
            logger = Logger;
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Admiral.admiral_assets")) {
                resources = AssetBundle.LoadFromStream(stream);
            }
            
            allModules = T2Module.InitModules(new T2Module.ModInfo {
                displayName = "Admiral",
                longIdentifier = "Admiral",
                shortIdentifier = "ADML",
                mainConfigFile = cfgFile
            });

            T2Module.SetupAll_PluginAwake(allModules);
        }

        public void Start() {
            T2Module.SetupAll_PluginStart(allModules);
        }
    }
}