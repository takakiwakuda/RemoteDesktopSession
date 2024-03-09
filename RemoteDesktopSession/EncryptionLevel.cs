namespace RemoteDesktopSession;

/// <summary>
/// Indicates encryption levels used by Remote Desktop Connection (RDC).
/// </summary>
/// <remarks>
/// Troubleshoot authentication errors when you use RDP to connect to Azure VM - Virtual Machines | Microsoft Learn
/// https://learn.microsoft.com/en-us/troubleshoot/azure/virtual-machines/cannot-connect-rdp-azure-vm#troubleshoot-standalone-vms
/// </remarks>
public enum EncryptionLevel
{
    None,
    Low,
    ClientCompatible,
    High,
    FipsCompliant
}
