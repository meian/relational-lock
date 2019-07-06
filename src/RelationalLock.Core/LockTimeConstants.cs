using System;
using System.Collections.Generic;

namespace RelationalLock {

    public static class LockTimeConstants {
        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromMilliseconds(int.MaxValue);
        public static readonly TimeSpan MinTimeSpan = TimeSpan.FromTicks(1);

        public static TimeSpan Correct(this TimeSpan span, TimeSpan? minSpan = null, TimeSpan? maxSpan = null) {
            var min = minSpan ?? MinTimeSpan;
            var max = maxSpan ?? MaxTimeSpan;
            return span > max ? max : span < min ? min : span;
        }

        public static TimeSpan FromNow(this DateTime datetime) => (datetime - DateTime.Now).Correct();

        public static DateTime FromNowAt(this TimeSpan timespan) => DateTime.Now.Add(timespan.Correct());

        internal static TimeSpan ValidDefaultTimeSpan(this TimeSpan span, string name) {
            if (span < MinTimeSpan || MaxTimeSpan < span) {
                throw new ArgumentOutOfRangeException(name, $"invalid range TimeSpan: {span}");
            }
            return span;
        }
    }
}
