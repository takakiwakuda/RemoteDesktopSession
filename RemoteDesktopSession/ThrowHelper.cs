namespace RemoteDesktopSession;

internal static class ThrowHelper
{
    internal static void ThrowIfNegative(int value, string paramName)
    {
        if (value < 0)
        {
            string message = $"{paramName} ('{value}') must be a non-negative value.";
            throw new ArgumentOutOfRangeException(paramName, value, message);
        }
    }

    internal static void ThrowIfNull(object? argument, string paramName)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    internal static void ThrowIfNullOrEmpty(string? argument, string paramName)
    {
        if (string.IsNullOrEmpty(argument))
        {
            ThrowIfNull(argument, paramName);
            throw new ArgumentException("The value cannot be an empty string.", paramName);
        }
    }
}
