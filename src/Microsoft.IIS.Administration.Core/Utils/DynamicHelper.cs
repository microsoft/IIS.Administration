// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Helper class to aid in the conversion from simple dynamic objects (ex: JSON representations) to desired primitive types
    /// </summary>
    public static class DynamicHelper {

        public static Nullable<T> To<T>(dynamic value) where T : struct {
            if(value == null) {
                return null;
            }

            if (value is JValue) {
                try {
                    return To<T>(value.Value);
                }
                catch (FormatException e) {
                    throw new ApiArgumentException(value.Path, e);
                }
                catch (ArgumentException e) {
                    throw new ApiArgumentException(value.Path, e);
                }
            }

            if (value is T) {
                Nullable<T> t = value;
                return t;
            }

            string str = value as string;

            if (value is long) {
                // For conversion of JSON primitive preferred type (long) to (int) and (byte)
                str = value.ToString();
            }

            if(str == null) {
                return null;
            }

            Nullable<T> returnValue;

            if (typeof(T).GetTypeInfo().IsEnum) {
                returnValue = (Nullable<T>)Enum.Parse(typeof(T), str, true);
            }
            else if (typeof(T) == typeof(TimeSpan)) {
                returnValue = (Nullable<T>)((object)TimeSpan.Parse(str));
            }
            else if (typeof(T) == typeof(DateTime) && string.IsNullOrEmpty(str)) {
                return null;
            }
            else {
                returnValue = (Nullable<T>)Convert.ChangeType(str, typeof(T));
            }

            return returnValue;
        }

        public static long? To(dynamic value, long min, long max)
        {
            long? v = DynamicHelper.To<long>(value);

            if (v != null)
            {
                Validator.WithinRange(min, max, v.Value, value is JValue ? ((JValue)value).Path : string.Empty);
            }

            return v;
        }

        public static List<T> ToList<T>(IEnumerable<dynamic> values) where T : struct
        {
            var result = new List<T>();

            foreach (dynamic d in values) {
                T? t = To<T>(d);

                if (t == null) {
                    throw new ArgumentNullException("element");
                }

                result.Add(t.Value);
            }

            return result;
        }

        public static List<string> ToList(IEnumerable<dynamic> values)
        {
            var result = new List<string>();

            foreach (dynamic d in values) {
                string v = Value(d);

                result.Add(v);
            }

            return result;
        }

        public static string Value(dynamic member) {
            if (member == null) {
                return null;
            }

            if (member is JValue) {
                return Value(member.Value);
            }

            return member as string;
        }

        public static long? ToLong(dynamic value, int fromBase, long min = long.MinValue, long max = long.MaxValue) {
            string val = DynamicHelper.Value(value);
            if (val == null) {
                return null;
            }

            try {
                var v = Convert.ToInt64(val, fromBase);

                Validator.WithinRange(min, max, v, value is JValue ? ((JValue)value).Path : string.Empty);

                return v;
            }
            catch (FormatException e) {
                if (value is JValue) {
                    throw new ApiArgumentException(value.Path, e);
                }
                throw;
            }
        }

        public static T? If<T>(dynamic model, Action<T> then) where T : struct
        {
            T? value = DynamicHelper.To<T>(model);

            if (value != null) {
                try {
                    then(value.Value);
                }
                catch (FileLoadException e) {
                    throw new LockedException(model.Path, e);
                }
                catch (ApiArgumentException) {
                    throw;
                }
                catch (Exception e) {
                    if (model is JValue) {
                        throw new ApiArgumentException(model.Path, e);
                    }
                    throw;
                }
            }

            return value;
        }

        public static long? If(dynamic model, long min, long max, Action<long> then)
        {
            long? value = DynamicHelper.To(model, min, max);

            if (value != null) {
                try {
                    then(value.Value);
                }
                catch (FileLoadException e) {
                    throw new LockedException(model.Path, e);
                }
                catch (ApiArgumentException) {
                    throw;
                }
                catch (Exception e) {
                    if (model is JValue) {
                        throw new ApiArgumentException(model.Path, e);
                    }
                    throw;
                }
            }

            return value;
        }

        public static string If(dynamic model, Action<string> then)
        {
            string value = DynamicHelper.Value(model);

            if (value != null) {
                try {
                    then(value);
                }
                catch (FileLoadException e) {
                    throw new LockedException(model.Path, e);
                }
                catch (ApiArgumentException) {
                    throw;
                }
                catch (Exception e) {
                    if (model is JValue) {
                        throw new ApiArgumentException(model.Path, e);
                    }
                    throw;
                }
            }

            return value;
        }
    }
}
