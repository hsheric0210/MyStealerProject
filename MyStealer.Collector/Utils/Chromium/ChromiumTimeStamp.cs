using System;

namespace MyStealer.Utils.Chromium
{
    public static class ChromiumTimeStamp
    {
        public static DateTime ToUtc(long timestamp) => new DateTime(Math.Max(0, timestamp / 1000000 - 11644473600), DateTimeKind.Utc);

        public static DateTime ToLocal(long timestamp) => new DateTime(Math.Max(0, timestamp / 1000000 - 11644473600), DateTimeKind.Local);
    }
}
