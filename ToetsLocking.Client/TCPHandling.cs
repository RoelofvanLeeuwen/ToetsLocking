using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ToetsLocking.Client
{
    internal static class TCPHandling
    {
        /// <summary>
        /// Deze methode luistert naar een verzoek van de RaspberryPi. 
        /// Wanneer in het verzoek 'GET_NAME' is verwerkt,
        /// zal er een antwoord komen met de opgegeven userName erin
        /// </summary>
        /// <param name="raspberryPiPort">De poort waarop de RaspberryPi het verzoek zal doen</param>
        /// <param name="userName">De naam welke de gebruiker moet invoeren om zich kenbaar te maken</param>
        internal static void StartListener(string machineNameIdentifier, int raspberryPiPort, string userName)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, raspberryPiPort);
            listener.Start();

            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (request == machineNameIdentifier)
                    {
                        byte[] response = Encoding.ASCII.GetBytes(userName);
                        stream.Write(response, 0, response.Length);
                    }
                }
            }
        }
        /// <summary>
        /// Deze methode zend het ip-adres en de userName naar de RaspberryPi, 
        /// zodat de RaspberryPi later deze cliënt kan bevragen
        /// </summary>
        /// <param name="connectionInfo"></param>
        internal static void SendMachineInfo(ConnectionInfo connectionInfo)
        {

            using (TcpClient client = new TcpClient(connectionInfo.raspberryPiIp, connectionInfo.raspberryPiPort))
            using (NetworkStream stream = client.GetStream())
            {
                string message = $"{connectionInfo.userName}:{connectionInfo.machineIp}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }

        }
        /// <summary>
        /// Returns een AddressFamily.InterNetwork ip-adres
        /// </summary>
        /// <returns>IP-adres</returns>
        /// <exception cref="IPNotFoundException"></exception>
        internal static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new IPNotFoundException("Geen IP-adres gevonden.");
        }
    }
    public class IPNotFoundException : Exception
    {
        public IPNotFoundException(string? message) : base(message)
        {
        }
    }
}

