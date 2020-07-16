using System;
using System.Diagnostics;

namespace rcheckd
{
    class Action
    {
        public Logger log { get; set; }
        private int maxWaitTime = 5000; //default value

        // Maximum wait time for an action to complete
        public string MaxWaitTime
        {
            get { return maxWaitTime.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    maxWaitTime = 5000;
                else if (int.TryParse(value, out int result))
                {
                    if (result > 0)
                        maxWaitTime = result;
                    else if (result == 0)
                        maxWaitTime = 5000;
                }
                else
                    log.Write("Invalid Action_MaxWaitTime specified, default used", 9003, EventLogEntryType.Warning);
            }
            
        }

        public Action(Logger inLog)
        {
            log = inLog;
        }

        /// <summary>Executes a set of instructions through the command window</summary>
        /// <param name="executableFile">Name of the executable file or program</param>
        /// <param name="argumentList">List of arguments</param>
        public void ExecuteCommand(string executableFile, string argumentList, string workingDirectory)
        {
            log.Write(string.Format("Attempting to execute command: {0} {1}", executableFile, argumentList), 2508);  
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;
            startInfo.FileName = executableFile;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = argumentList;
            startInfo.WorkingDirectory = workingDirectory;

            try
            {
                // Start the process with the info specified
                DateTime stime = DateTime.Now;
                using (Process process = Process.Start(startInfo))
                {
                    if (process.WaitForExit(maxWaitTime))
                    {
                        string message = string.Format("Action completed in {0} milliseconds with return code: {1}",
                            (DateTime.Now - stime).Milliseconds, process.ExitCode);

                        log.Write(message, 2509);
                    }
                    else
                    {
                        string message = string.Format("Action failed to completed in {0} milliseconds",
                                 maxWaitTime);
                        log.Write(message, 2591, EventLogEntryType.Warning);

                        if (!process.HasExited)
                            process.Kill();          
                        
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Action: " + executableFile + " : " +  e.Message);
            }

        }
        /// <summary>
        /// Replace replaceable parameters in a string
        ///   this is used to allow replaceable parameters in action command arguments 
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string ReplaceStrings(string arguments, string filepath, string oldfilepath, string changetype)
        {
           string outstring = arguments.Replace("{File}", filepath);
            outstring = outstring.Replace("{OldFile}",oldfilepath);
            outstring = outstring.Replace("{ChangeType}", changetype);
            outstring = outstring.Replace("{Date}", DateTime.Now.ToShortDateString());
            outstring = outstring.Replace("{Time}", DateTime.Now.ToLongTimeString());
            outstring = outstring.Replace("{ComputerName}", Environment.MachineName);

           return outstring;
        } 

    }
}
