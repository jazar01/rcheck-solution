﻿using System;
using System.Diagnostics;
using System.IO;

namespace rcheckd
{

    public class Logger
    { 
        public string Logfile { get; set; }
        public bool Debug { get; set; }
        public static string EventSource = "RCheckd";

        public Logger(string filepath, bool debug)
        {
            Logfile = filepath;
            Debug = debug;
        }
        /* 
         * EventID conventions
         * 0    - service manager messages
         * 1xxx - general messages
         * 2xxx - file events
         * 3xxx - recoverable errors/warnings
         * 4xxx - fatal errors
         * 9xxx - debugging messages
         */
        /// <summary>
        /// write log message
        /// </summary>
        /// <param name="message">text of message</param>
        /// <param name="eventID"></param>
        /// <param name="type"></param>
        public void Write(string message, int eventID, EventLogEntryType type)
        {
            if (eventID >= 9000 && !Debug)  /* only log debugging messages if Debug is true */
                return;

            if (Logfile.ToLower() == "eventlog")
            {
                
                WriteEventLog(EventSource, message, type, eventID);
            }
            else
            {
                string entry = string.Format("{0} {1}\t{2} {3} {4}\n",
                                                DateTime.Now.ToShortDateString(),
                                                DateTime.Now.ToLongTimeString(),
                                                eventID,
                                                EventTypeString(type),
                                                message);

                //  TODO - check to see if directory needs to be created
                FileInfo fi = new FileInfo(Logfile);
                if (!fi.Exists) // new log file, write a header
                    File.WriteAllText(Logfile,string.Format("{0} {1}\t{2} {3} {4}\n", "Date     ", "Time   ", "Event", "Type", "Message"));

                System.IO.File.AppendAllText(Logfile, entry);
            }


        }


        // write informational log message
        public void Write(string message, int eventID)
        {
            Write(message, eventID, EventLogEntryType.Information);
        }

        // if event source doesn't already exist, start it
        public static void CreateEventSource()
        {
            if (!EventLog.SourceExists(EventSource))
                EventLog.CreateEventSource(EventSource, "");
        }

        // Convert a an EventLogEntryType to a short readable string
        private static string EventTypeString(EventLogEntryType type)
        {
            switch (type)
            {
                case EventLogEntryType.Error:
                    return "Error";
                case EventLogEntryType.Warning:
                    return "Warn ";
                case EventLogEntryType.Information:
                    return "Info ";
                case EventLogEntryType.FailureAudit:
                    return "AudtF";
                case EventLogEntryType.SuccessAudit:
                    return "AudtS";
                default:
                    return "Unkwn "; // shouldn't happen
            }
        }

        /// <summary>
        /// Write to event log
        /// This is used in the logger class but is exposed public for cases where 
        /// we dont have a log file configured yet, such as when getting the configuration data.
        /// </summary>
        /// <param name="EventSource"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="eventID"></param>
       public static void WriteEventLog(string EventSource, string message, EventLogEntryType type, int eventID )
        {
            CreateEventSource();
            EventLog.WriteEntry(EventSource, message, type, eventID);
        }

    }
}
