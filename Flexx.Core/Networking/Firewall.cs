using Open.Nat;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Networking;

public static class Firewall
{
    #region Public Methods

    public static async Task ClosePort(int port)
    {
        NatDevice device = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, new(5000));

        foreach (Mapping mapping in await device.GetAllMappingsAsync())
        {
            if (mapping.PrivatePort == port)
            {
                log.Debug($"Deleting {mapping}");

                await device.DeletePortMapAsync(mapping);
            }
        }
    }

    public static IPAddress GetLocalIP()
    {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
            GatewayIPAddressInformationCollection addresses = adapterProperties.GatewayAddresses;
            if (addresses.Count > 0)
            {
                foreach (GatewayIPAddressInformation address in addresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string root = $"{address.Address.ToString().Split('.')[0]}.{address.Address.ToString().Split('.')[1]}.{address.Address.ToString().Split('.')[2]}";
                        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                        foreach (IPAddress ip in host.AddressList)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                            {
                                if (ip.ToString().StartsWith(root))
                                {
                                    return ip;
                                }
                            }
                        }
                    }
                }
            }
        }
        throw new WebException("No outward bound IP address was found");
    }

    public static IPAddress GetPublicIP()
    {
        try
        {
            return new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, new(5000)).Result.GetExternalIPAsync().Result;
        }
        catch
        {
            try
            {
                return new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Pmp, new(5000)).Result.GetExternalIPAsync().Result;
            }
            catch
            {
                throw new WebException("Couldn't find router using either UPNP or PMP protocols.");
            }
        }
    }

    public static async Task<bool> IsPortOpen(int port)
    {
        NatDiscoverer nat = new();
        CancellationTokenSource cts = new(5000);
        NatDevice device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

        foreach (Mapping mapping in await device.GetAllMappingsAsync())
        {
            if (mapping.PrivatePort == port)
            {
                return true;
            }
        }
        return false;
    }

    public static async Task ListPorts()
    {
        NatDiscoverer nat = new();
        CancellationTokenSource cts = new(5000);
        NatDevice device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

        foreach (Mapping mapping in await device.GetAllMappingsAsync())
        {
            log.Debug($"OPENED => {mapping}");
        }
    }

    public static async Task OpenPort(int port, string description = "FlexxTV Media Server")
    {
        try
        {
            await ClosePort(port);
            NatDevice device = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, new(10000));
            Mapping map = new(Protocol.Tcp, port, port, description);
            await device.CreatePortMapAsync(map);
            map = new(Protocol.Udp, port, port, description);
            await device.CreatePortMapAsync(map);
            log.Debug($"Created {map}");
        }
        catch (Exception e)
        {
            log.Error(e);
        }
    }

    #endregion Public Methods
}