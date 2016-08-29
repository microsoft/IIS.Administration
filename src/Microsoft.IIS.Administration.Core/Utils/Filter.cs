// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils {
    using System;
    using System.Reflection;
    using AspNetCore.Http;

    public sealed class Filter {
        private IQueryCollection _values;

        public Filter(IQueryCollection values) {
            _values = values;
        }


        public Nullable<T> Get<T>(string key) where T : struct {
            if (string.IsNullOrEmpty(key)) {
                return null;
            }

            Nullable<T> returnValue;

            string val = _values[key];

            if (val == null) {
                return null;
            }
            try {
                if (typeof(T).GetTypeInfo().IsEnum) {
                    returnValue = (T)Enum.Parse(typeof(T), val, true);
                }
                else if (typeof(T) == typeof(TimeSpan)) {
                    returnValue = (T)((object)TimeSpan.Parse(val));
                }
                else {
                    returnValue = (T)Convert.ChangeType(val, typeof(T));
                }
            }
            catch (OverflowException e) {
                throw new ApiArgumentException(key, e);
            }
            catch (FormatException e) {
                throw new ApiArgumentException(key, e);
            }
            catch (ArgumentException e) {
                throw new ApiArgumentException(key, e);
            }

            return returnValue;
        }

        public string Get(string key) {
            return _values[key];
        }
    }
}
