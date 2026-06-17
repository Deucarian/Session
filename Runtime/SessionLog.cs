using Deucarian.Logging;

namespace Deucarian.Session
{
    /// <summary>
    /// Package-level log categories for Deucarian Session.
    /// </summary>
    public static class SessionLog
    {
        public static readonly DLog General = DLog.For("Session");
        public static readonly DLog Authentication = DLog.For("Session.Authentication");
        public static readonly DLog Storage = DLog.For("Session.Storage");
        public static readonly DLog Samples = DLog.For("Session.Samples");
    }
}
