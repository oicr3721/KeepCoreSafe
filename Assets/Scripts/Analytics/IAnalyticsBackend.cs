using System.Collections.Generic;

namespace KeepCoreSafe.Analytics
{
    public enum AnalyticsProgressionStatus
    {
        Start,
        Complete,
        Fail
    }

    public interface IAnalyticsBackend
    {
        void SetEnabled(bool enabled);
        void Initialize();
        void SendDesign(string eventId, float? value, IReadOnlyDictionary<string, object> fields);
        void SendProgression(
            AnalyticsProgressionStatus status,
            string progression01,
            string progression02,
            string progression03,
            IReadOnlyDictionary<string, object> fields);
    }
}
