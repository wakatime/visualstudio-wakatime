using System;
using System.Diagnostics;

namespace WakaTime
{
    internal static class ProcessorArchitectureHelper
    {
        private static readonly bool Is64BitProcess = IntPtr.Size == 8;
        internal static bool Is64BitOperatingSystem = Is64BitProcess || InternalCheckIsWow64();

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major != 5 || Environment.OSVersion.Version.Minor < 1) &&
                Environment.OSVersion.Version.Major < 6) return false;

            using (var p = Process.GetCurrentProcess())
            {
                return NativeMethods.IsWow64Process(p.Handle, out var retVal) && retVal;
            }
        }
    }
}