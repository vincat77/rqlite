using System.Collections.Generic;
using System.Net.NetworkInformation;

public class NetworkReporter
{
    public Dictionary<string, object> Stats()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var stats = new Dictionary<string, object>();
        foreach (var i in interfaces)
        {
            var addresses = new List<Address>();
            foreach (var addr in i.GetIPProperties().UnicastAddresses)
            {
                addresses.Add(new Address { Addr = addr.Address.ToString() });
            }
            stats[i.Name] = new InterfaceDetail
            {
                Flags = i.OperationalStatus.ToString(),
                HardwareAddress = i.GetPhysicalAddress().ToString(),
                Addresses = addresses
            };
        }
        return new Dictionary<string, object> { { "interfaces", stats } };
    }
}

public class Address
{
    public string Addr { get; set; } = string.Empty;
}

public class InterfaceDetail
{
    public string Flags { get; set; } = string.Empty;
    public string HardwareAddress { get; set; } = string.Empty;
    public List<Address> Addresses { get; set; } = new();
}
