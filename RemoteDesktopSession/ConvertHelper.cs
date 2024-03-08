namespace RemoteDesktopSession;

internal static class ConvertHelper
{
    internal static DateTime? FromFileTime(long fileTime)
    {
        return fileTime == 0 ? null : DateTime.FromFileTime(fileTime);
    }
}
