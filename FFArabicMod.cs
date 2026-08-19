using MelonLoader;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using HarmonyLib;
using TMPro;
using System.Text;
using System.Reflection;

namespace FFArabic
{
    public class FFArabicMod : MelonMod
    {
        private static Dictionary<string, string> translations;
        private static HashSet<string> loggedMissingKeys = new HashSet<string>();
        private static readonly char[] termSeparators = new[] { '/', '\\' };
        public static HashSet<string> loggedGlyphIssues = new HashSet<string>();
        public static TMP_FontAsset arabicFontAsset; // Bundled font asset
        public static TMP_FontAsset arabicSystemFontAsset; // System fallback font asset
        public static FFArabicMod instance;
        public static string ModDirectory;

        public override void OnInitializeMelon()
        {
            instance = this;
            ModDirectory = Path.GetDirectoryName(MelonAssembly.Location);
            LoggerInstance.Msg("FF Arabic Mod Initializing...");
            LoadTranslations();
            LoadFontAsset();
            ApplyHarmonyPatches();
        }

        private void ApplyHarmonyPatches()
        {
            var harmony = new HarmonyLib.Harmony("com.ffarabic.mod");
            LoggerInstance.Msg("Applying Harmony patches...");
            PatchLocalizationByTerm(harmony);

            // --- Patch for .text property setter ---
            try
            {
                var textSetter = typeof(TextMeshProUGUI).GetProperty("text", BindingFlags.Public | BindingFlags.Instance)?.GetSetMethod();
                if (textSetter != null)
                {
                    var prefix = new HarmonyMethod(typeof(TranslationPatches), nameof(TranslationPatches.StringPrefix));
                    harmony.Patch(textSetter, prefix);
                    LoggerInstance.Msg("Successfully patched TextMeshProUGUI.text (setter).");
                }
                else
                {
                    LoggerInstance.Warning("Could not find TextMeshProUGUI.text property setter.");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Error patching TextMeshProUGUI.text (setter): {e}");
            }
            
            // --- Patch for SetText(StringBuilder) method ---
            try
            {
                var setTextStringBuilder = typeof(TextMeshProUGUI).GetMethod("SetText", new Type[] { typeof(StringBuilder) });
                if (setTextStringBuilder != null)
                {
                    var prefix = new HarmonyMethod(typeof(TranslationPatches), nameof(TranslationPatches.StringBuilderPrefix));
                    harmony.Patch(setTextStringBuilder, prefix);
                    LoggerInstance.Msg("Successfully patched TextMeshProUGUI.SetText(StringBuilder).");
                }
                else
                {
                    LoggerInstance.Warning("Could not find TextMeshProUGUI.SetText(StringBuilder).");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Error patching TextMeshProUGUI.SetText(StringBuilder): {e}");
            }

            // --- Patch for SetText(string ...) overloads (used by tooltips in some UI paths) ---
            try
            {
                var prefix = new HarmonyMethod(typeof(TranslationPatches), nameof(TranslationPatches.StringSetTextPrefix));
                int patchedCount = 0;

                Type[] targetTypes = new[] { typeof(TextMeshProUGUI), typeof(TMP_Text) };
                foreach (Type targetType in targetTypes)
                {
                    MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.Name != "SetText")
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length > 0 && parameters[0].ParameterType == typeof(string))
                        {
                            harmony.Patch(method, prefix);
                            patchedCount++;
                        }
                    }
                }

                LoggerInstance.Msg($"Successfully patched {patchedCount} TMP SetText(string ...) overload(s).");
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Error patching TMP SetText(string ...) overloads: {e}");
            }
        }

        private void PatchLocalizationByTerm(HarmonyLib.Harmony harmony)
        {
            try
            {
                Type languageSourceDataType = AccessTools.TypeByName("I2.Loc.LanguageSourceData");
                if (languageSourceDataType == null)
                {
                    LoggerInstance.Warning("Could not find I2.Loc.LanguageSourceData. Key-based translation patch skipped.");
                    return;
                }

                var getTranslationMethod = AccessTools.Method(languageSourceDataType, "GetTranslation");
                if (getTranslationMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(TranslationPatches), nameof(TranslationPatches.LanguageSourceGetTranslationPostfix));
                    harmony.Patch(getTranslationMethod, postfix: postfix);
                    LoggerInstance.Msg("Successfully patched I2.Loc.LanguageSourceData.GetTranslation.");
                }
                else
                {
                    LoggerInstance.Warning("Could not find I2.Loc.LanguageSourceData.GetTranslation.");
                }

                var tryGetTranslationMethod = AccessTools.Method(languageSourceDataType, "TryGetTranslation");
                if (tryGetTranslationMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(TranslationPatches), nameof(TranslationPatches.LanguageSourceTryGetTranslationPostfix));
                    harmony.Patch(tryGetTranslationMethod, postfix: postfix);
                    LoggerInstance.Msg("Successfully patched I2.Loc.LanguageSourceData.TryGetTranslation.");
                }
                else
                {
                    LoggerInstance.Warning("Could not find I2.Loc.LanguageSourceData.TryGetTranslation.");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Error patching I2 localization methods: {e}");
            }
        }

        private void LoadTranslations()
        {
            string jsonPath = Path.Combine(ModDirectory, "translations_by_key.json");

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    var loadedTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    translations = loadedTranslations != null
                        ? new Dictionary<string, string>(loadedTranslations, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    LoggerInstance.Msg($"Successfully loaded {translations.Count} translations.");
                }
                catch (Exception e)
                {
                    LoggerInstance.Error($"Error loading translations: {e.Message}");
                }
            }
            else
            {
                LoggerInstance.Error("translations_by_key.json not found!");
            }
        }

        private void LoadFontAsset()
        {
            string fontPath = Path.Combine(ModDirectory, "arabic_font.ttf");

            try
            {
                if (File.Exists(fontPath))
                {
                    arabicFontAsset = TMP_FontAsset.CreateFontAsset(new Font(fontPath));
                }

                var systemFont = Font.CreateDynamicFontFromOSFont(
                    new string[] { "Tahoma", "Segoe UI", "Arial", "Traditional Arabic" },
                    64);
                if (systemFont != null)
                {
                    arabicSystemFontAsset = TMP_FontAsset.CreateFontAsset(systemFont);
                }

                if (arabicFontAsset != null)
                {
                    var fallbackAssets = new List<TMP_FontAsset>();

                    if (arabicSystemFontAsset != null)
                    {
                        EnsureFontGlyphs(arabicSystemFontAsset, ArabicFixer.GetPresentationFormsCharset());
                        fallbackAssets.Add(arabicSystemFontAsset);
                    }

                    EnsureFontGlyphs(arabicFontAsset, ArabicFixer.GetPresentationFormsCharset());
                    arabicFontAsset.fallbackFontAssetTable = fallbackAssets;
                    LoggerInstance.Msg("Successfully created bundled Arabic TMP font asset.");
                }

                if (arabicSystemFontAsset != null)
                {
                    EnsureFontGlyphs(arabicSystemFontAsset, ArabicFixer.GetPresentationFormsCharset());
                    if (arabicFontAsset != null)
                    {
                        arabicSystemFontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { arabicFontAsset };
                    }
                }

                if (arabicFontAsset == null && arabicSystemFontAsset != null)
                {
                    arabicFontAsset = arabicSystemFontAsset;
                }

                if (arabicFontAsset == null)
                {
                    LoggerInstance.Error("Failed to create Arabic TMP font asset from bundled and system fonts.");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Error dynamically creating font asset: {e.Message}");
            }
        }

        public static void EnsureFontGlyphs(TMP_FontAsset fontAsset, string text)
        {
            if (fontAsset == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                string missingCharacters;
                fontAsset.TryAddCharacters(text, out missingCharacters);
            }
            catch (Exception e)
            {
                if (instance != null)
                {
                    instance.LoggerInstance.Warning("Failed to add glyphs to TMP font asset: " + e.Message);
                }
            }
        }

        public static bool TryTranslateTerm(string key, out string translatedText)
        {
            translatedText = null;

            if (translations == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (translations.TryGetValue(key, out translatedText))
            {
                return true;
            }

            string trimmedKey = key.Trim();
            if (!string.Equals(trimmedKey, key, StringComparison.Ordinal) &&
                translations.TryGetValue(trimmedKey, out translatedText))
            {
                return true;
            }

            int separatorIndex = trimmedKey.LastIndexOfAny(termSeparators);
            if (separatorIndex >= 0 && separatorIndex < trimmedKey.Length - 1)
            {
                string shortKey = trimmedKey.Substring(separatorIndex + 1);
                if (translations.TryGetValue(shortKey, out translatedText))
                {
                    return true;
                }
            }

            return false;
        }
        
        private static void LogMissingTranslation(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || loggedMissingKeys.Contains(key))
            {
                return;
            }

            loggedMissingKeys.Add(key);

            if (instance != null && ModDirectory != null)
            {
                string shortKey = key.Trim();
                int separatorIndex = shortKey.LastIndexOfAny(termSeparators);
                if (separatorIndex >= 0 && separatorIndex < shortKey.Length - 1)
                {
                    shortKey = shortKey.Substring(separatorIndex + 1);
                }

                instance.LoggerInstance.Warning($"[FFArabic] Missing translation for key: '{key}' | shortKey: '{shortKey}'");

                try
                {
                    string untranslatedFilePath = Path.Combine(ModDirectory, "Untranslated.txt");
                    File.AppendAllText(untranslatedFilePath, key + " | shortKey=" + shortKey + Environment.NewLine);
                }
                catch (Exception e)
                {
                    instance.LoggerInstance.Error($"Failed to write to Untranslated.txt: {e.Message}");
                }
            }
        }

        public static string T(string key)
        {
            if (TryTranslateTerm(key, out string translatedText))
            {
                return translatedText;
            }

            // Log missing keys with their short form to make diagnosis easier.
            if (!string.IsNullOrWhiteSpace(key))
            {
                LogMissingTranslation(key);
            }

            return key; // Return the key if no translation is found
        }
    }

    public static class TranslationPatches
    {
        private static bool ShouldCheckGlyph(char c)
        {
            if (c == '\u0020')
            {
                return false;
            }

            return ArabicFixer.IsArabicChar(c) ||
                   ArabicFixer.IsArabicPresentationForm(c) ||
                   c == '\u060C' || c == '\u061B' || c == '\u061F';
        }

        private static bool FontHasCharacter(TMP_FontAsset font, char c)
        {
            if (font == null)
            {
                return false;
            }

            if (font.characterLookupTable != null && font.characterLookupTable.ContainsKey((uint)c))
            {
                return true;
            }

            if (font.fallbackFontAssetTable != null)
            {
                for (int i = 0; i < font.fallbackFontAssetTable.Count; i++)
                {
                    if (FontHasCharacter(font.fallbackFontAssetTable[i], c))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasUnsupportedGlyphs(TMP_FontAsset font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!ShouldCheckGlyph(c))
                {
                    continue;
                }

                if (!FontHasCharacter(font, c))
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogGlyphIssues(TMP_Text instance, string sourceText)
        {
            if (FFArabicMod.instance == null || string.IsNullOrEmpty(sourceText))
            {
                return;
            }

            string issueKey = sourceText + "|" + (instance != null && instance.font != null ? instance.font.name : "<no-font>");
            if (FFArabicMod.loggedGlyphIssues.Contains(issueKey))
            {
                return;
            }

            List<string> missing = new List<string>();
            TMP_FontAsset font = instance != null ? instance.font : FFArabicMod.arabicFontAsset;

            for (int i = 0; i < sourceText.Length; i++)
            {
                char c = sourceText[i];
                if (!ShouldCheckGlyph(c))
                {
                    continue;
                }

                if (!FontHasCharacter(font, c))
                {
                    missing.Add("'" + c + "' U+" + ((int)c).ToString("X4") + "@" + i);
                }
            }

            if (missing.Count > 0)
            {
                FFArabicMod.loggedGlyphIssues.Add(issueKey);
                FFArabicMod.instance.LoggerInstance.Warning(
                    "[ArabicGlyphDiag] Font '" + (font != null ? font.name : "<null>") + "' missing glyph(s) for text: \"" +
                    sourceText + "\" | missing: " + string.Join(", ", missing));
            }
        }

                private static string FormatArabicText(string text)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        return text;
                    }

                    bool hasArabic = false;
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (ArabicFixer.IsArabicChar(text[i]))
                        {
                            hasArabic = true;
                            break;
                        }
                    }

                    if (!hasArabic)
                    {
                        return text;
                    }

                    string normalizedTextBuilder = new StringBuilder(text.Length)
                        .Append(text)
                        .ToString();

                    string normalized = normalizedTextBuilder.Replace('?', '؟');
                    return ArabicFixer.Reshape(normalized);
                }

        private static TMP_FontAsset SelectBestFontForText(string shapedText)
        {
            if (FFArabicMod.arabicFontAsset == null)
            {
                return null;
            }

            FFArabicMod.EnsureFontGlyphs(FFArabicMod.arabicFontAsset, shapedText);
            if (!HasUnsupportedGlyphs(FFArabicMod.arabicFontAsset, shapedText))
            {
                return FFArabicMod.arabicFontAsset;
            }

            if (FFArabicMod.arabicSystemFontAsset != null)
            {
                FFArabicMod.EnsureFontGlyphs(FFArabicMod.arabicSystemFontAsset, shapedText);
                if (!HasUnsupportedGlyphs(FFArabicMod.arabicSystemFontAsset, shapedText))
                {
                    return FFArabicMod.arabicSystemFontAsset;
                }
            }

            return FFArabicMod.arabicFontAsset;
        }

        public static void LanguageSourceGetTranslationPostfix(string term, ref string __result)
        {
            if (FFArabicMod.TryTranslateTerm(term, out string translatedText))
            {
                __result = translatedText;
            }
        }

        public static void LanguageSourceTryGetTranslationPostfix(string term, ref bool __result, ref string Translation)
        {
            if (FFArabicMod.TryTranslateTerm(term, out string translatedText))
            {
                Translation = translatedText;
                __result = true;
            }
        }

        private static bool ContainsArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (ArabicFixer.IsArabicChar(c) || ArabicFixer.IsArabicPresentationForm(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyArabicFormatting(TMP_Text instance, ref string text)
        {
            if (instance == null || string.IsNullOrEmpty(text) || !ContainsArabicText(text))
            {
                return;
            }

            text = FormatArabicText(text);
            TMP_FontAsset targetFont = SelectBestFontForText(text);
            if (targetFont != null && instance.font != targetFont)
            {
                instance.font = targetFont;
            }

            FFArabicMod.EnsureFontGlyphs(instance.font, text);
            instance.isRightToLeftText = true;
            LogGlyphIssues(instance, text);
        }

        public static void StringPrefix(TextMeshProUGUI __instance, ref string __0)
        {
            if (FFArabicMod.arabicFontAsset != null && !string.IsNullOrEmpty(__0))
            {
                ApplyArabicFormatting(__instance, ref __0);
            }
        }

        public static void StringBuilderPrefix(TextMeshProUGUI __instance, StringBuilder __0)
        {
            if (FFArabicMod.arabicFontAsset != null && __0 != null && __0.Length > 0)
            {
                string originalString = __0.ToString();
                string formatted = originalString;
                ApplyArabicFormatting(__instance, ref formatted);
                if (originalString != formatted)
                {
                    __0.Clear();
                    __0.Append(formatted);
                }
            }
        }

        public static void StringSetTextPrefix(TMP_Text __instance, ref string __0)
        {
            if (FFArabicMod.arabicFontAsset != null && !string.IsNullOrEmpty(__0))
            {
                ApplyArabicFormatting(__instance, ref __0);
            }
        }
    }
}