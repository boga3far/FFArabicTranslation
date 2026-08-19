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
        }

        private void LoadTranslations()
        {
            string jsonPath = Path.Combine(ModDirectory, "translations_by_key.json");

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
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
        
        public static string T(string key)
        {
            if (key != null && translations != null && translations.TryGetValue(key, out string translatedText))
            {
                // Return the raw translated text to test native TMP rendering
                return translatedText;
            }

            // Don't log null, empty, or whitespace strings as missing keys.
            if (!string.IsNullOrWhiteSpace(key) && !loggedMissingKeys.Contains(key))
            {
                loggedMissingKeys.Add(key);
                
                // Ensure instance and ModDirectory are initialized before logging/writing files.
                if (instance != null && ModDirectory != null)
                {
                    instance.LoggerInstance.Msg($"Missing Key: \"{key}\"");

                    try
                    {
                        string untranslatedFilePath = Path.Combine(ModDirectory, "Untranslated.txt");
                        File.AppendAllText(untranslatedFilePath, key + Environment.NewLine);
                    }
                    catch (Exception e)
                    {
                        instance.LoggerInstance.Error($"Failed to write to Untranslated.txt: {e.Message}");
                    }
                }
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

        private static void LogGlyphIssues(TextMeshProUGUI instance, string sourceText)
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

            string normalized = text.Replace('?', '؟');
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

        public static void StringPrefix(TextMeshProUGUI __instance, ref string __0)
        {
            if (FFArabicMod.arabicFontAsset != null && !string.IsNullOrEmpty(__0))
            {
                string translated = FFArabicMod.T(__0);
                if (__0 != translated)
                {
                    string formatted = FormatArabicText(translated);
                    TMP_FontAsset targetFont = SelectBestFontForText(formatted);

                    __0 = formatted;
                    if (targetFont != null && __instance.font != targetFont)
                    {
                        __instance.font = targetFont;
                    }
                    FFArabicMod.EnsureFontGlyphs(__instance.font, __0);
                    __instance.isRightToLeftText = true;
                    LogGlyphIssues(__instance, __0);

                }
            }
        }

        public static void StringBuilderPrefix(TextMeshProUGUI __instance, StringBuilder __0)
        {
            if (FFArabicMod.arabicFontAsset != null && __0 != null && __0.Length > 0)
            {
                var originalString = __0.ToString();
                var translatedString = FFArabicMod.T(originalString);
                if (originalString != translatedString)
                {
                    string formatted = FormatArabicText(translatedString);
                    TMP_FontAsset targetFont = SelectBestFontForText(formatted);

                    __0.Clear();
                    __0.Append(formatted);
                    if (targetFont != null && __instance.font != targetFont)
                    {
                        __instance.font = targetFont;
                    }
                    FFArabicMod.EnsureFontGlyphs(__instance.font, formatted);
                    __instance.isRightToLeftText = true;
                    LogGlyphIssues(__instance, formatted);

                }
            }
        }
    }
}
