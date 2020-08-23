using BepInEx.Configuration;
using System;
using System.Linq;
using System.Reflection;
using TILER2;
using static TILER2.MiscUtil;

namespace ThinkInvisible.Admiral {
    public abstract class RuntimeAdmiralSubmodule<T>:RuntimeAdmiralSubmodule where T : RuntimeAdmiralSubmodule<T> {
        public static T instance {get;private set;}

        public RuntimeAdmiralSubmodule() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting RuntimeAdmiralSubmodule was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class RuntimeAdmiralSubmodule : BaseAdmiralSubmodule {
        [AutoItemConfig("If false, this module will be disabled and none of its changes will take effect.",
            AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferUntilEndGame)]
        public bool enabled {get; internal set;} = true;

        internal override void Setup() {
            base.Setup();
            ConfigEntryChanged += (sender, args) => {
                if(args.target.boundProperty.Name == nameof(enabled)) {
                    if((bool)args.newValue == true) Install();
                    else Uninstall();
                }
            };
        }
    }

    public abstract class AdmiralSubmodule<T>:AdmiralSubmodule where T : AdmiralSubmodule<T> {
        public static T instance {get;private set;}

        public AdmiralSubmodule() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting AdmiralSubmodule was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class AdmiralSubmodule : BaseAdmiralSubmodule {
        [AutoItemConfig("If false, this module will be disabled and none of its changes will take effect.",
            AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferForever)]
        public bool enabled {get; internal set;} = true;

        internal override void Setup() {
            base.Setup();
            ConfigEntryChanged += (sender, args) => {
                if(args.target.boundProperty.Name == nameof(enabled)) {
                    if((bool)args.newValue == true) Install();
                    else Uninstall();
                }
            };
        }
    }

    public abstract class BaseAdmiralSubmodule<T>:BaseAdmiralSubmodule where T : BaseAdmiralSubmodule<T> {
        public static T instance {get;private set;}

        public BaseAdmiralSubmodule() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting BaseAdmiralSubmodule was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class BaseAdmiralSubmodule : AutoItemConfigContainer {
        internal static FilingDictionary<BaseAdmiralSubmodule> allModules = new FilingDictionary<BaseAdmiralSubmodule>();

        internal BaseAdmiralSubmodule() {
            allModules.Add(this);
        }

        internal virtual void Setup() {}

        internal virtual void Install() {}

        internal virtual void Uninstall() {}

        internal static FilingDictionary<BaseAdmiralSubmodule> InitAll(string modDisplayName, ConfigFile cfl) {
            FilingDictionary<BaseAdmiralSubmodule> f = new FilingDictionary<BaseAdmiralSubmodule>();
            foreach(Type type in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseAdmiralSubmodule)))) {
                var newBpl = (BaseAdmiralSubmodule)Activator.CreateInstance(type);
                newBpl.BindAll(cfl, modDisplayName, "Modules." + newBpl.GetType().Name);
                f.Add(newBpl);
            }
            return f;
        }
    }
}
