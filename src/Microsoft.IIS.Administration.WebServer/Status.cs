// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Microsoft.Web.Administration;

    public enum Status
    {
        Unknown = 0,
        Stopping,
        Stopped,
        Starting,
        Started,
        Recycling,
    }

    public static class StatusExtensions
    {
        public static Status FromObjectState(ObjectState state)
        {
            switch (state)
            {
                case ObjectState.Started:
                    return Status.Started;

                case ObjectState.Starting:
                    return Status.Starting;

                case ObjectState.Stopped:
                    return Status.Stopped;

                case ObjectState.Stopping:
                    return Status.Stopping;

                default:
                    return Status.Unknown;
            }
        }

        public static ObjectState ToObjectState(Status status)
        {
            switch (status) {
                case Status.Stopping:
                    return ObjectState.Stopping;
                case Status.Stopped:
                    return ObjectState.Stopped;
                case Status.Starting:
                    return ObjectState.Starting;
                case Status.Started:
                    return ObjectState.Started;
                default:
                    return ObjectState.Unknown;
            }
        }
    }
}
