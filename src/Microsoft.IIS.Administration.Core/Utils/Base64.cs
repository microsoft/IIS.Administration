// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    using System;
    using System.Text;

    public static class Base64
    {
        public static string Encode(byte[] buff)
        {
            StringBuilder sb = new StringBuilder(Convert.ToBase64String(buff));

            // Escape
            int paddingIndex = sb.Length;
            for (int i = 0; i < sb.Length; ++i)
            {
                if (sb[i] == '=' && paddingIndex == sb.Length)
                {
                    paddingIndex = i;
                }

                switch (sb[i])
                {
                    case '+':
                        sb[i] = '-';
                        break;

                    case '/':
                        sb[i] = '_';
                        break;
                }
            }

            // Remove padding
            return sb.ToString(0, paddingIndex);
        }

        public static byte[] Decode(string s)
        {
            StringBuilder sb = new StringBuilder(s);

            // Unescape
            for (int i = 0; i < sb.Length; ++i)
            {
                switch (sb[i])
                {
                    case '-':
                        sb[i] = '+';
                        break;

                    case '_':
                        sb[i] = '/';
                        break;
                }
            }

            // Add padding
            if (sb.Length % 4 > 0)
            {
                sb.Append(new string('=', 4 - sb.Length % 4));
            }

            return Convert.FromBase64String(sb.ToString());
        }
    }
}
