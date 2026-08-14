using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KeepCoreSafe.Localization;
using UnityEditor;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class LocalizationDatabaseSync
    {
        private const string I18nFolder = "Assets/Resources/i18n";

        private static readonly Regex CodeKeyPattern = new(
            "LocalizationManager\\.(?:Get|Format)\\(\\s*\"(?<key>[^\"]+)\"|PlayLocalized\\(\\s*\"(?<key>[^\"]+)\"",
            RegexOptions.Compiled);

        private static readonly Regex AssetKeyPattern = new(
            "^  (?:displayName|description): (?<key>[A-Za-z0-9_.-]+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        [MenuItem("Keep Core Safe/Localization/Sync Missing Keys")]
        public static void SyncMissingKeys()
        {
            HashSet<string> keys = CollectKnownKeys();
            int added = 0;

            foreach (string path in Directory.GetFiles(I18nFolder, "*.json"))
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                Dictionary<string, string> table =
                    LocalizationJsonParser.ParseFlatStringTable(json);
                List<string> missing = new();
                foreach (string key in keys)
                {
                    if (!table.ContainsKey(key))
                        missing.Add(key);
                }

                if (missing.Count == 0)
                    continue;

                missing.Sort(string.CompareOrdinal);
                json = InsertMissingKeys(json, missing);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                added += missing.Count;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Localization sync complete. Added {added} missing key entries.");
        }

        private static HashSet<string> CollectKnownKeys()
        {
            HashSet<string> keys = new();
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Scripts" }))
                CollectCodeKeys(AssetDatabase.GUIDToAssetPath(guid), keys);

            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                CollectCodeKeys(path, keys);

            foreach (string path in Directory.GetFiles("Assets/Resources/Data", "*.asset", SearchOption.AllDirectories))
                CollectAssetKeys(path, keys);

            return keys;
        }

        private static void CollectCodeKeys(string path, HashSet<string> keys)
        {
            if (!path.EndsWith(".cs"))
                return;

            string text = File.ReadAllText(path, Encoding.UTF8);
            foreach (Match match in CodeKeyPattern.Matches(text))
            {
                string key = match.Groups["key"].Value;
                if (LooksLikeLocalizationKey(key))
                    keys.Add(key);
            }
        }

        private static void CollectAssetKeys(string path, HashSet<string> keys)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            foreach (Match match in AssetKeyPattern.Matches(text))
            {
                string key = match.Groups["key"].Value;
                if (LooksLikeLocalizationKey(key))
                    keys.Add(key);
            }
        }

        private static bool LooksLikeLocalizationKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                   && key.Contains('.')
                   && !key.Contains(' ');
        }

        private static string InsertMissingKeys(string json, List<string> missing)
        {
            int insertIndex = json.LastIndexOf('}');
            if (insertIndex < 0)
                return json;

            StringBuilder builder = new(json.Length + missing.Count * 32);
            builder.Append(json, 0, insertIndex);
            string trimmedPrefix = builder.ToString().TrimEnd();
            if (!trimmedPrefix.EndsWith("{"))
                builder.Append(',');
            builder.AppendLine();

            for (int i = 0; i < missing.Count; i++)
            {
                string key = missing[i];
                builder.Append("  \"");
                builder.Append(Escape(key));
                builder.Append("\": \"");
                builder.Append(Escape(key));
                builder.Append('"');
                builder.AppendLine(i < missing.Count - 1 ? "," : string.Empty);
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }
    }
}
