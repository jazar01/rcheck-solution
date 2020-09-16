using System;
using System.Threading;
using System.Net.Mail;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;

namespace rcheckd
{

    /// <summary>
    /// email functions
    /// </summary>
    public class Email
    {
        public Logger Log { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string From { get; set; }
        public string To { get; set; }

        public Email(Logger log,
                     string mailServer, 
                     int mailServerPort, 
                     string mailAuthAccount, 
                     string mailAuthPassword)
        {
            Log = log;
            Server = mailServer;
            Port = mailServerPort;
            Account = mailAuthAccount;
            Password = mailAuthPassword;
        }
        /// <summary>
        /// Send a notification for RCheckd with all the defaults.
        /// </summary>
        /// <param name="entry"></param>
        public void notify(string entry)
        {
            // string DNSName = System.Net.Dns.GetHostName();
            string MachineName = System.Environment.MachineName;
            string message = "Notification from machine: " + MachineName + "\n\n" + entry;

            SendMail(From, To, "RCheckd notification from " + MachineName, message, MailPriority.High);
        }

        /// <summary>
        /// Send an email message with specified priority
        /// </summary>
        /// <param name="strFrom"></param>
        /// <param name="strTo"></param>
        /// <param name="strSubject"></param>
        /// <param name="strBody"></param>
        /// <param name="priority"></param>
        public void SendMail(string strFrom, string strTo, string strSubject, string strBody, MailPriority priority)
        {

            if (string.IsNullOrEmpty(strTo))
                return;

            MailMessage Message;
            Message = new MailMessage();
            Message.To.Add(strTo);
            Message.From = new MailAddress(strFrom);
            Message.Subject = strSubject;
            Message.Body = strBody;
            Message.Priority = priority;
            Message.BodyEncoding = System.Text.Encoding.UTF8;

            if (string.IsNullOrEmpty(strTo) || string.IsNullOrEmpty(strBody))
                return;

            try
            {
                AsyncMailSender(Message);
                // using the thread pool was unreliable because QueueUserWorkItem is aborted when the parent 
                // task ends.
                // System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(MailSender), (object) Message);
            }
            catch (Exception e)
            {
                string message = "Email failed - error: " + e.Message + ": " + e.InnerException.ToString();
                Log.Write(message, 5001, System.Diagnostics.EventLogEntryType.Error);
                
            }
        }

        /// <summary>
        /// spin off a thread to send an email message asynchronously
        /// </summary>
        /// <param name="message"></param>
        private void AsyncMailSender(MailMessage message)
        {
            try
            {
                System.Threading.ParameterizedThreadStart startDelegate = new ParameterizedThreadStart(MailSender);
                System.Threading.Thread mailThread = new Thread(startDelegate);

                // Background= false, causes the parent process to remain alive as long as any foreground
                // threads are still running.  Background= true, is not good because if the parent process ends,
                // then all background threads are aborted.  
                mailThread.IsBackground = false;
                mailThread.Name = "RCheckd Mail";
                mailThread.Start(message);
                Thread.Sleep(500);

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        /// <summary>
        /// This method actually sends the message
        /// </summary>
        /// <param name="mmessage"></param>
        private void MailSender(Object mmessage)
        {
            try
            {
                using (SmtpClient Mailer = new SmtpClient(Server, Port))
                {
                    MailMessage Message = (MailMessage)mmessage;

                    Mailer.EnableSsl = true;

                    if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Password))
                        Mailer.Credentials = new System.Net.NetworkCredential(Account, Password);

                    Mailer.Send(Message);
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                string message;
                if (e.InnerException == null)
                    message = "Email failed - error: " + e.Message;
                else
                    message = "Email failed - error: " + e.Message + ": " + e.InnerException.ToString();

                Log.Write(message, 5002, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Send an email message with default priority
        /// </summary>
        /// <param name="strFrom"></param>
        /// <param name="strTo"></param>
        /// <param name="strSubject"></param>
        /// <param name="strBody"></param>
        public void SendMail(string strFrom, string strTo, string strSubject, string strBody)
        {
            SendMail(strFrom, strTo, strSubject, strBody, MailPriority.Normal);
        }

    }
}
