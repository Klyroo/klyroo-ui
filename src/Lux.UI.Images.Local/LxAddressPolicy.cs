using System.Net;

namespace Lux.UI.Images.Local;

/// <summary>Which addresses a remote image may be fetched from: global unicast addresses only, with the IPv4 rules applied to the IPv4 address an IPv6 form embeds.</summary>
internal static class LxAddressPolicy
{
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var b = address.GetAddressBytes();
        return b.Length == 4 ? IsPublicV4(b) : IsPublicV6(b);
    }

    private static bool IsPublicV4(ReadOnlySpan<byte> b) =>
        !(b[0] == 0                                          // 0.0.0.0/8 "this network"
          || b[0] == 10                                      // 10.0.0.0/8
          || (b[0] == 100 && b[1] is >= 64 and <= 127)       // 100.64.0.0/10 carrier-grade NAT
          || b[0] == 127                                     // loopback
          || (b[0] == 169 && b[1] == 254)                    // link-local, cloud metadata
          || (b[0] == 172 && b[1] is >= 16 and <= 31)        // 172.16.0.0/12
          || (b[0] == 192 && b[1] == 0 && b[2] == 0)         // 192.0.0.0/24 IETF protocol assignments
          || (b[0] == 192 && b[1] == 0 && b[2] == 2)         // 192.0.2.0/24 documentation
          || (b[0] == 192 && b[1] == 88 && b[2] == 99)       // 192.88.99.0/24 6to4 relay anycast
          || (b[0] == 192 && b[1] == 168)                    // 192.168.0.0/16
          || (b[0] == 198 && b[1] is 18 or 19)               // 198.18.0.0/15 benchmarking
          || (b[0] == 198 && b[1] == 51 && b[2] == 100)      // 198.51.100.0/24 documentation
          || (b[0] == 203 && b[1] == 0 && b[2] == 113)       // 203.0.113.0/24 documentation
          || b[0] >= 224);                                   // multicast, reserved, broadcast

    private static bool IsPublicV6(ReadOnlySpan<byte> b)
    {
        // Forms that carry an IPv4 address: the IPv4 rules decide.
        if (IsZero(b[..12])) return IsPublicV4(b[12..]);                                                       // ::a.b.c.d (IPv4-compatible), also :: and ::1
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B && IsZero(b[4..12])) return IsPublicV4(b[12..]); // 64:ff9b::/96 NAT64
        if (b[0] == 0x20 && b[1] == 0x02) return IsPublicV4(b[2..6]);                                          // 2002::/16 6to4

        // Everything else must be global unicast (2000::/3) and not one of the special blocks inside it.
        if ((b[0] & 0xE0) != 0x20) return false;
        if ((b[8] & 0xFD) == 0 && b[9] == 0x00 && b[10] == 0x5E && b[11] == 0xFE && !IsPublicV4(b[12..])) return false; // ISATAP interface id carrying a non-public IPv4
        if (b[0] == 0x20 && b[1] == 0x01)
        {
            if (b[2] == 0x00 && b[3] == 0x00) return false;                  // 2001::/32 Teredo
            if (b[2] == 0x0D && b[3] == 0xB8) return false;                  // 2001:db8::/32 documentation
            if (b[2] == 0x00 && (b[3] == 0x02 || b[3] is >= 0x10 and <= 0x2F)) return false; // 2001:2::/32 benchmarking, 2001:10::/28 and 2001:20::/28 ORCHID
        }
        if (b[0] == 0x3F && b[1] == 0xFF && (b[2] & 0xF0) == 0x00) return false; // 3fff::/20 documentation
        return true;
    }

    private static bool IsZero(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value != 0) return false;
        }
        return true;
    }
}
