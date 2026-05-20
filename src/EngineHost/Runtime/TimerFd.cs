using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace ViciOne.ManagedEngine.Runtime;

[SupportedOSPlatform("linux")]
internal sealed partial class TimerFd : IDisposable
{
    private readonly SafeFdHandle _handle;

    internal TimerFd()
    {
        var fd = NativeMethods.timerfd_create(ClockId.CLOCK_MONOTONIC, 0);
        if (fd == -1)
            ThrowLastError("timerfd_create failed");

        _handle = new SafeFdHandle(fd);
    }

    /// <summary>
    /// Arm or rearm the timer.
    /// Pass null to disarm.
    /// </summary>
    internal void SetPeriod(TimeSpan? dueTime, TimeSpan? period = null)
    {
        var spec = new ItimerSpec();

        if (dueTime.HasValue)
            spec.it_value = TimeSpec.From(dueTime.Value);

        if (period.HasValue)
            spec.it_interval = TimeSpec.From(period.Value);

        var result = NativeMethods.timerfd_settime(Fd, 0, in spec, IntPtr.Zero);
        if (result == -1)
            ThrowLastError("timerfd_settime failed");
    }

    /// <summary>
    /// Blocks until the timer expires and returns the number of expirations.
    /// This is a pure blocking read from the fd. No async, no Task.
    /// Returns 0 if cancel could be regarded.
    /// </summary>
    internal ulong WaitForNextExpiry(CancellationToken cancellationToken)
    {
        ulong expirations = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = NativeMethods.read(Fd, ref expirations, sizeof(ulong));
            if (read == sizeof(ulong))
                return expirations;

            var errno = Marshal.GetLastPInvokeError();

            // Retry on EINTR
            if (errno == (int)Errno.EINTR)
                continue;

            // If nonblocking was requested and nothing is there.
            if (errno == (int)Errno.EAGAIN)
                continue;

            ThrowWithErrno("read(timerfd) failed", errno);
        }

        return 0;
    }

    public void Dispose()
        => _handle.Dispose();

    private int Fd => _handle.DangerousGetHandle().ToInt32();

    private static void ThrowLastError(string message)
    {
        var errno = Marshal.GetLastPInvokeError();
        ThrowWithErrno(message, errno);
    }

    private static void ThrowWithErrno(string message, int errno)
        => throw new System.ComponentModel.Win32Exception(errno, message);
}
