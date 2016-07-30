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
using System.IO;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

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
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                string appRoot = Environment.GetEnvironmentVariable("RoleRoot");
                string fullPath = Path.Combine(appRoot + @"\", @"AppRoot\startup.cmd");
                ProcessStartInfo startInfo = new ProcessStartInfo(fullPath)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Verb = "runas"
                };

                Process p = System.Diagnostics.Process.Start(startInfo);
                using (StreamReader reader = p.StandardOutput)
                {
                    string processResult = reader.ReadToEnd();
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "startProcess.log"), processResult + "\r\n\r\n");
                }
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "startProcess.log"),
                    ex.ToString() + "\r\n\r\n");
            }

            var hasRunPs = false;
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                if (!hasRunPs)
                {
                    try
                    {
                        File.AppendAllText(Path.Combine(Path.GetTempPath(), "PS.log"), "start PS\r\n\r\n");
                        ProcessStartInfo startInfo = new ProcessStartInfo("powershell.exe")
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            Verb = "runas"
                        };

                        startInfo.Arguments = @"P:\test.ps1";
                        Process p = System.Diagnostics.Process.Start(startInfo);

                        using (StreamReader reader = p.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            File.AppendAllText(Path.Combine(Path.GetTempPath(), "PS.log"), result + "\r\n\r\n");
                        }

                        using (StreamReader reader = p.StandardError)
                        {
                            string result = reader.ReadToEnd();
                            File.AppendAllText(Path.Combine(Path.GetTempPath(), "PS.log"), result + "\r\n\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "startProcess.log"),
                            ex.ToString() + "\r\n\r\n");
                    }
                    hasRunPs = true;
                }
                await Task.Delay(1000);
            }
        }
    }
}
