using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;

namespace ThinkInvisible.Admiral {
    public abstract class AdmiralModule<T>:AdmiralModule where T : AdmiralModule<T> {
        public static T instance {get;private set;}

        protected AdmiralModule() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting AdmiralModule was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class AdmiralModule : AutoItemConfigContainer {
        internal static FilingDictionary<AdmiralModule> allModules = new FilingDictionary<AdmiralModule>();

        public bool enabled {get; internal set;} = true;

        public virtual string configDescription => null;
        public virtual bool addEnabledConfig => true;
        public virtual AutoItemConfigFlags enabledConfigFlags => AutoItemConfigFlags.PreventNetMismatch;
        public virtual bool invalidatesLanguage => false;
        
        protected List<R2API.LanguageAPI.LanguageOverlay> languageOverlays = new List<R2API.LanguageAPI.LanguageOverlay>();

        internal virtual void Setup() {
            ConfigEntryChanged += (sender, args) => {
                if(args.target.boundProperty.Name == nameof(enabled)) {
                    if((bool)args.newValue == true) {
                        Install();
                        InstallLang();
                    } else {
                        Uninstall();
                        UninstallLang();
                    }
                    if(invalidatesLanguage) Language.CCLanguageReload(new ConCommandArgs());
                }
            };
        }

        internal virtual void Install() {
        }

        internal virtual void Uninstall() {
        } 

        //Will be called once after initial language setup, and also if/when the module is installed after setup.
        internal virtual void InstallLang() {
        }

        //Will be called if/when the module is uninstalled after setup.
        internal virtual void UninstallLang() {
            foreach(var overlay in languageOverlays) {
                overlay.Remove();
            }
            languageOverlays.Clear();
        }

        internal static FilingDictionary<AdmiralModule> InitAll(ConfigFile cfl) {
            FilingDictionary<AdmiralModule> f = new FilingDictionary<AdmiralModule>();
            foreach(Type type in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AdmiralModule)))) {
                var newBpl = (AdmiralModule)Activator.CreateInstance(type);
                if(newBpl.addEnabledConfig)
                    newBpl.Bind(typeof(AdmiralModule).GetProperty(nameof(enabled)), cfl, "Admiral", "Modules." + newBpl.GetType().Name, new AutoItemConfigAttribute(
                    ((newBpl.configDescription != null) ? (newBpl.configDescription + "\n") : "") + "Set to False to disable this module and all of its content. Doing so may cause changes in other modules as well.",
                    newBpl.enabledConfigFlags));
                newBpl.BindAll(cfl, "Admiral", "Modules." + newBpl.GetType().Name);
                f.Add(newBpl);
            }
            return f;
        }

        protected AdmiralModule() {
            allModules.Add(this);
        } 
    }
}
