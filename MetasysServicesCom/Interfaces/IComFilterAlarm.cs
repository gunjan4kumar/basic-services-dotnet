﻿using System.Runtime.InteropServices;

namespace JohnsonControls.Metasys.ComServices
{
    /// <summary>
    /// Provides attribute to filter alarm.
    /// </summary>
    [ComVisible(true)]
    [Guid("129059db-8a39-4c94-bc8a-86c0975e72c9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IComFilterAlarm
    {
        /// <summary>
        /// Earliest start time ISO8601 string
        /// </summary>
        string StartTime { get; set; }

        /// <summary>
        /// Latest end time ISO8601 string
        /// </summary>
        string EndTime { get; set; }

        /// <summary>
        /// The inclusive priority range, from 0 to 255, of the alarm.
        /// </summary>
        string PriorityRange { get; set; }

        /// <summary>
        /// The type of the requested alarms.
        /// </summary>
        int? Type { get; set; }

        /// <summary>
        /// The flag to exclude pending alarms Default: false.
        /// </summary>
        bool? ExcludePending { get; set; }

        /// <summary>
        /// The flag to exclude acknowledged alarms Default: false.
        /// </summary>
        bool? ExcludeAcknowledged { get; set; }

        /// <summary>
        /// The flag to exclude discarded alarms Default: false.
        /// </summary>
        bool? ExcludeDiscarded { get; set; }

        /// <summary>
        /// The attribute of the requested alarms.
        /// </summary>
        int? Attribute { get; set; }

        /// <summary>
        /// The system category of the requested alarms.
        /// </summary>
        int? Category { get; set; }

        /// <summary>
        /// The page number of items to return Default: 1.
        /// </summary>
        int? Page { get; set; }

        /// <summary>
        /// The maximum number of items to return in the response. 
        /// Valid range is 1-10,000. Default: 100
        /// </summary>
        int? PageSize { get; set; }

        /// <summary>
        /// The criteria to use when sorting results
        /// Accepted Values: itemReference, priority, creationTime
        /// </summary>
        string Sort { get; set; }
    }
}