
using System.Collections;
using System.ServiceProcess;
using System;
using System.IO;


namespace rcheckd
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            string serviceName = this.serviceInstaller1.ServiceName;
           
            ServiceController controller;

            try
            { 
                controller = new ServiceController(serviceName);        
                //File.AppendAllText(@"C:\temp\uninstall.log", DateTime.Now.ToLongTimeString() + " Controller Status = " + pStatus(controller.Status) + "\n");

                if (controller.Status == ServiceControllerStatus.Running | controller.Status == ServiceControllerStatus.Paused)
                {
                    // File.AppendAllText(@"C:\temp\uninstall.log", DateTime.Now.ToLongTimeString() + " Attempting to stop\n");
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, new System.TimeSpan(0, 0, 0, 60));
                    controller.Close();
                }
                
            }
            catch (Exception ) // TODO Handle do log this
            {
                // unable to find or stop service, ignore this on uninstall
                // File.AppendAllText(@"C:\temp\uninstall.log", "The service could not be stopped. Please stop the service manually. Error: " + e.Message);
            }
            finally
            {
                base.OnBeforeUninstall(savedState);
            }

        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Monitors files for changes (Orasi)";
            this.serviceInstaller1.DisplayName = "RCheck Monitor Service";
            this.serviceInstaller1.ServiceName = "RCheckd";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller1.Committed += new System.Configuration.Install.InstallEventHandler(this.OnCommitted);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;


        // below only used during debugging
        private string pStatus(ServiceControllerStatus status)
        {
            if (status == ServiceControllerStatus.Running)
                return "Running";
            if (status == ServiceControllerStatus.Stopped)
                return "Stopped";
            if (status == ServiceControllerStatus.Paused)
                return "Paused";
            if (status == ServiceControllerStatus.StartPending)
                return "StartPending";
            if (status == ServiceControllerStatus.PausePending)
                return "PausePending";
            if (status == ServiceControllerStatus.StopPending)
                return "StopPending";
            else
                return "other";
        }
    }
}