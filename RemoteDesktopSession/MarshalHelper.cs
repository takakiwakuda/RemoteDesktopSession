namespace RemoteDesktopSession;

internal static class MarshalHelper
{
    internal static unsafe string PtrToString(char* value)
    {
        int length = GetLength(value);
        return length == 0 ? string.Empty : new string(value, 0, length);
    }

    internal static unsafe string? PtrToStringOrNull(char* value)
    {
        int length = GetLength(value);
        return length == 0 ? null : new string(value, 0, length);
    }

    private static unsafe int GetLength(char* value)
    {
        int length = 0;

        while (*value != '\0')
        {
            length++;
            value++;
        }

        return length;
    }
}
