using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Linq;


public static class LanPortCheck
{
    public static bool IsPortUsed(int port = 0)
    {
        if (port == default)
        {
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            port = (int)transport.ConnectionData.Port;
        }

        bool alreadyInUse = System.Net.NetworkInformation.IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveUdpListeners()
            .Any(p => p.Port == port);

        return alreadyInUse;
    }
}