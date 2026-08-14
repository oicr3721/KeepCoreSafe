namespace KeepCoreSafe.Localization
{
    public readonly struct LocalizationLanguageInfo
    {
        public LocalizationLanguageInfo(string locale, string displayName)
        {
            Locale = locale;
            DisplayName = displayName;
        }

        public string Locale { get; }
        public string DisplayName { get; }
    }
}
