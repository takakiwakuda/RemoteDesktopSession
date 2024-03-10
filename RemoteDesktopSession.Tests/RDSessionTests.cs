using System.Diagnostics;
using System.Reflection;
using Windows.Win32.System.RemoteDesktop;

namespace RemoteDesktopSession.Tests;

public class RDSessionTests
{
    private static readonly string LocalServer = Environment.MachineName;

    private static RDSession CreateDefaultSession()
    {
        return RDSession.Current;
    }

    [Fact]
    public void GetSessionById_NegativeSessionId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>("sessionId", () => RDSession.GetSessionById(-1));
    }

    [Fact]
    public void GetSessionById_NotRunningSessionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("sessionId", () => RDSession.GetSessionById(ushort.MaxValue));
    }

    [Fact]
    public void GetSessionById_NegativeSessionIdWithServerName_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>("sessionId",
                                                   () => RDSession.GetSessionById(-1, LocalServer));
    }

    [Fact]
    public void GetSessionById_NullServerName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("serverName", () => RDSession.GetSessionById(1, null));
    }

    [Fact]
    public void GetSessionById_EmptyServerName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("serverName", () => RDSession.GetSessionById(1, ""));
    }

    [Fact]
    public void GetSessions_NullServerName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("serverName", () => RDSession.GetSessions(null));
    }

    [Fact]
    public void GetSessions_EmptyServerName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("serverName", () => RDSession.GetSessions(""));
    }

    [Fact]
    public void Client_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.Client);
    }

    [Fact]
    public void ConnectTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.ConnectTime);
    }

    [Fact]
    public void CurrentTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.CurrentTime);
    }

    [Fact]
    public void DisconnectTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.DisconnectTime);
    }

    [Fact]
    public void DomainName_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.DomainName);
    }

    [Fact]
    public void IdleTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.IdleTime);
    }

    [Fact]
    public void LastInputTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.LastInputTime);
    }

    [Fact]
    public void LogonTime_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.LogonTime);
    }

    [Fact]
    public void SessionName_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.SessionName);
    }

    [Fact]
    public void SessionState_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.SessionState);
    }

    [Fact]
    public void UserName_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.UserName);
    }

    [Fact]
    public void Disconnect_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.Disconnect());
    }

    [Fact]
    public void Logoff_Disposed_ThrowsObjectDisposedException()
    {
        var session = CreateDefaultSession();
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => session.Logoff());
    }

    [Fact]
    public void TestCurrentUserSession()
    {
        using var session = RDSession.Current;

        Assert.Equal(GetCurrentProcessSessionId(), session.SessionId);
        Assert.Equal(Environment.MachineName, session.ServerName);
        Assert.Equal(Environment.UserDomainName, session.DomainName);
        Assert.Equal(Environment.UserName, session.UserName);

        static int GetCurrentProcessSessionId()
        {
            using var process = Process.GetCurrentProcess();
            return process.SessionId;
        }
    }

    [Fact]
    public void TestGetSessionById()
    {
        using var session = CreateDefaultSession();
        using var sessionToTest = RDSession.GetSessionById(session.SessionId);

        Assert.Equal(session.SessionId, sessionToTest.SessionId);
        Assert.Equal(session.SessionName, sessionToTest.SessionName);
        Assert.Equal(session.ServerName, sessionToTest.ServerName);
        Assert.Equal(session.DomainName, sessionToTest.DomainName);
        Assert.Equal(session.UserName, sessionToTest.UserName);
    }

    [Fact]
    public void TestGetSessions()
    {
        using var session = CreateDefaultSession();
        bool sessionExists = false;

        foreach (var sessionToTest in RDSession.GetSessions())
        {
            if (sessionToTest.SessionName == session.SessionName)
            {
                Assert.Equal(session.SessionId, sessionToTest.SessionId);
                Assert.Equal(session.ServerName, sessionToTest.ServerName);
                Assert.Equal(session.DomainName, sessionToTest.DomainName);
                Assert.Equal(session.UserName, sessionToTest.UserName);

                sessionExists = true;
            }
        }

        Assert.True(sessionExists);
    }

    [Fact]
    public void TestRefresh()
    {
        using var session = CreateDefaultSession();

        GenerateFieldValue(session);

        session.Refresh();

        Assert.False((bool)GetPrivateFieldValue(session, "_sessionNameGenerated"));
        Assert.False((bool)GetPrivateFieldValue(session, "_domainNameGenerated"));
        Assert.False((bool)GetPrivateFieldValue(session, "_userNameGenerated"));
        Assert.False((bool)GetPrivateFieldValue(session, "_idleTimeGenerated"));
        Assert.Equal(0, (long)GetPrivateFieldValue(session, "_idleTime"));
        Assert.Null(GetPrivateFieldValue(session, "_sessionInfo"));
        Assert.Null(GetPrivateFieldValue(session, "_clientInfo"));

        static void GenerateFieldValue(RDSession session)
        {
            SetPrivateFieldValue(session, "_sessionNameGenerated", true);
            SetPrivateFieldValue(session, "_domainNameGenerated", true);
            SetPrivateFieldValue(session, "_userNameGenerated", true);
            SetPrivateFieldValue(session, "_idleTimeGenerated", true);
            SetPrivateFieldValue(session, "_idleTime", 1);
            SetPrivateFieldValue(session, "_clientInfo", new WTSCLIENTW());
        }

        static object GetPrivateFieldValue(RDSession session, string name)
        {
            return typeof(RDSession).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                                    .GetValue(session);
        }

        static void SetPrivateFieldValue(RDSession session, string name, object? value)
        {
            typeof(RDSession).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                             .SetValue(session, value);
        }
    }
}
