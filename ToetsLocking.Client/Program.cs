using Microsoft.Extensions.Configuration;

namespace ToetsLocking.Client
{
    internal class Program
    {
        //static string settingsFilePath = "settings.txt";
        //static string raspberryPiIp = "172.19.2.227"; // IP-adres van de Raspberry Pi
        //static string raspberryPiIp = "127.0.0.1";

        static IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

        static string? raspberryPiAdres = config.GetValue<string>("RaspberryPiAdres");
        static int raspberryPiPort = config.GetValue<int>("RaspberryPiPort");
        static string? machineNameIdentifier = config.GetValue<string>("MachineNameIdentifier");

        static string? userName = string.Empty;
        static string machineIp = string.Empty;

        static void Main(string[] args)
        {
            Console.WriteLine("Wat is je naam?");
            userName = Console.ReadLine();
            if (string.IsNullOrEmpty(userName))
            {
                PrintError("Vul een geldige naam in!!!");
                return;
            }

            machineIp = TCPHandling.GetLocalIPAddress();
            PrintStatus($"Je IP-Adres welke gebruikt word is: {machineIp}");

            //Luister naar een verzoek van de RaspberryPi
            Thread listenerThread = new Thread(
                () => TCPHandling.StartListener(machineNameIdentifier, raspberryPiPort, userName));

            listenerThread.Start();

            //Maak jezelf kenbaar bij de RaspberryPi
            TCPHandling.SendMachineInfo(
                new ConnectionInfo(
                    raspberryPiAdres,
                    raspberryPiPort,
                    userName,
                    machineIp));
        }

        static void PrintError(string message)
        {
            var origneleKleur = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = origneleKleur;
        }

        static void PrintStatus(string message)
        {
            var origneleKleur = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = origneleKleur;
        }
    }
}
