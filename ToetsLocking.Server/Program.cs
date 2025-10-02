using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ToetsLocking.Server
{
    internal class Program
    {
        static IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

        static List<MachineInfo> windowsMachines = new List<MachineInfo>();

        static string? machineNameIdentifier = config.GetValue<string>("MachineNameIdentifier");
        static int pingtimeout = config.GetValue<int>("Pingtimeout");
        static int checkInterval = config.GetValue<int>("CheckInterval");
        static string? logFilePath = config.GetValue<string>("Logfile");
        static int port = config.GetValue<int>("ServerPort");

        static object lockObject = new object();

        static void Main()
        {
            Thread listenerThread = new Thread(StartListener);
            listenerThread.Start();
            Console.WriteLine("Start met luisteren...");
            while (true)
            {
                lock (lockObject)
                {
                    foreach (MachineInfo machine in windowsMachines)
                    {
                        if (!PingMachine(machine))
                        {
                            PrintError($"{machine} reageert niet!!");
                            LogUnresponsiveMachine(machine);

                        }
                        else
                        {
                            try
                            {
                                machine.Gebruiker = GetMachineName(machine);
                                PrintStatus($"{machine.MachineIP} reageert als {machine.Gebruiker}");
                            }
                            catch (Exception)
                            {

                                PrintError($"{machine.MachineIP}({machine.Gebruiker}) reageert niet!!");
                                LogUnresponsiveMachine(machine);
                            }


                        }
                    }
                }
                Thread.Sleep(checkInterval);
            }
        }

        static void StartListener()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string[] parts = message.Split(':');
                string machineIp = parts[1].Trim();

                lock (lockObject)
                {
                    if (!windowsMachines.Any(n => n.MachineIP == machineIp))
                    {
                        windowsMachines.Add(new MachineInfo() { MachineIP = machineIp });
                        PrintStatus($"{machineIp} aangemeld!");
                    }
                }
            }
            client.Close();
        }
        static void PrintStatus(string message)
        {
            var origneleKleur = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = origneleKleur;
        }
        static void PrintError(string message)
        {
            var origneleKleur = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = origneleKleur;
        }
        static bool PingMachine(MachineInfo machineInfo)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(machineInfo.MachineIP, pingtimeout);
            return reply.Status == IPStatus.Success;
        }

        static void LogUnresponsiveMachine(MachineInfo machineInfo)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {machineInfo.MachineIP}({machineInfo.Gebruiker}) reageert niet.");
            }
        }

        static string GetMachineName(MachineInfo machineInfo)
        {
            using (TcpClient client = new TcpClient(machineInfo.MachineIP, port))
            using (NetworkStream stream = client.GetStream())
            {
                byte[] request = Encoding.ASCII.GetBytes(machineNameIdentifier);
                stream.Write(request, 0, request.Length);

                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            }
        }


    }

}

