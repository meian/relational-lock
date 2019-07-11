using System;
using System.Collections.Generic;
using System.Threading;

namespace RelationalLock {

    /// <summary>
    /// Provides constants and corrections for <see cref="TimeSpan"/>.
    /// </summary>
    public static class LockTimeConstants {

        /// <summary>
        /// Max TimeSpan value in library.
        /// Value for <see cref="int.MaxValue"/> milliseconds.
        /// <seealso cref="SemaphoreSlim.Wait(TimeSpan)"/> document.
        /// </summary>
        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromMilliseconds(int.MaxValue);

        /// <summary>
        /// Min TimeSpan value in library.
        /// Value for 1 milliseconds.
        /// </summary>
        public static readonly TimeSpan MinTimeSpan = TimeSpan.FromTicks(1);

        /// <summary>
        /// Correct TimeSpan value between min and max.
        /// </summary>
        /// <param name="value">target TimeSpan value</param>
        /// <param name="minSpan">min TimeSpan for correct. Default is <see cref="MinTimeSpan"/>.</param>
        /// <param name="maxSpan">max TimeSpan for correct. Default is <see cref="MaxTimeSpan"/>.</param>
        /// <returns>if greater than max then max, if less than min then min, if between then original value.</returns>
        public static TimeSpan Correct(this TimeSpan value, TimeSpan? minSpan = null, TimeSpan? maxSpan = null) {
            var min = minSpan ?? MinTimeSpan;
            var max = maxSpan ?? MaxTimeSpan;
            return value > max ? max : value < min ? min : value;
        }

        /// <summary>
        /// Get TimeSpan from <see cref="DateTime.Now"/>.
        /// </summary>
        /// <param name="value">value to compare with now</param>
        /// <returns>difference period from now</returns>
        public static TimeSpan FromNow(this DateTime value) => (value - DateTime.Now).Correct();

        /// <summary>
        /// Get DateTime after specified period.
        /// </summary>
        /// <param name="value">period</param>
        /// <returns>DateTime after period</returns>
        public static DateTime FromNowAt(this TimeSpan value) => DateTime.Now.Add(value.Correct());

        internal static TimeSpan ValidDefaultTimeSpan(this TimeSpan span, string name) {
            if (span < MinTimeSpan || MaxTimeSpan < span) {
                throw new ArgumentOutOfRangeException(name, $"invalid range TimeSpan: {span}");
            }
            return span;
        }
    }
}
