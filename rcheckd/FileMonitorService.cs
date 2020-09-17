using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace rcheckd
{
    public class FileMonitorService : ServiceBase
    {
        private const string SERVICENAME = "rcheckd";

        // declare a list of watchers
        readonly List<FileSystemWatcher> watcherlist = new List<FileSystemWatcher>();
        Config config;
        public FileMonitorService()
        {
            this.ServiceName = SERVICENAME;  // can probably take this out, but must be tested.  see rcheckd.cs
            this.CanShutdown = true;  // allows this process to listen for shutdown events
        }


        Logger log;
        Email mailer;
    

        /// <summary>
        /// This method runs when the service is started
        ///    args can be passed in but the parameters in the service properties
        ///    on the windows gui are not saved??  So args here are not very useful
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            try
            {
                // find the config file in the installation directory.
                string dir = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location);
                config = Config.GetYaml(Path.Combine(dir,"rcheckconfig.yml"));
            }
            catch (Exception e)
            {
                Logger.WriteEventLog(Logger.EventSource, "Error getting config: " + e.Message, System.Diagnostics.EventLogEntryType.Error, 4901);
                Logger.WriteEventLog(Logger.EventSource, "rcheckd failed to start", System.Diagnostics.EventLogEntryType.Error, 4902);
                ExitCode = -1;
                throw new Exception("4911 Unable to parse YAML configuration file");
            }

            log = new Logger(config.Logfile, config.Debug);
            log.NotifyOnMessages = config.NotifyOnMessages;
            log.Write("rcheckd starting",1001);
            log.Write(config.ToString(), 9001); // only written if debug is true

            // see if we have a usable email configuration for notifications.
            if (string.IsNullOrEmpty(config.MailSettings.MailServer))
                mailer = null; // we don't have at least minimal Mailserver config, so don't attempt to send emails.
            else if (string.IsNullOrEmpty(config.MailSettings.MailTo))
            {
                log.Write("Configurtion Error - MailServer was specified, but no MailTo address was specfied.  Email notifications are disabled.",
                    5902, System.Diagnostics.EventLogEntryType.Warning);
                mailer = null; // disable email notifications.  we don't know who to send them to
            }
            else if (string.IsNullOrEmpty(config.MailSettings.MailTo))
            {
                log.Write("Configurtion Error - MailServer was specified, but no MailFrom address was specfied.  Email notifications are disabled.",
                    5903, System.Diagnostics.EventLogEntryType.Warning);
                mailer = null; // disable email notifications.  we don't know who to send them to
            }
            else
            {
                mailer = new Email(log,
                       config.MailSettings.MailServer,
                       config.MailSettings.MailServerPort,
                       config.MailSettings.MailAuthAccount,
                       config.MailSettings.MailAuthPassword);
     
                mailer.To = config.MailSettings.MailTo;
                mailer.From = config.MailSettings.MailFrom;

                if (config.Debug)
                    mailer.notify("RCheckd Started");

                log.mailer = mailer; // set the log mailer so it can send mail
            }

            // Test all sentinal files on startup
            //   in case something happened while this service was down
            log.Write("Testing Sentinal Files", 1012);
            TestSentinalFiles();

            // add watchers for the files specified in the yaml configuration
            log.Write("Adding Watchers", 1014);
            MakeWatchers();

        }

        /// <summary>
        /// Makes a file system watcher for each file to be monitored
        /// </summary>
        void MakeWatchers()
        {
            foreach (FileRecord fr in config.Files)
            {
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.BeginInit();  // must be here or the watcher will be completely ignored
                watcher.EnableRaisingEvents = true;
                watcher.Changed += new FileSystemEventHandler(File_Changed);
                watcher.Deleted += new FileSystemEventHandler(File_Changed);
                watcher.Created += new FileSystemEventHandler(File_Changed);
                watcher.Renamed += new RenamedEventHandler(File_Renamed);
                watcher.Error += new ErrorEventHandler(OnError);
                watcher.InternalBufferSize=4096; // increase if we get any buffer overflows, use multiple of 4K

                FileInfo fi;
                try
                {
                    fi = new FileInfo(fr.Path);  
                }
                catch (Exception e)
                {
                    log.Write("File: " + fr.Path + " : " + e.Message, 3001, System.Diagnostics.EventLogEntryType.Error);
                    continue;
                }

                if (!fi.Exists)
                    log.Write("   File: " + fr.Path + " : " + 
                        "Does not exists, adding to the watchlist in case it is created later ",
                        3002, System.Diagnostics.EventLogEntryType.Warning);

                watcher.Path = fi.DirectoryName;   
                watcher.Filter = fi.Name;

                watcher.NotifyFilter = NotifyFilters.LastWrite
                                | NotifyFilters.Size
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;
               
                watcherlist.Add(watcher);
                watcher.EndInit();
                log.Write("   Added: " + fr.Path, 1008);
            }
                
        }


        /// <summary>
        /// called when the service is stopped.
        /// </summary>
        protected override void OnStop()
        {
            log.Write("rcheckd stopping", 1002); 
            Thread.Sleep(1000); 
            foreach (FileSystemWatcher watcher in watcherlist)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            watcherlist.Clear();
            Thread.Sleep(3000); // extra time to clean up
        }


        /// <summary>
        /// called when the rcheckd is stopped due to system being shut down
        /// </summary>
        protected override void OnShutdown()
        {
            log.Write("rcheckd is stopping because the system is shutting down", 1003);
            base.OnShutdown();
        }


        /// <summary>
        /// Event handler called when a file changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventargs"></param>
        private void File_Changed(object sender, System.IO.FileSystemEventArgs eventargs)
        {
            string filepath = eventargs.FullPath;
            string changetype = eventargs.ChangeType.ToString();
            // If its a sentinal file, check for content change
            if (config.IsSentinal(filepath))
            {
                log.Write("Sentinal file: " + filepath + " " + changetype + " Detected", 2406);
                Sentinal sentinal = new Sentinal(filepath);
                if (sentinal.Test())
                {
                    log.Write("Sentinal file: " + filepath + " Contents unchanged", 2408);
                    return;
                }
                else
                {
                    log.Write("Sentinal file: " + filepath + " Contents changed", 2407, System.Diagnostics.EventLogEntryType.Warning);
                    try
                    {
                        if (sentinal.Randomness < 400)
                            log.Write("              " + filepath + " POSSIBLY ENCRYPTED", 2491, System.Diagnostics.EventLogEntryType.Warning);
                    }     
                    catch (Exception e)
                    {
                        // probably a permission issue, more details will be in the exception message
                        log.Write("           " + filepath + "  Unknown status - " + e.Message,
                             4918, System.Diagnostics.EventLogEntryType.Warning);
                    }
                }

            }
            else
            {
                StringBuilder message = new StringBuilder();
                message.Append("File: " + filepath + "  " + changetype);
                log.Write(message.ToString(), 2001, System.Diagnostics.EventLogEntryType.Warning);
            }

            // Do any action specified for the particular change that occured
            DoAction(filepath,"",changetype);
        }


        /// <summary>
        /// Invoke action as specifed in yaml for a particular file event
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="oldfilepath"></param>
        /// <param name="changetype"></param>
        private void DoAction(string filepath, string oldfilepath, string changetype)
        {
            // only attempt action if action is specified
            if (config.HasAction(filepath))
            {
                Action a = new Action(log);

                string parameters = config.GetFileParameter(filepath, "Action_Parameters");
                if (string.IsNullOrEmpty(parameters))
                    parameters = "";
                else
                    parameters = Action.ReplaceStrings(parameters, filepath, oldfilepath, changetype);

                string workingDirectory = config.GetFileParameter(filepath, "Action_Directory");
                if (string.IsNullOrEmpty(workingDirectory))
                    workingDirectory = "";

                a.MaxWaitTime = config.GetFileParameter(filepath, "Action_MaxWaitTime");
                try
                {
                    a.ExecuteCommand(config.GetFileParameter(filepath, "Action_Command"), parameters, workingDirectory);
                }
                catch (Exception e1)
                {
                    log.Write(e1.Message, 4904, System.Diagnostics.EventLogEntryType.Error);
                }

            }
        }


        /// <summary>
        /// Event handler when a file is renamed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventargs"></param>
        private void File_Renamed(object sender, System.IO.RenamedEventArgs eventargs)
        {
            string filepath = eventargs.FullPath;
            string oldfilepath = eventargs.OldFullPath;
            string changetype = eventargs.ChangeType.ToString();

            StringBuilder message = new StringBuilder();
            message.Append("File: " + oldfilepath + "  " + changetype + " to " + filepath) ;

            log.Write(message.ToString(), 2003, System.Diagnostics.EventLogEntryType.Warning);   
            // Do any action specified for the file that was renamed
            DoAction(filepath, oldfilepath, changetype);  
        }

        //  This method is called when the FileSystemWatcher detects an error.
        private void OnError(object source, ErrorEventArgs e)
        {
            //  Show that an error has been detected.
            log.Write("Watcher Error: " + e.GetException().Message, 4909, System.Diagnostics.EventLogEntryType.Error);
            //  In case of buffer overflow   
            //  This can happen if Windows is reporting many file system events quickly
            //  and internal buffer of the  FileSystemWatcher is not large enough to handle this
            //  rate of events. The InternalBufferOverflowException error informs the application
            //  that some of the file system events are being lost.

        }


        /// <summary>
        /// test all sentinal files
        /// </summary>
        private void TestSentinalFiles()
        {
            foreach (FileRecord File in config.Files)
            {
                if (File.Sentinal)
                {
                    Sentinal sentinal = new Sentinal(File.Path);
                    if (sentinal.Test())
                        log.Write("   Passed: " + File.Path, 2402);
                    else
                    {
                        log.Write("   Failed: " + File.Path + "  " + sentinal.Error, 2404, System.Diagnostics.EventLogEntryType.Warning);
                        try
                        {
                            if (sentinal.Randomness < 400)
                                log.Write("           " + File.Path + "  POSSIBLY ENCRYPTED", 2492, System.Diagnostics.EventLogEntryType.Warning);
                        }
                        catch (Exception e)
                        {
                            // probably a permission issue, more details will be in the exception message
                                log.Write("           " + File.Path + "  Unknown status - " + e.Message,
                                     4916, System.Diagnostics.EventLogEntryType.Warning);
                        }
                      
                    }
                }
            }
        }
    }
}
