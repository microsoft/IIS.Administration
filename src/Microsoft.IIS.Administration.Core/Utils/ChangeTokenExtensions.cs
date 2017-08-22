// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ChangeTokenExtensions
    {
        public static async Task WaitForChange(this IChangeToken changeToken, int millisecondTimeout)
        {
            var tcs = new TaskCompletionSource<IChangeToken>();
            IDisposable waitForChange = null;
            CancellationTokenSource ct = null;

            ct = new CancellationTokenSource(millisecondTimeout);

            ct.Token.Register(() => tcs.TrySetException(new TimeoutException()), useSynchronizationContext: false);

            waitForChange = changeToken.RegisterChangeCallback(_ => tcs.TrySetResult(changeToken), null);

            await tcs.Task;

            if (ct != null) {
                ct.Dispose();
                ct = null;
            }

            if (waitForChange != null) {
                waitForChange.Dispose();
                waitForChange = null;
            }
        }
    }
}
