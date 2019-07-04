using System;
using System.Collections.Generic;
using System.Linq;

namespace RelationalLock {

    public static class KeysUtil {

        public static string ToViewString(this string[] keys) {
            if (keys == null) {
                return string.Empty;
            }
            else {
                return $"[{string.Join(", ", keys.Select(ToViewString))}]";
            }
        }

        private static string ToViewString(string key) {
            if (key == null) {
                return "($null)";
            }
            else if (key.Length == 0) {
                return "($empty)";
            }
            else {
                return key;
            }
        }
    }
}
