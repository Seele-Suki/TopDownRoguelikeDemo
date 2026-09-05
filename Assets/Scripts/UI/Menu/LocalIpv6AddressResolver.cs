using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TopDownRoguelike.Menu.UI
{
    public static class LocalIpv6AddressResolver
    {
        public static string ResolveLocalAddressOrLoopback()
        {
            var candidates = new List<IPAddress>();

            foreach (NetworkInterface networkInterface
                in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus !=
                    OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType ==
                    NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType ==
                    NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation address
                    in networkInterface.GetIPProperties()
                        .UnicastAddresses)
                {
                    candidates.Add(address.Address);
                }
            }

            return SelectPreferredAddress(candidates);
        }

        public static string SelectPreferredAddress(
            IEnumerable<IPAddress> candidates)
        {
            if (candidates != null)
            {
                foreach (IPAddress address in candidates)
                {
                    if (!IsGlobalIpv6(address))
                    {
                        continue;
                    }

                    return new IPAddress(
                        address.GetAddressBytes()).ToString();
                }
            }

            return IPAddress.IPv6Loopback.ToString();
        }

        private static bool IsGlobalIpv6(IPAddress address)
        {
            if (address == null ||
                address.AddressFamily != AddressFamily.InterNetworkV6 ||
                address.Equals(IPAddress.IPv6Any) ||
                address.Equals(IPAddress.IPv6Loopback) ||
                address.IsIPv6LinkLocal ||
                address.IsIPv6Multicast ||
                address.IsIPv6SiteLocal ||
                IsUniqueLocal(address))
            {
                return false;
            }

            return true;
        }

        private static bool IsUniqueLocal(IPAddress address)
        {
            byte firstByte = address.GetAddressBytes()[0];
            return (firstByte & 0xfe) == 0xfc;
        }
    }
}
