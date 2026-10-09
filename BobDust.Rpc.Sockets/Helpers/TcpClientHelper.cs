using System.Net;

public static class TcpClientHelper
{
    public static bool AreSame(string thisHostOrIp, string thatHostOrIp)
    {
        return AreSameDestination(ResolveIpAddresses(thisHostOrIp), ResolveIpAddresses(thatHostOrIp));
    }

    public static async Task<bool> AreSameAsync(string thisHostOrIp, string thatHostOrIp)
    {
        return AreSameDestination(await ResolveIpAddressesAsync(thisHostOrIp), await ResolveIpAddressesAsync(thatHostOrIp));
    }

    // Resolve host/IP to a set of normalized IP addresses
    public static HashSet<IPAddress> ResolveIpAddresses(string hostOrIp)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(hostOrIp, out IPAddress? parsedIp))
        {
            addresses = [parsedIp];
        }
        else
        {
            var entry = Dns.GetHostEntry(hostOrIp);
            addresses = entry.AddressList;
        }

        // Map IPv4-mapped IPv6 addresses or normalize loopbacks if needed
        return [.. addresses.Select(NormalizeIp)];
    }

    // Resolve host/IP to a set of normalized IP addresses
    public static async Task<HashSet<IPAddress>> ResolveIpAddressesAsync(string hostOrIp)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(hostOrIp, out IPAddress? parsedIp))
        {
            addresses = [parsedIp];
        }
        else
        {
            var entry = await Dns.GetHostEntryAsync(hostOrIp);
            addresses = entry.AddressList;
        }

        // Map IPv4-mapped IPv6 addresses or normalize loopbacks if needed
        return [.. addresses.Select(NormalizeIp)];
    }

    public static bool AreSameDestination(HashSet<IPAddress> setA, HashSet<IPAddress> setB)
    {
        return setA.Overlaps(setB);
    }

    private static IPAddress NormalizeIp(IPAddress ip)
    {
        // Convert IPv4-mapped IPv6 (e.g., ::ffff:127.0.0.1) to standard IPv4
        if (ip.IsIPv4MappedToIPv6)
        {
            return ip.MapToIPv4();
        }
        return ip;
    }
}
