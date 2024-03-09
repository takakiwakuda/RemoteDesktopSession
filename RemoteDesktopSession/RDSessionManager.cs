using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace RemoteDesktopSession;

internal static class RDSessionManager
{
    internal static unsafe void ConnectSession(uint logonId, uint targetLogonId, string password, bool wait = true)
    {
        fixed (char* pPassword = password)
        {
            if (!PInvoke.WTSConnectSession(logonId, targetLogonId, pPassword, wait))
            {
                ThrowWin32Error();
            }
        }
    }

    internal static void DisconnectSession(SafeServerHandle handle, uint sessionId, bool wait = true)
    {
        if (!PInvoke.WTSDisconnectSession(handle.HANDLE, sessionId, wait))
        {
            ThrowWin32Error(sessionId);
        }
    }

    internal static WTSCLIENTW GetClientInfo(SafeServerHandle handle, uint sessionId)
    {
        return QuerySessionInfo<WTSCLIENTW>(handle, sessionId, WTS_INFO_CLASS.WTSClientInfo);
    }

    internal static WTSINFOW GetSessionInfo(SafeServerHandle handle, uint sessionId)
    {
        return QuerySessionInfo<WTSINFOW>(handle, sessionId, WTS_INFO_CLASS.WTSSessionInfo);
    }

    internal static unsafe WTSINFOW[] GetSessionInfos(SafeServerHandle handle)
    {
        uint level = 1;
        uint count;
        WTS_SESSION_INFO_1W* pSessionInfos;

        if (!PInvoke.WTSEnumerateSessionsEx(handle.HANDLE, &level, 0, &pSessionInfos, &count))
        {
            ThrowWin32Error();
        }

        WTSINFOW[] sessionInfos = new WTSINFOW[count];

        try
        {
            for (int i = 0; i < count; i++)
            {
                sessionInfos[i] = GetSessionInfo(handle, pSessionInfos[i].SessionId);
            }
        }
        finally
        {
            Debug.Assert(PInvoke.WTSFreeMemoryEx(WTS_TYPE_CLASS.WTSTypeSessionInfoLevel1, pSessionInfos, count));
        }

        return sessionInfos;
    }

    internal static void LogoffSession(SafeServerHandle handle, uint sessionId, bool wait = true)
    {
        if (!PInvoke.WTSLogoffSession(handle.HANDLE, sessionId, wait))
        {
            ThrowWin32Error(sessionId);
        }
    }

    internal static unsafe SafeServerHandle OpenServer(string serverName)
    {
        if (IsLocalServer(serverName))
        {
            return SafeServerHandle.Null;
        }

        fixed (char* pServerName = serverName)
        {
            HANDLE handle = PInvoke.WTSOpenServer(pServerName);
            return new SafeServerHandle(handle, true);
        }
    }

    /// <summary>
    /// Returns a value that indicates whether the specified server is the local server.
    /// </summary>
    /// <param name="serverName">The server name to test.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="serverName"/> is the local server;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsLocalServer(string serverName)
    {
        return serverName.Length == 0
               || serverName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private static unsafe T QuerySessionInfo<T>(SafeServerHandle handle, uint sessionId, WTS_INFO_CLASS @class)
        where T : struct
    {
        PWSTR buffer;
        uint bytesReturned;

        if (!PInvoke.WTSQuerySessionInformation(handle.HANDLE, sessionId, @class, &buffer, &bytesReturned))
        {
            ThrowWin32Error(sessionId);
        }

        try
        {
            return Marshal.PtrToStructure<T>((nint)buffer.Value);
        }
        finally
        {
            PInvoke.WTSFreeMemory(buffer.Value);
        }
    }

    private static void ThrowWin32Error(uint? sessionId = null)
    {
        int error = Marshal.GetLastWin32Error();

        switch ((WIN32_ERROR)error)
        {
            case WIN32_ERROR.ERROR_FILE_NOT_FOUND:
                if (sessionId is null)
                {
                    throw new ArgumentException("No session was found.", nameof(sessionId));
                }
                else
                {
                    throw new ArgumentException($"No session was found for ID '{sessionId}'.", nameof(sessionId));
                }
            case WIN32_ERROR.ERROR_ACCESS_DENIED:
                throw new UnauthorizedAccessException("Access to Remote Desktop Services is not allowed.");
            default:
                throw new Win32Exception(error);
        }
    }
}
