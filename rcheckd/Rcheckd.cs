using System;
using System.Runtime.CompilerServices;
using System.ServiceProcess;


namespace rcheckd
{
    static class Rcheckd
    {
        /// <summary>
        /// The main entry point for the application.
        /// An array of services is created then run. (1 service in this case)
        /// </summary>
        static void Main()
        {
            // don't bother with args here, they aren't
            // passed to the service on startup
            Logger.CreateEventSource();
            
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FileMonitorService()
                
            };
            ServiceBase.Run(ServicesToRun);
        }



        

         


    }
}
