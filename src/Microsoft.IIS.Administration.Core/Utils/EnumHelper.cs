// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class EnumHelper
    {
        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value) && Convert.ToInt32(value) != 0)
                    yield return value;
        }
    }
}
