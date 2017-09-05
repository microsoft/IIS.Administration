// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Utils {
    using System;
    using System.Text;


    public static class TimeSpanExtentions {

        public static string Humanize(this TimeSpan ts) {
            int years = (int)Math.Round(Math.Abs(ts.TotalDays) / 365);
            int months = (int)Math.Round(Math.Abs(ts.TotalDays) / 30);
            int days = Math.Abs(ts.Days);
            int hours = Math.Abs(ts.Hours);
            int minutes = Math.Abs(ts.Minutes);

            var sb = new StringBuilder();

            // Years
            if (years > 0) {
                sb.AppendFormat("{0} year{1} ", years, years > 1 ? "s" : "");
            }

            // Months
            if (months > 0 && years == 0) {
                sb.AppendFormat("{0} month{1} ", months, months > 1 ? "s" : "");
            }

            // Days
            if (days > 0 && months == 0) {
                sb.AppendFormat("{0} day{1} ", days, days > 1 ? "s" : "");
            }

            // Hours
            if (hours > 0 && Math.Abs(ts.TotalDays) < 1) {
                sb.AppendFormat("{0} hour{1} ", hours, hours > 1 ? "s" : "");
            }

            // Minutes
            if (minutes > 0 && Math.Abs(ts.TotalHours) < 1) {
                sb.AppendFormat("{0} minute{1} ", minutes, minutes > 1 ? "s" : "");
            }

            // Seconds
            if (Math.Abs(ts.TotalMinutes) < 1) {
                sb.AppendFormat("a few moments ");
            }


            return sb.ToString().Trim();
        }
    }
}
