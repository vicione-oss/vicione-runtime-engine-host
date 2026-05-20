using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed partial class TimerFd
{
    private enum ClockId : int
    {
        CLOCK_REALTIME = 0,
        CLOCK_MONOTONIC = 1
    }

    private enum Errno
    {
        EINTR = 4,
        EAGAIN = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Style", "IDE1006:Benennungsstile")]
    private struct TimeSpec
    {
        public long tv_sec;
        public long tv_nsec;

        public static TimeSpec From(TimeSpan time)
        {
            // TimeSpan tick = 100 ns
            var totalNanoseconds = time.Ticks * 100;
            var seconds = totalNanoseconds / 1_000_000_000;
            var nanoseconds = totalNanoseconds % 1_000_000_000;

            return new TimeSpec
            {
                tv_sec = seconds,
                tv_nsec = nanoseconds
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Style", "IDE1006:Benennungsstile")]
    private struct ItimerSpec
    {
        public TimeSpec it_interval; // period
        public TimeSpec it_value;    // next expiry
    }

    private static partial class NativeMethods
    {
        [LibraryImport("libc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int timerfd_create(ClockId clockid, int flags);

        [LibraryImport("libc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int timerfd_settime(int fd, int flags, in ItimerSpec newValue, IntPtr oldValue);

        [LibraryImport("libc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int read(int fd, ref ulong buffer, IntPtr count);

        [LibraryImport("libc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static partial int close(int fd);
    }

    private sealed class SafeFdHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeFdHandle(int fd) : base(ownsHandle: true) => SetHandle(fd);

        protected override bool ReleaseHandle() => NativeMethods.close(handle.ToInt32()) == 0;
    }
}
