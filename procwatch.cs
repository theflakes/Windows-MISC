// Register WMI watch events for proc start/stop and print proc ID, name, and proc command line used.

using System;
using System.Management;
using System.Diagnostics;

namespace procwatch
{
    class Program
    {
        private ManagementEventWatcher processStartWatcher;
        private ManagementEventWatcher processStopWatcher;

        public void StartMonitoring()
        {
            string startQuery = "SELECT * FROM Win32_ProcessStartTrace";
            string stopQuery = "SELECT * FROM Win32_ProcessStopTrace";
            string managementPath = string.Format(@"root\cimv2");

            processStartWatcher = new ManagementEventWatcher(new WqlEventQuery(startQuery));
            processStopWatcher = new ManagementEventWatcher(new WqlEventQuery(stopQuery));
            ManagementScope scope = new ManagementScope(managementPath);
            scope.Connect();
            processStartWatcher.Scope = scope;
            processStopWatcher.Scope = scope;

            processStartWatcher.EventArrived += processStartWatcher_EventArrived;
            processStopWatcher.EventArrived += processStopWatcher_EventArrived;

            processStartWatcher.Start();
            processStopWatcher.Start();
        }

        public void StopMonitoring()
        {
            processStartWatcher.EventArrived -= processStartWatcher_EventArrived;
            processStopWatcher.EventArrived -= processStopWatcher_EventArrived;

            processStartWatcher.Stop();
            processStopWatcher.Stop();
        }

        void processStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var o = e.NewEvent.Properties["ProcessName"];
            var id = e.NewEvent.Properties["ProcessId"];

            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(
                "SELECT CommandLine FROM Win32_Process WHERE ProcessId=" + id.Value))
            {
                foreach (ManagementObject mo in mos.Get())
                {
                    Console.WriteLine("Got Start: {1}:{0} - {2}", 
                        o.Value, id.Value, mo["CommandLine"].ToString());
                }
            }
        }

        void processStopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var o = e.NewEvent.Properties["ProcessName"];
            var id = e.NewEvent.Properties["ProcessId"];

            Console.WriteLine("Got Stop: {1}:{0}", o.Value, id.Value);
        }

        public static void Main(string[] args)
        {
            Program eventWatcher = new Program();
            eventWatcher.StartMonitoring();

            Stopwatch s = new Stopwatch();
            s.Start();
            while (s.Elapsed < TimeSpan.FromSeconds(600))
            {
                System.Threading.Thread.Sleep(1000);
            }

            s.Stop();
        }
    }
}
