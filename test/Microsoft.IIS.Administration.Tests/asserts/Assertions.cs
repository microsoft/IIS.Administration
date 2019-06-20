// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
