using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KeepCoreSafe.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace KeepCoreSafe.Editor
{
    public static class LocalizationFontAtlasBuilder
    {
        private const string LocalizationFolder = "Assets/Resources/i18n";
        private const string LogPrefix = "[Localization Font Builder]";
        // Localization does not swap fonts per locale. These are the three UI font
        // assets used by localized labels: regular, bold, and compact offer text.
        private static readonly string[] LocalizationFontPaths =
        {
            "Assets/Fonts/Mona12 SDF.asset",
            "Assets/Fonts/Mona12-Bold SDF.asset",
            "Assets/Fonts/MonaS10x12 SDF.asset"
        };

        [MenuItem("Tools/Localization/Build Font Atlases")]
        public static void BuildFontAtlases()
        {
            try
            {
                List<LocaleCharacters> locales = CollectLocaleCharacters();
                if (locales.Count == 0)
                {
                    Debug.LogWarning($"{LogPrefix} No valid localization JSON was found.");
                    return;
                }

                List<TMP_FontAsset> fonts = FindLocalizationFonts();
                if (fonts.Count == 0)
                {
                    Debug.LogWarning($"{LogPrefix} No configured Dynamic TMP Font Asset was found.");
                    return;
                }

                string allCharacters = BuildCharacterString(locales.SelectMany(locale => locale.CodePoints));
                StringBuilder report = new();
                report.AppendLine($"{LogPrefix}");
                report.AppendLine($"Languages found: {locales.Count}");
                report.AppendLine($"Dynamic Font Assets found: {fonts.Count}");
                report.AppendLine();

                foreach (LocaleCharacters locale in locales)
                {
                    report.AppendLine($"{locale.Locale}:");
                    report.AppendLine($"  JSON: {locale.AssetPath}");
                    report.AppendLine($"  Unique characters: {locale.CodePoints.Count}");
                }

                report.AppendLine();
                foreach (TMP_FontAsset font in fonts)
                {
                    int before = font.characterTable?.Count ?? 0;
                    font.TryAddCharacters(allCharacters, out _, true);
                    int after = font.characterTable?.Count ?? 0;
                    EditorUtility.SetDirty(font);
                    font.HasCharacters(
                        allCharacters, out uint[] missingInAsset, false, false);
                    font.HasCharacters(
                        allCharacters, out uint[] missingWithFallbacks, true, false);
                    missingInAsset ??= Array.Empty<uint>();
                    missingWithFallbacks ??= Array.Empty<uint>();

                    report.AppendLine($"Font: {AssetDatabase.GetAssetPath(font)}");
                    report.AppendLine($"  Population mode: {font.atlasPopulationMode}");
                    report.AppendLine($"  Added characters: {Mathf.Max(0, after - before)}");
                    report.AppendLine($"  Total characters: {after}");
                    if (missingInAsset.Length > 0)
                    {
                        report.AppendLine(
                            $"  Served by fallback / unavailable in this asset: {missingInAsset.Length} "
                            + $"({FormatCodePoints(missingInAsset, 24)})");
                    }
                    if (missingWithFallbacks.Length > 0)
                    {
                        report.AppendLine(
                            $"  WARNING - missing after fallbacks: {missingWithFallbacks.Length} "
                            + $"({FormatCodePoints(missingWithFallbacks, 24)})");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                report.AppendLine();
                report.AppendLine("Font Atlas Build Completed.");
                Debug.Log(report.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Build failed.\n{exception}");
                throw;
            }
        }

        private static List<LocaleCharacters> CollectLocaleCharacters()
        {
            List<LocaleCharacters> locales = new();
            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { LocalizationFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null
                    || !LocalizationJsonParser.TryReadMeta(
                        asset.text, out string locale, out _))
                {
                    Debug.LogWarning($"{LogPrefix} Invalid localization JSON was skipped: {path}");
                    continue;
                }

                Dictionary<string, string> table =
                    LocalizationJsonParser.ParseFlatStringTable(asset.text);
                HashSet<uint> codePoints = new();
                foreach (string value in table.Values)
                    AddCodePoints(value, codePoints);

                locales.Add(new LocaleCharacters(locale, path, codePoints));
            }

            locales.Sort((left, right) => string.CompareOrdinal(left.Locale, right.Locale));
            return locales;
        }

        private static List<TMP_FontAsset> FindLocalizationFonts()
        {
            List<TMP_FontAsset> fonts = new();
            foreach (string path in LocalizationFontPaths)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null)
                {
                    Debug.LogWarning($"{LogPrefix} Font Asset was not found: {path}");
                    continue;
                }
                if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} Font Asset is not Dynamic and was skipped: {path}");
                    continue;
                }
                fonts.Add(font);
            }
            return fonts;
        }

        private static string BuildCharacterString(IEnumerable<uint> codePoints)
        {
            StringBuilder builder = new();
            foreach (uint codePoint in codePoints.Distinct().OrderBy(value => value))
                builder.Append(char.ConvertFromUtf32((int)codePoint));
            return builder.ToString();
        }

        private static void AddCodePoints(string text, HashSet<uint> destination)
        {
            if (string.IsNullOrEmpty(text))
                return;

            for (int i = 0; i < text.Length; i++)
            {
                uint codePoint = text[i];
                if (char.IsHighSurrogate(text[i])
                    && i + 1 < text.Length
                    && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = (uint)char.ConvertToUtf32(text[i], text[++i]);
                }
                destination.Add(codePoint);
            }
        }

        private static string FormatCodePoints(IEnumerable<uint> points, int maximum)
        {
            return string.Join(", ", points.Distinct().OrderBy(value => value).Take(maximum)
                .Select(value => $"U+{value:X4}"));
        }

        private readonly struct LocaleCharacters
        {
            public LocaleCharacters(string locale, string assetPath, HashSet<uint> codePoints)
            {
                Locale = locale;
                AssetPath = assetPath;
                CodePoints = codePoints;
            }

            public string Locale { get; }
            public string AssetPath { get; }
            public HashSet<uint> CodePoints { get; }
        }
    }
}
