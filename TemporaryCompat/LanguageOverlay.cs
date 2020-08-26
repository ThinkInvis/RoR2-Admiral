using MonoMod.Utils;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ThinkInvisible.Admiral {
    /// <summary>
    /// Partial port of an upcoming R2API.LanguageAPI feature.
    /// </summary>
    public static class LanguageAPIBleedingEdge {
        internal static void Patch() {
            Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
        }

        private static void OnCurrentLanguageChanged() {
            var currentLanguage = Language.currentLanguage;
            if (currentLanguage is null)
                return;

            GenericOverlays.Clear();
            LanguageSpecificOverlays.Clear();
            onSetupLanguageOverlays?.Invoke();

            currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(GenericOverlays);
                
            if (LanguageSpecificOverlays.TryGetValue(currentLanguage.name, out var languageSpecificOverlayDic)) {
                currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(languageSpecificOverlayDic);
            }
        }

        private static Dictionary<string, string> ReplaceAndAddRange(this Dictionary<string, string> dict, Dictionary<string, string> other) {
            dict = dict.Where(kvp => !other.ContainsKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            dict.AddRange(other);

            return dict;
        }

        internal static Dictionary<string, string> GenericOverlays = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificOverlays = new Dictionary<string, Dictionary<string, string>>();

        internal delegate void SetupLanguageOverlays();
        internal static event SetupLanguageOverlays onSetupLanguageOverlays;

        /// <summary>
        /// Manages temporary language token changes.
        /// </summary>
        public class LanguageOverlay {
            private readonly OverlayTokenData[] overlays;
            /// <summary>Contains information about the language token changes this LanguageOverlay makes.</summary>
            public readonly ReadOnlyCollection<OverlayTokenData> readOnlyOverlays;

            internal LanguageOverlay(OverlayTokenData[] _overlays) {
                overlays = _overlays;
                readOnlyOverlays = new ReadOnlyCollection<OverlayTokenData>(overlays);
            }

            internal LanguageOverlay(OverlayTokenData _singleOverlay) {
                overlays = new OverlayTokenData[]{_singleOverlay};
                readOnlyOverlays = new ReadOnlyCollection<OverlayTokenData>(overlays);
            }

            internal void Add() {
                onSetupLanguageOverlays += LanguageOverlay_onSetupLanguageOverlays;
            }

            /// <summary>Undoes this LanguageOverlay's language token changes; you may safely dispose it afterwards. Requires a language reload to take effect.</summary>
            public void Remove() {
                onSetupLanguageOverlays -= LanguageOverlay_onSetupLanguageOverlays;
            }

            private void LanguageOverlay_onSetupLanguageOverlays() {
                foreach(var overlay in overlays) {
                    Dictionary<string, string> targetDict;
                    if(overlay.isGeneric) {
                        targetDict = GenericOverlays;
                    } else {
                        if(!LanguageSpecificOverlays.ContainsKey(overlay.lang))
                            LanguageSpecificOverlays.Add(overlay.lang, new Dictionary<string, string>());
                        targetDict = LanguageSpecificOverlays[overlay.lang];
                    }
                    targetDict[overlay.key] = overlay.value;
                }
            }
        }

        /// <summary>
        /// Adds a single temporary language token, and its associated value, to all languages. Please add multiple instead (dictionary- or file-based signatures) where possible. Language-specific tokens, as well as overlays added later in time, will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string key, string value) {
            var overlay = new LanguageOverlay(new OverlayTokenData(key, value));
            overlay.Add();
            return overlay;
        }
        
        /// <summary>
        /// Adds a single temporary language token, and its associated value, to a specific language. Please add multiple instead (dictionary- or file-based signatures) where possible. Overlays added later in time will take precedence. Call LanguageOverlay.Remove() on the result to undo your change to this language token.
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="lang">Language you want to add this to</param>
        /// <returns>A LanguageOverlay representing your language addition/override; call .Remove() on it to undo the change. May be safely disposed after calling .Remove().</returns>
        public static LanguageOverlay AddOverlay(string key, string value, string lang) {
            var overlay = new LanguageOverlay(new OverlayTokenData(key, value, lang));
            overlay.Add();
            return overlay;
        }

        /// <summary>
        /// Contains information about a single temporary language token change.
        /// </summary>
        public struct OverlayTokenData {
            /// <summary>The token identifier to add/replace the value of.</summary>
            public string key;
            /// <summary>The value to set the target token to.</summary>
            public string value;
            /// <summary>The language which the target token belongs to, if isGeneric = false.</summary>
            public string lang;
            /// <summary>Whether the target token is generic (applies to all languages which don't contain the token).</summary>
            public bool isGeneric;

            internal OverlayTokenData(string _key, string _value, string _lang) {
                key = _key;
                value = _value;
                lang = _lang;
                isGeneric = false;
            }
            internal OverlayTokenData(string _key, string _value) {
                key = _key;
                value = _value;
                lang = "";
                isGeneric = true;
            }
        }
    }
}
