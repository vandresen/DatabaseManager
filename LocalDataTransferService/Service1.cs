using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LocalDataTransferService
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer(); // name space(using System.Timers;) 
        string queueName = "mytestqueue";
        string storageAccount = "";

        public Service1()
        {
            InitializeComponent();
            storageAccount = ConfigurationManager.AppSettings["StorageAccount"];
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds  
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            
            QueueClient queueClient = new QueueClient(storageAccount, queueName);
            if (queueClient.Exists())
            {
                QueueProperties properties = queueClient.GetProperties();
                //PeekedMessage[] peekedMessage = queueClient.PeekMessages();
                int cachedMessagesCount = properties.ApproximateMessagesCount;
                WriteToFile($"Number of queue messages {cachedMessagesCount}");
                if (cachedMessagesCount > 0)
                {
                    QueueMessage[] messages = queueClient.ReceiveMessages();
                    foreach (var message in messages)
                    {
                        WriteToFile($"Message: '{message.MessageText}'");
                        queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
                        
                    }
                }


            }
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
