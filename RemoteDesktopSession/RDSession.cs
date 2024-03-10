using Windows.Win32;
using Windows.Win32.System.RemoteDesktop;

namespace RemoteDesktopSession;

/// <summary>
/// Provides access Remote Desktop Services sessions.
/// </summary>
public class RDSession : IDisposable
{
    private static readonly string DefaultServerName = Environment.MachineName;

    /// <summary>
    /// Gets the <see cref="RDSession"/> that represents the current user.
    /// </summary>
    public static RDSession Current
    {
        get
        {
            SafeServerHandle handle = SafeServerHandle.Null;
            WTSINFOW sessionInfo = RDSessionManager.GetSessionInfo(handle, PInvoke.WTS_CURRENT_SESSION);

            return new RDSession(handle, sessionInfo, (int)sessionInfo.SessionId, DefaultServerName);
        }
    }

    /// <summary>
    /// Gets information about the client of the session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public RDConnectionClient Client
    {
        get
        {
            _clientInfo ??= RDSessionManager.GetClientInfo(Handle, (uint)_sessionId);
            return new RDConnectionClient(_clientInfo.Value);
        }
    }

    /// <summary>
    /// Gets the most recent client connection time.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public DateTime? ConnectTime => ConvertHelper.FromFileTime(SessionInfo.ConnectTime);

    /// <summary>
    /// Gets the time that the <see cref="RDSession"/> was refreshed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public DateTime CurrentTime => DateTime.FromFileTime(SessionInfo.CurrentTime);

    /// <summary>
    /// Gets the last client disconnection time.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public DateTime? DisconnectTime => ConvertHelper.FromFileTime(SessionInfo.DisconnectTime);

    /// <summary>
    /// Gets the name of the domain that the user belongs to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public string? DomainName
    {
        get
        {
            if (!_domainNameGenerated)
            {
                unsafe
                {
                    var domain = SessionInfo.Domain;
                    _domainName = MarshalHelper.PtrToStringOrNull(domain.Value);
                }

                _domainNameGenerated = true;
            }

            return _domainName;
        }
    }

    /// <summary>
    /// Gets the session idle time.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public TimeSpan IdleTime
    {
        get
        {
            if (!_idleTimeGenerated)
            {
                if (SessionInfo.LastInputTime > 0)
                {
                    _idleTime = SessionInfo.CurrentTime - SessionInfo.LastInputTime;
                }

                _idleTimeGenerated = true;
            }

            return new TimeSpan(_idleTime);
        }
    }

    /// <summary>
    /// Gets the time of the last user input in the session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public DateTime? LastInputTime => ConvertHelper.FromFileTime(SessionInfo.LastInputTime);

    /// <summary>
    /// Gets the time that the user logged on to the session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public DateTime? LogonTime => ConvertHelper.FromFileTime(SessionInfo.LogonTime);

    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public int SessionId => _sessionId;

    /// <summary>
    /// Gets the name of the session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public string? SessionName
    {
        get
        {
            if (!_sessionNameGenerated)
            {
                unsafe
                {
                    var sessionName = SessionInfo.WinStationName;
                    _sessionName = MarshalHelper.PtrToStringOrNull(sessionName.Value);
                }

                _sessionNameGenerated = true;
            }

            return _sessionName;
        }
    }

    /// <summary>
    /// Gets a value that indicates the session's current connection state.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public SessionState SessionState => (SessionState)SessionInfo.State;

    /// <summary>
    /// Gets the name of the server hosting the session.
    /// </summary>
    public string ServerName => _serverName;

    /// <summary>
    /// Gets the name of the user who owns the session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public string? UserName
    {
        get
        {
            if (!_userNameGenerated)
            {
                unsafe
                {
                    var userName = SessionInfo.UserName;
                    _userName = MarshalHelper.PtrToStringOrNull(userName.Value);
                }

                _userNameGenerated = true;
            }

            return _userName;
        }
    }

    /// <summary>
    /// Gets a <see cref="SafeServerHandle"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    internal SafeServerHandle Handle
    {
        get
        {
            ThrowIfDisposed();

            _handle ??= RDSessionManager.OpenServer(_serverName);
            return _handle;
        }
    }

    /// <summary>
    /// Gets a <see cref="WTSINFOW"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    internal WTSINFOW SessionInfo
    {
        get
        {
            _sessionInfo ??= RDSessionManager.GetSessionInfo(Handle, (uint)_sessionId);
            return _sessionInfo.Value;
        }
    }

    private readonly int _sessionId;
    private readonly string _serverName;
    private string? _domainName;
    private string? _sessionName;
    private string? _userName;
    private long _idleTime;
    private bool _domainNameGenerated;
    private bool _sessionNameGenerated;
    private bool _userNameGenerated;
    private bool _idleTimeGenerated;
    private bool _disposed;
    private SafeServerHandle? _handle;
    private WTSINFOW? _sessionInfo;
    private WTSCLIENTW? _clientInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="RDSession"/> class
    /// for the specified session ID, the server handle,
    /// the session information, and the name of the host server.
    /// </summary>
    /// <param name="handle">The server handle.</param>
    /// <param name="sessionInfo">The session information.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="serverName">The name of the server.</param>
    private RDSession(SafeServerHandle? handle, WTSINFOW sessionInfo, int sessionId, string serverName)
    {
        _handle = handle;
        _sessionInfo = sessionInfo;
        _sessionId = sessionId;
        _serverName = serverName;
    }

    /// <summary>
    /// Gets a <see cref="RDSession"/> with the specified session ID
    /// that exists on the server where the application is running.
    /// </summary>
    /// <param name="sessionId">The session ID to get.</param>
    /// <returns>
    /// A <see cref="RDSession"/> associated with the <paramref name="sessionId"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The session specified by the <paramref name="sessionId"/> is not found.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <paramref name="sessionId"/> is negative.
    /// </exception>
    public static RDSession GetSessionById(int sessionId)
    {
        ThrowHelper.ThrowIfNegative(sessionId, nameof(sessionId));

        SafeServerHandle handle = SafeServerHandle.Null;
        WTSINFOW sessionInfo = RDSessionManager.GetSessionInfo(handle, (uint)sessionId);

        return new RDSession(handle, sessionInfo, sessionId, DefaultServerName);
    }

    /// <summary>
    /// Gets a <see cref="RDSession"/> with the specified session ID
    /// that exists on the specified server.
    /// </summary>
    /// <param name="sessionId">The session ID to get.</param>
    /// <param name="serverName">The name of the server to use.</param>
    /// <returns>
    /// A <see cref="RDSession"/> associated with the <paramref name="sessionId"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The <paramref name="serverName"/> is an empty string.
    /// -or-
    /// The session specified by the <paramref name="sessionId"/> is not found.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="serverName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <paramref name="sessionId"/> is negative.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Unable to access Remote Desktop Services on the <paramref name="serverName"/>.
    /// </exception>
    public static RDSession GetSessionById(int sessionId, string serverName)
    {
        ThrowHelper.ThrowIfNegative(sessionId, nameof(sessionId));
        ThrowHelper.ThrowIfNullOrEmpty(serverName, nameof(serverName));

        SafeServerHandle handle = RDSessionManager.OpenServer(serverName);

        try
        {
            WTSINFOW sessionInfo = RDSessionManager.GetSessionInfo(handle, (uint)sessionId);

            return new RDSession(handle, sessionInfo, sessionId, serverName);
        }
        catch (Exception)
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets an array of <see cref="RDSession"/> that exists on the server where the application is running.
    /// </summary>
    /// <returns>
    /// An array of <see cref="RDSession"/>.
    /// </returns>
    public static RDSession[] GetSessions()
    {
        return GetSessions(SafeServerHandle.Null, DefaultServerName);
    }

    /// <summary>
    /// Gets an array of <see cref="RDSession"/> that exists on the specified server.
    /// </summary>
    /// <param name="serverName">The name of the server to use.</param>
    /// <returns>
    /// An array of <see cref="RDSession"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The <paramref name="serverName"/> is an empty string.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="serverName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Unable to access Remote Desktop Services on the <paramref name="serverName"/>.
    /// </exception>
    public static RDSession[] GetSessions(string serverName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(serverName, nameof(serverName));

        using SafeServerHandle handle = RDSessionManager.OpenServer(serverName);
        return GetSessions(handle, serverName);
    }

    /// <summary>
    /// Gets an array of <see cref="RDSession"/> with the specified server handle.
    /// </summary>
    /// <param name="handle">The server handle to use.</param>
    /// <param name="serverName">The name of the server to use.</param>
    /// <returns>
    /// An array of <see cref="RDSession"/>.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Unable to access Remote Desktop Services on the <paramref name="serverName"/>.
    /// </exception>
    private static RDSession[] GetSessions(SafeServerHandle handle, string serverName)
    {
        WTSINFOW[] sessionInfos = RDSessionManager.GetSessionInfos(handle);
        RDSession[] sessions = new RDSession[sessionInfos.Length];
        WTSINFOW sessionInfo;

        for (int i = 0; i < sessions.Length; i++)
        {
            sessionInfo = sessionInfos[i];
            sessions[i] = new RDSession(null, sessionInfo, (int)sessionInfo.SessionId, serverName);
        }

        return sessions;
    }

    /// <summary>
    /// Releases all resources used by this <see cref="RDSession"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Release the unmanaged resources used by this <see cref="RDSession"/>
    /// and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources;
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing)
        {
            _handle?.Dispose();
            _handle = null;

            Refresh();
        }
    }

    /// <summary>
    /// Disconnects the user associated with the this session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public void Disconnect()
    {
        RDSessionManager.DisconnectSession(Handle, (uint)_sessionId);
    }

    /// <summary>
    /// Logs off the user associated with the this session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    public void Logoff()
    {
        RDSessionManager.LogoffSession(Handle, (uint)_sessionId);
    }

    /// <summary>
    /// Refreshes the state of this object.
    /// </summary>
    public void Refresh()
    {
        _domainNameGenerated = false;
        _sessionNameGenerated = false;
        _userNameGenerated = false;
        _idleTimeGenerated = false;
        _idleTime = 0;
        _sessionInfo = null;
        _clientInfo = null;
    }

    /// <summary>
    /// Throws an exception if this object has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="RDSession"/> has been disposed.
    /// </exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
