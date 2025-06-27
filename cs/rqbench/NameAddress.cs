public class NameAddress
{
    public string Address { get; }

    public NameAddress(string address)
    {
        Address = address;
    }

    public string Network() => "tcp";

    public override string ToString() => Address;
}
