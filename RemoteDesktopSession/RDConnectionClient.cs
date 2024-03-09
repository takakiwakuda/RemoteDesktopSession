using System.Net;
using System.Net.Sockets;
using Windows.Win32.System.RemoteDesktop;

namespace RemoteDesktopSession;

/// <summary>
/// Represents information about a Remote Desktop Connection (RDC) client.
/// </summary>
public class RDConnectionClient
{
    private const int IPv4AddressLength = 4;
    private const int IPv6AddressLength = 16;

    /// <summary>
    /// Gets the client network address.
    /// </summary>
    public IPAddress? Address
    {
        get
        {
            if (_clientAddress.Value.Length == 0)
            {
                return null;
            }

            return new IPAddress(_clientAddress.Value);
        }
    }

    /// <summary>
    /// Gets the address family of the address.
    /// </summary>
    public AddressFamily AddressFamily => (AddressFamily)_clientInfo.ClientAddressFamily;

    /// <summary>
    /// Gets the client build number.
    /// </summary>
    public int ClientBuildNumber => (int)_clientInfo.ClientBuildNumber;

    /// <summary>
    /// Gets the location of the ActiveX control DLL.
    /// </summary>
    public string? ClientDirectory => _clientDirectory.Value;

    /// <summary>
    /// Gets NetBIOS name of the client computer.
    /// </summary>
    public string? ClientName => _clientName.Value;

    /// <summary>
    /// Gets the name of the domain that the user belongs to.
    /// </summary>
    public string? DomainName => _domainName.Value;

    /// <summary>
    /// Gets the name of the client user.
    /// </summary>
    public string? UserName => _userName.Value;

    /// <summary>
    /// Gets the security level of encryption.
    /// </summary>
    public EncryptionLevel EncryptionLevel => (EncryptionLevel)_clientInfo.EncryptionLevel;

    /// <summary>
    /// Gets the horizontal resolution of the client's display in pixels.
    /// </summary>
    public int HorizontalResolution => _clientInfo.HRes;

    /// <summary>
    /// Gets the vertical resolution of the client's display in pixels.
    /// </summary>
    public int VerticalResolution => _clientInfo.VRes;

    /// <summary>
    /// Gets the color depth of the client's display.
    /// </summary>
    public ColorDepth ColorDepth => (ColorDepth)_clientInfo.ColorDepth;

    private readonly Lazy<byte[]> _clientAddress;
    private readonly Lazy<string?> _clientDirectory;
    private readonly Lazy<string?> _clientName;
    private readonly Lazy<string?> _domainName;
    private readonly Lazy<string?> _userName;
    private readonly WTSCLIENTW _clientInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDConnectionClient"/> class with the specified client information.
    /// </summary>
    /// <param name="clientInfo">The client information.</param>
    internal RDConnectionClient(WTSCLIENTW clientInfo)
    {
        _clientInfo = clientInfo;

        _clientAddress = new Lazy<byte[]>(GetClientAddress);
        _clientDirectory = new Lazy<string?>(GetClientDirectory);
        _clientName = new Lazy<string?>(GetClientName);
        _domainName = new Lazy<string?>(GetDomainName);
        _userName = new Lazy<string?>(GetUserName);
    }

    /// <summary>
    /// Returns the client name as a string.
    /// </summary>
    /// <returns>
    /// The client name; if the client name is <see langword="null"/>, the result of the <see cref="object.ToString"/>.
    /// </returns>
    public override string? ToString()
    {
        return _clientName.Value is null ? base.ToString() : _clientName.Value;
    }

    private unsafe byte[] GetIPv4Address()
    {
        var clientAddress = _clientInfo.ClientAddress;
        byte[] address = new byte[IPv4AddressLength];

        for (int i = 0; i < address.Length; i++)
        {
            address[i] = (byte)clientAddress[i];
        }

        return address;
    }

    private unsafe byte[] GetIPv6Address()
    {
        var clientAddress = _clientInfo.ClientAddress;
        byte[] address = new byte[IPv6AddressLength];
        ushort number;

        for (int i = 0; i < address.Length / 2; i++)
        {
            number = clientAddress[i];
            address[i * 2] = (byte)(number >> 8);
            address[i * 2 + 1] = (byte)number;
        }

        return address;
    }

    private byte[] GetClientAddress()
    {
        return AddressFamily switch
        {
            AddressFamily.InterNetwork => GetIPv4Address(),
            AddressFamily.InterNetworkV6 => GetIPv6Address(),
            _ => [],
        };
    }

    private unsafe string? GetClientDirectory()
    {
        var clientDirectory = _clientInfo.ClientDirectory;
        return MarshalHelper.PtrToStringOrNull(clientDirectory.Value);
    }

    private unsafe string? GetClientName()
    {
        var clientName = _clientInfo.ClientName;
        return MarshalHelper.PtrToStringOrNull(clientName.Value);
    }

    private unsafe string? GetDomainName()
    {
        var domain = _clientInfo.Domain;
        return MarshalHelper.PtrToStringOrNull(domain.Value);
    }

    private unsafe string? GetUserName()
    {
        var userName = _clientInfo.UserName;
        return MarshalHelper.PtrToStringOrNull(userName.Value);
    }
}
