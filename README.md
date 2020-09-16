Rcheck V1

This project is intended to be used to detect and alert when a ransomware attack
is in progress.  It does this by detecting changes in specified files.

Requirements:
Windows 10, or a Windows Server OS.

****  Quick Start ****

1) Run the Setup utility.  (rightclick on setup.exe and "Run as Administrator")
        It is best to run Setup as adminstrator. If Setup is not run as administrator 
        you may not have permission to edit and save rcheckconfig.yml in the installation  
        directory, and you will need to manually set the security permissions on that file.
        
2) Review and edit the rcheckconfig.yml in the installation directory.
        The installation utility installs a shortcut to rcheckconfig.yml on the desktop
        for convenience.  

3) Generate the Sentinal files.  
        (rightclick on GenerateSentinals.cmd in the installation directory 
        and "Run as Administrator").  Running this command with administrator 
        permissions allows the Sentinal files specified in rcheckconfig.yml
        to be created in most locations.  GenerateSentinals can be run without
        administrator permissions as long as none of the paths are in restricted
        locations.

4) Start the rcheckd service


5) Check the eventlog or logfile for errors and messages.
        It is highly recommended to test your configuration and actions.


********

Uninstall:
1) Backup your rcheckconfig.yml
2) Use the Windows Control Panel to uninstall the program.

********

MAIN COMPONENTS  (found in the installation directory)

rcheck  - A console application that will read the rcheckconfig.yml file
          and generate Sentinal files in locations and names specified.
          The rcheck utility can also be used to test to see if Sentinal 
          files have been modified.

rcheckd - A windows service that continuously monitors the files specified 
          in rcheckconfig.yml. If any of the files change, events are logged 
          and actions will be taken if specified

********

RCHECKCONFIG.YML

This is the configuration that specifies what rcheck and rcheckd are supposed to do.
It is in yaml format.  see https://en.wikipedia.org/wiki/YAML
    # indicates comments
    indention is important
    DO NOT USE TABS,  use spaces

    Test your yaml file with http://www.yamllint.com/  
        This will validate that it is in a valid yaml format, but it does not ensure
        your configuration is correct.

rcheckconfig.yml can be modified at anytime but is only processed when the rcheckd
services startes.  If you modify rcheckconfig.yml, you must restart the serivce to
make the changes effective.

If the rcheckconfig.yml is not in a valid format, the service will fail to start.

Specify where rcheckd will log events.  Use either:
Logfile: EventLog
   -or-
Logfile: <your logfile path and name>

When logging to the event log, rcheckd logs in "Windows Logs/Application" 

Note: the rcheck command line util always sends output to stdout (the console)

You can specify "Debug: True" to cause more verbose logging to occur.  This is
recommended when experimenting with your rcheckconfig.yml file.

rcheckconfig.yml has a Files: section to specify which files should be 
generated or monitored.

Each file entry must specify a path using -Path. 
example:

-Path c:\documents\importantstuff.db

other parameters for each file are optional.

    Sentinal: true     Indicates to rcheck that a Sentinal file should be generated.
                       A Sentinal file is a specially formated file that has a hash 
                       in the first 32 bytes and the remainder of the file is a 
                       specially formatted series of bytes that match the hash value.

    Sentinal: false    (default)  rcheck will not generate this file.  The file specified
                       in the -Path will be monitored for changes by rcheckd.


    Length: <value>    This is only effective when Sentinal is set to true.  It indicates
                       that the file being generated should be the specified size.  Length 
                       can be an value between 4KB and 2GB inclusive.  If no length is specified
                       and Sentinal is set to true the file will be generated with a random
                       size between 4KB and 32MB.

    Action_Command:    If rcheckd detects a change in the specified file, the specified command
                       will be executed immediately.  Each file can have only one command.

    Action_Parameters: If rcheckd detects a change in the specified file, the Action_Command will
                       be executed and the specified parameters will be passed to the command.  
                       *see note below on replaceable strings in Parameters

    Action_Directory:  If rcheckd detects a change in the specified file, Action_Command will be 
                       executed using the specifed directory as the default working directory.

    Action_MaxWaitTime:  Specifies the maximum amount of time rcheckd will wait for the 
                       Action_Command to execute.
                       
    
    * Action_Parameters can have replaceable strings as follows:
        {File}   -   the full path and name of the file that was changed
        {ChangeType}  -  The type of change event that occured. (Change, Rename, or Delete)
        {Date}      - The date the change event occured
        {Time}      - The time the change event occured.

    When specifying Action_Command or Action_Parameters that contain spaces, the string must be
    enclosed in quotes.

    When using a backslash '\' or special characters in a quoted string you must escape the
    character with a backslash.  e.g. to include an actual backslash specify '\\'

    Special NOTE: If you are executing a built in Windows or DOS shell command you may need
    to execute the windows command processor "cmd".  When executing "cmd" be sure to include "/C"
    in the parameters, otherwise the command processor will not end after executing the command
    specified in the parameters.

    *********

    RCHECK command line utility

        rcheck {-g | -t | -d}

             -g  generates Sentinal files as specified in rcheckconfig.yml.
                 Only files with the Sentinal parameter set to true will be generated.
                 Sentinal files will be genereated in the specifed path with a the specified
                 length.  If the length is not specified a random length from 4K to 32MB will
                 be used. rcheck must be run with permissions to create the Sentinal files
                 in the specified paths.  

             -t  tests the Sentinal files specified in rcheckconfig.yml.
                 The return code on exit from rcheck will indicate the number of Sentinal files
                 that have been changed or deleted.  If all Sentinal files are in place and
                 unmodified then rcheck will return 0.

             -d  deletes previously generated Sentinal files.  
                 For safety rcheck will only delete Sentinal files that pass the Sentinal test,
                 meaning they are indeed Sentinal files that are unmodified.  rcheck must run
                 with permission to delete the files at the paths specified in rcheckconfig.yml

        Tips: Run rcheck from an administrator command prompt when generating or deleting 
              Sentinal files to make sure it has permissions needed.  In most cases there is no
              need to run rcheck with administrator permissions when testing files.

              rcheck -t can be used from a batch file or scheduled tasks to determine if any
              Sentinal files have been tampered with before running or deleting backups.

              NOTE: the rcheck command line utility does not perform any Actions that may be 
              specifed in rcheckconfig.yml.  Actions are only performed during real-time 
              monitoring by the rcheckd service.

    ********

    RCHECKD Windows Service

        The rcheckd service is installed when the setup utility is run.  rcheckd monitors the
        files specifed in rcheckconfig.yml.  If a file is changed a message is generated in 
        either the Windows Event Log, or in a log file specified in rcheckconfig.yml.  In 
        addition to logging events when changes occur, rcheckd can be configured to execute
        specified actions when a file is changed.

        Types of changes detected:

            Change - a file has been modified and saved.
            Delete - a file has been deleted.
            Rename - a filename has changed TO one of the filenames being monitored

            Note:  any file can be monitored for changes.  Sentinal files generated by the
            rcheck command line utility have additonal checks to determine if they have been
            encrypted.
        
        When a file change is detected during real-time monitoring by rcheckd, actions for the 
        the changed file in rcheckconfig.yml will be executed.

        Upon starting of the rcheckd service Sentinal files will be tested for changes and
        events logged if changes occured while rcheckd was not running.  However, any specified
        actions will not be performed during rcheckd service startup.  

********

    EVENTS generated by the rcheckd service

        1001 INFO  rcheckd service is starting

        1002 INFO  rcheckd service is stopping 

        1003 INFO  rcheckd service is stopping because the system is shutting down

        1008 INFO  A watcher for the specified file was succesfully added.

        1012 INFO  Displayed during service startup indicationg that Sential files are being tested
                   for changes that may have occured while rcheckd was not running.

        1014 INFO  Watchers are being added for file paths specified in rcheckconfig.yml

        2001 INFO  Warning a monitored file was changed

        2003 WARN  A file was renamed to the name of a monitored file

        2402 INFO  A Sentinal file was tested on service startup and passed

        2404 WARN  A Sentinal file was tested on service startup and failed (contents changed)

        2406 INFO  rcheckd detected a change in the specified Sentinal file

        2407 WARN  The contents of a Sentinal file have changed

        2408 INFO  A Sentinal file changed, but the contents are not changed

        2491 WARN  A Sentinal file has changed and is likely encrypted

        2492 WARN  A Sentinal file change was detected on service startup and file is likey encrypted.

        2508 INFO  An Action_Command is attempting to execute after a change was detected

        2509 INFO  An Action_Command executed and completed

        2591 WARN  An Action_Command was started but failed ot complete in the allowed time

        3001 ERROR A file specified in rcheckconfig.yml cannot be found or accessed.

        3002 WARN  A file specified in rcheckconfig.yml does not exist, but a watcher will
                   be created in case the file is created later.

        4901 ERROR There was a problem getting the configuration from rcheckconfig.yml. 
                   The file is possibly improperly formatted, or does not exist in the
                   expect location (installation directory)

        4902 ERROR The Windows Service rcheckd failed to start.  This is most likely
                   because of a problem with rcheckconfig.yml.

        4904 ERROR A file changed, but the specifed ACTION_COMMAND encountered and error

        4909 ERROR A File Watcher encountered an error and may not be monitoring the file

        4911 ERROR Unable parse rcheckconfig.yml.  check the format and use http://www.yamllint.com

        4912 WARN  The setup program could not give rcheckconfig.yml write permissions

        
        4916 WARN  An error occured while attempting to test a Sentinal file for randomnes.
                   This could be a permissions problem.

        4918 WARN  An error occured while attempting to test a Sentinal file for randomness
                   after a change was detected by the rcheckd service. This could be a permissions problem.

        5001 ERROR An attempt to send an email message failed.

        5002 ERROR An attempt to send an email message failed.

        5901 ERROR An attempt was made to send an email notification, but there is not a valid mailserver
                   configuration in rcheckconfig.yml

        9001 INFO  Verbose information displayed when Debug: True.  Displays the configuration
                   data obtained from rcheckconfig.yml

        9003 WARN  Verbose information displayed when Debug: True.  An invalid Action_MaxWaitTime
                   was specified in rcheckconfig.yml, the default of 5 seconds will be used.

********

GENERAL TIPS AND IDEAS:

    Do not monitor files that are expected to change. Like a log file or other file on the system
    That will generate numerous events.  

    Create Sentinal files in strategic locations where ransomware would likely find them.
       c:\users\someuser\documents\...
       c:\   (if allowed, it is likely that the ransomware would start at the root)
       c:\data  (make up some directories that looks attractive)

    Use filenames with known extensions that hackers would think are valuable data.
       .docx
       .pdf
       .db
       .dat
       .xls
       .bkp

    Consider using file names that you would recognize so you will not delete them accidentally
       rc_company.db
       rc_customerhistory.dat
       quickbooks_database.rc.db
       master_data_rc.bkp

    Dont make the names too obvious that a hacker would avoid them like:
       HackerBaitFile.txt

    Use mostly Sentinal files, but for ease of testing create one or more plain text files so
    you can easily modify with notepad and make sure the changes are detected and actions are
    excuting as you expect.

    Creating very large Sentinal files can be slow and not necessary.

    ALWAYS test your Action_Command and parameters. 

