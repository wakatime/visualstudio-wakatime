using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WakaTime
{
    internal static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool WritePrivateProfileString(
            [MarshalAs(UnmanagedType.LPWStr)] string section,
            [MarshalAs(UnmanagedType.LPWStr)] string key,
            [MarshalAs(UnmanagedType.LPWStr)] string val,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern UInt32 GetPrivateProfileString(
            [MarshalAs(UnmanagedType.LPWStr)] string section,
            [MarshalAs(UnmanagedType.LPWStr)] string key,
            [MarshalAs(UnmanagedType.LPWStr)] string def,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder retVal, int size,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);
    }
}
