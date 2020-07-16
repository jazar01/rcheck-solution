using System;
using System.ComponentModel;
using System.Diagnostics;

namespace rcheckd
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void OnCommitted(object sender, System.Configuration.Install.InstallEventArgs e)
        {
            System.ServiceProcess.ServiceInstaller installer = (System.ServiceProcess.ServiceInstaller) sender;
            string serviceName = installer.ServiceName;
            
            string parameters = "/c sc failure \"" + serviceName + "\" reset= 0 actions=restart/5000/restart/5000/restart/5000";
            // File.AppendAllText(@"C:\temp\install.log", "Adding recovery options " + parameters + "\n Name = " + serviceName + "\n");
            try
            {
                Process.Start("cmd.exe", parameters);
            }
            catch (Exception)
            {
                // TODO log this to the event log
            }
        }
    }
}
