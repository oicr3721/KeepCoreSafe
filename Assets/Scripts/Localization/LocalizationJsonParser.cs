using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KeepCoreSafe.Localization
{
    public static class LocalizationJsonParser
    {
        private static readonly Regex StringPairPattern = new(
            "\"(?<key>(?:\\\\.|[^\"\\\\])*)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.Compiled);

        public static Dictionary<string, string> ParseFlatStringTable(string json)
        {
            Dictionary<string, string> table = new();
            if (string.IsNullOrWhiteSpace(json))
                return table;

            foreach (Match match in StringPairPattern.Matches(json))
            {
                string key = Unescape(match.Groups["key"].Value);
                if (key is "locale" or "displayName")
                    continue;

                table[key] = Unescape(match.Groups["value"].Value);
            }

            return table;
        }

        public static bool TryReadMeta(string json, out string locale, out string displayName)
        {
            locale = ReadString(json, "locale");
            displayName = ReadString(json, "displayName");
            return !string.IsNullOrWhiteSpace(locale)
                   && !string.IsNullOrWhiteSpace(displayName);
        }

        private static string ReadString(string json, string key)
        {
            Match match = Regex.Match(
                json,
                $"\"{Regex.Escape(key)}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"");
            return match.Success ? Unescape(match.Groups["value"].Value) : string.Empty;
        }

        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder builder = new(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    builder.Append(c);
                    continue;
                }

                char escaped = value[++i];
                switch (escaped)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u' when i + 4 < value.Length:
                    {
                        string hex = value.Substring(i + 1, 4);
                        if (ushort.TryParse(
                                hex,
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out ushort code))
                        {
                            builder.Append((char)code);
                            i += 4;
                        }
                        break;
                    }
                    default:
                        builder.Append(escaped);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
