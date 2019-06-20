
using System;

namespace Microsoft.IIS.Administration.Tests.Asserts {
    public static class Assertions {
        public static Action<T> All<T>(params Action<T>[] conditions) {
            return (T v) => {
                foreach (var c in conditions) {
                    c(v);
                }
            };
        }
    }
}
