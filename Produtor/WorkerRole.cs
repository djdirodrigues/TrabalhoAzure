using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Produtor
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        static CloudQueue cloudQueueOne;
        static CloudQueue cloudQueueTwo;

        // Connection to QueueOne and QueueTwo
        public static void ConnectToStorageQueue()
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=diegoresourcesdiag994;AccountKey=WQaZFdTT45XThk/4GyJcCOO2dfPv1HS/a/TSsjHIWGDlGGdQtu45FL53wc99wbY9FQm4EQzBq52Dhz32O/c/XQ==;EndpointSuffix=core.windows.net";
            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                Console.WriteLine("Expected connection string 'Azure Storage Account to be a valid Azure Storage Connection String.");
            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            cloudQueueOne = cloudQueueClient.GetQueueReference("queueone");
            cloudQueueTwo = cloudQueueClient.GetQueueReference("queuetwo");

            cloudQueueOne.CreateIfNotExists();
            cloudQueueTwo.CreateIfNotExists();
        }

        //Send message to QueueTwo
        public void SendMessageToQueueTwo(String MessageText)
        {
            var message = new CloudQueueMessage(MessageText);

            cloudQueueTwo.AddMessage(message);

        }

        //Get message form QueueOne
        public void GetMessageFromQueueOne()
        {
            CloudQueueMessage cloudQueueMessage = cloudQueueOne.GetMessage();

            if (cloudQueueMessage == null)
            {
                return;
            }
            Trace.TraceInformation("Get message from QueueOne and send to QueueTwo");
            SendMessageToQueueTwo(cloudQueueMessage.AsString);
            cloudQueueOne.DeleteMessage(cloudQueueMessage);
        }

        public override void Run()
        {
            Trace.TraceInformation("Produtor/Consumidor is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Produtor/Consumidor has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Produtor/Consumidor is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Produtor/Consumidor has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            ConnectToStorageQueue();

            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                GetMessageFromQueueOne();
                await Task.Delay(1000);
            }
        }
    }
}