using Open.Nat;
using System;
using System.Diagnostics;
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

    public static bool IsAddedToFirewall
    {
        get
        {
            FirewallManager.FirewallCom firewall = new();
            foreach (var added in firewall.GetAuthorizeApps())
            {
                if (added.Name.Equals("FlexxTV Media Server"))
                {
                    if (!added.ProcessImageFileName.Equals(Paths.ExecutingBinary) && IsAdministrator)
                    {
                        firewall.RemoveAuthorizeApp(added.Name);
                        return IsAddedToFirewall;
                    }
                    return true;
                }
            }
            return false;
        }
    }

    public static bool IsAdministrator => !OperatingSystem.IsWindows() ||
       new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent())
       .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

    public static void AddToFirewall()
    {
        if (OperatingSystem.IsWindows() && !IsAddedToFirewall)
        {
            if (IsAdministrator)
            {
                FirewallManager.FirewallCom firewall = new();
                firewall.AddAuthorizeApp(new("FlexxTV Media Server", Paths.ExecutingBinary) { Enabled = true, IpVersion = FirewallManager.IpVersion.Any, Scope = FirewallManager.Scope.All });
            }
            else
            {
                log.Warn("Adding FlexxTV Media Server to Firewall");
                Process process = new()
                {
                    StartInfo = new()
                    {
                        FileName = Paths.ExecutingBinary,
                        Arguments = "-firewall",
                        Verb = "runas",
                        UseShellExecute = true,
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }

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
            log.Debug($"Created {map}");
            map = new(Protocol.Udp, port, port, description);
            await device.CreatePortMapAsync(map);
            log.Debug($"Created {map}");
            await ListPorts();
        }
        catch (Exception e)
        {
            log.Error($"Had an Issue opening port: {port}", e);
        }
    }

    #endregion Public Methods
}