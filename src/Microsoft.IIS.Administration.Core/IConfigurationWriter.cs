// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    public interface IConfigurationWriter
    {
        void WriteSection(string name, object value);
    }
}
