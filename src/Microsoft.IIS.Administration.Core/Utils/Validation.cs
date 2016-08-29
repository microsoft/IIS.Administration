// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    public class Validator
    {
        public static long WithinRange(long min, long max, long value, string name)
        {
            if (value < min || value > max)
            {
                throw new ApiArgumentOutOfRangeException(name, min, max);
            }
            return value;
        }
    }
}
