using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BasicFleetServer.Utils
{
    public static class UtilsForIP
    {
        public static string? GetActiveLanIP()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Must be up and running
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Ignore loopback ( PC talking to itself ) & tunnel ( Virtual envs )
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                var ipProps = ni.GetIPProperties();

                // IMPORTANT: Must have a default gateway ( connected to a network )
                if (!ipProps.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork))
                    continue;

                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }

            return null;
        }
    }
}