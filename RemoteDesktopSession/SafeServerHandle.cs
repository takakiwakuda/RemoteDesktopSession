using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace RemoteDesktopSession;

internal sealed class SafeServerHandle : SafeHandle
{
    public static SafeServerHandle Null => new(0, false);

    public override bool IsInvalid => false;

    public HANDLE HANDLE => (HANDLE)handle;

    public SafeServerHandle(nint preexistingHandle, bool ownsHandle) : base(preexistingHandle, ownsHandle)
    {
        SetHandle(preexistingHandle);
    }

    public SafeServerHandle(HANDLE preexistingHandle, bool ownsHandle) : this((nint)preexistingHandle, ownsHandle)
    {
    }

    protected override bool ReleaseHandle()
    {
        PInvoke.WTSCloseServer((HANDLE)handle);
        return true;
    }
}
