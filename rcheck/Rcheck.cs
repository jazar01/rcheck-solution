﻿using System;
using System.IO;
using rcheckd;
using System.Security.AccessControl;


namespace RCheck
{
    /***************************************************************************
     * RCheck generates a set of sentinal files at specified locations.        *
     *    The sentinal files can then be tested to determine if their contents *
     *    have been changed since generated.                                   *
     ***************************************************************************/

    class Rcheck
    {

        static Config config;

        static string configFilePath;
        static string operation;
        /*
        static string listpath;
        static string filepath;
        static int size;
        */

        #region Argument Parsing
        static void Parseargs(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }

            foreach( string arg in args)
            {
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.ToUpper().Substring(1, 1))
                    {
                        case "T":
                            if (string.IsNullOrEmpty(operation))
                                operation = "T";
                            else 
                                throw new ArgumentException("110 Extraneous operation argument: " + arg);
                            break;
                        case "G":
                            if (string.IsNullOrEmpty(operation))
                                operation = "G";
                            else
                                throw new ArgumentException("111 Extraneous operation argument: " + arg);
                            break;
                        case "D":
                            if (string.IsNullOrEmpty(operation))
                                operation = "D";
                            else
                                throw new ArgumentException("115 Extraneous operation argument: " + arg);
                            break;
                        case "H":
                        case "?":
                            operation = "H";
                            break;
                        case "I":
                            operation = "I";
                            break;
                        default:
                            throw new ArgumentException("101 Unknown argument: " + arg);
                    }
                }
                else
                    throw new ArgumentException("102 Unknown argument: " + arg);
            }

            if (operation == "H")
                return;

            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("120 You must specify -G, -T, or -D");

        }






        static void Help()
        {
            Console.Write
                (
                "\nUsage:\n" +
                "  rcheck [-T] [-G] [-D] [-H]  \n" +
                "                                                     \n" +
                "    -T    Tests the specified file(s) to determine if they have been modified \n\n" +
                "    -G    Generates specified file(s)              \n\n" +
                "    -D    Deletes specified file(s)              \n\n" +
                "    -H    Help\n\n" +
                " returns 0 if succesful, otherwise returns the number of failures \n\n" +
                "\nNotes:" +
                "\nThe -D option will only delete files generated by this utility.\n");
        }
        #endregion
        static int Main(string[] args)
        {
            try
            {
                Parseargs(args);
            }
            catch (ArgumentException e)
            {
                Console.Write("\nArgument error: " + e.Message + "\n");
                Help();
                return -1;
            }
            
            switch (operation)
            {
                case "H":
                    Help();
                    return 0;
                case "G":
                    Init();
                    return (Genfiles());
                case "T":
                    Init();
                    return (Testfiles());
                case "D":
                    Init();
                    Deletefiles();
                    return 0;
                case "I":
                    Init();
                    SetACL();
                    return 0;
                default:
                    return -999;  // should get here
            }
        }

        /// <summary>
        /// Used only by install utility to give current user who is installing
        /// permission to modify the config file.  Note this only works if
        /// setup is run as administrator.
        /// </summary>
        static void SetACL()
        {
            
            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            // Get a FileSecurity object that represents the
            // current security settings.
            FileSecurity fSecurity = File.GetAccessControl(configFilePath);

            // Add the FileSystemAccessRule to the security settings.
            fSecurity.AddAccessRule(new FileSystemAccessRule(user,
                FileSystemRights.FullControl, AccessControlType.Allow));

            // Set the new access settings.
            try
            {
                File.SetAccessControl(configFilePath, fSecurity);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteEventLog(Logger.EventSource, "Unable to set write permissions on: " + configFilePath + " Either do this manually, or run setup as adminstrator" , System.Diagnostics.EventLogEntryType.Warning, 4912);
            }
            
        }

        static void Init()
        {
            try
            {
                // find the config file in the installation directory.
                string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                configFilePath = System.IO.Path.Combine(dir, "rcheckconfig.yml");
                config = Config.GetYaml(configFilePath);

            }
            catch (Exception e)
            {
                Console.WriteLine( "Error getting config: " + e.Message);
                throw new Exception("4911 Unable to parse YAML configuration file");
            }


            // log = new logger(config.Logfile, config.Debug);
            Console.WriteLine("rcheck starting"); 

        }

/// <summary>
/// Test one or more files as specified on the command line
/// </summary>
/// <returns>number of failures</returns>
static int Testfiles()
        {
            int rc = 0;

            foreach (FileRecord File in config.Files)
            {
                if (File.Sentinal)
                {
                    Sentinal sentinal = new Sentinal(File.Path);
                    if (sentinal.Test())
                        Console.WriteLine("   Passed: " + File.Path + " R=" + sentinal.Randomness);
                    else
                    {
                        Console.WriteLine("   Failed: " + File.Path + "  " + sentinal.Error);
                        if (sentinal.Randomness < 400)
                                Console.WriteLine("            " + File.Path + " APPEARS TO BE ENCRYPTED  " + sentinal.Randomness);

                    rc++;
                    }
                }
            }

            Console.WriteLine("RC=" + rc);
            return rc;
        }

        /// <summary>
        /// Delete files previously generated sentinal files
        /// </summary>
        static void Deletefiles()
        {
            foreach (FileRecord File in config.Files)
            {      
                if (File.Sentinal)
                {
                    Sentinal s = new Sentinal(File.Path);
                    if (s.Delete())
                        Console.WriteLine("Deleted: '" + s.Path + "'");
                    else
                        Console.WriteLine("Failed:  '" + s.Path + "' " + s.Error);
                }
                else
                    Console.WriteLine(    "Skipped: '" + File.Path + "' is Not a sentinal file");

            }

        }



        /// <summary>
        /// Generate one or more sentinal files
        /// </summary>
        /// <returns>int number of failures</returns>
        static int Genfiles()
        {
            int rc = 0;

            foreach (FileRecord File in config.Files)
            {
                if (File.Sentinal) // only generate for files where Sentinal is true
                {
                    if (System.IO.File.Exists(File.Path))
                    {
                        Console.WriteLine("Failure: '" + File.Path + "' already exists ");
                        rc++;
                        continue;
                    }


                    Sentinal s = new Sentinal(File.Path);
                    int length = File.GetLength();

                    if (length == 0)
                        s.Length = new Random().Next(4, 32768) * 1024;  // set a random size if not specified
                    else
                        s.Length = length;


                    try
                    {
                        if (s.GenerateType1())
                            Console.WriteLine("Success: '" + s.Path + "' Generated, " + BytesConvert.ToString(s.Length));
                        else
                            Console.WriteLine("Something wierd happened attempting to write '" + File.Path);  // shouldnt be here
                                                                                                              // there should have been an execption if anything went wrong in s.Generate().
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("301 Error writing file: " + " " + e.Message);
                        rc++;
                    }
                }
            }

            Console.WriteLine("RC=" + rc);
            return rc;
        }


    }
}
