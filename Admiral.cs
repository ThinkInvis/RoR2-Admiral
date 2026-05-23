using BepInEx;
using R2API.Utils;
using UnityEngine;
using BepInEx.Configuration;
using R2API;
using System.Reflection;
using Path = System.IO.Path;

namespace ThinkInvisible.Admiral {
    
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class AdmiralPlugin:BaseUnityPlugin {
        public const string ModVer = "2.5.2";
        public const string ModName = "Admiral";
        public const string ModGuid = "com.ThinkInvisible.Admiral";
        
        internal static BepInEx.Logging.ManualLogSource _logger;
        internal static AssetBundle resources;

        internal static ConfigFile cfgFile;

        FilingDictionary<Module> allModules;

        public void Awake() {
            _logger = Logger;
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Admiral.admiral_assets")) {
                resources = AssetBundle.LoadFromStream(stream);
            }

            Module.SetupModuleClass();
            
            allModules = Module.InitModules(new Module.ModInfo {
                displayName = "Admiral",
                longIdentifier = "Admiral",
                shortIdentifier = "ADML",
                mainConfigFile = cfgFile
            });

            Module.SetupAll_PluginAwake(allModules);
        }

        public void Start() {
            Module.SetupAll_PluginStart(allModules);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity Engine.")]
        private void Update() {
            if(!RoR2.RoR2Application.loadFinished) return;
            AutoConfigModule.Update();
        }
    }
}