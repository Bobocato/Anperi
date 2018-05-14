using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnperiRemote.Utility
{
    static class PInvokeImports
    {
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        public const uint SE_PRIVILEGE_ENABLED = 2;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int InitiateSystemShutdownEx([Optional] string lpMachineName, [Optional] string lpMessage, 
            uint dwTimeout, bool bForceAppsClosed, bool bRebootAfterShutdown, ShutdownReason dwReason);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int AbortSystemShutdown([Optional] string lpMaschineName);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int AdjustTokenPrivileges(IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)]bool disableAllPrivileges,
            ref TokenPrivileges newState,
            UInt32 bufferLength,
            IntPtr previousState,
            IntPtr returnLength);

        [DllImport("advapi32.dll")]
        public static extern int OpenProcessToken(IntPtr processHandle, TokenAccessRights desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll")]
        public static extern int LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        public struct TokenPrivileges
        {
            public int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUIDAndAttributes[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct LUIDAndAttributes
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]

        public struct LUID
        {

            public UInt32 LowPart;
            public Int32 HighPart;
        }

        [Flags]
        public enum TokenAccessRights : int
        {
            TOKEN_QUERY = 0x0008,
            TOKEN_ADJUST_PRIVILEGES = 0x0020
        }

        [Flags]
        public enum ShutdownReason : uint
        {
            // Microsoft major reasons.
            SHTDN_REASON_MAJOR_OTHER = 0x00000000,
            SHTDN_REASON_MAJOR_NONE = 0x00000000,
            SHTDN_REASON_MAJOR_HARDWARE = 0x00010000,
            SHTDN_REASON_MAJOR_OPERATINGSYSTEM = 0x00020000,
            SHTDN_REASON_MAJOR_SOFTWARE = 0x00030000,
            SHTDN_REASON_MAJOR_APPLICATION = 0x00040000,
            SHTDN_REASON_MAJOR_SYSTEM = 0x00050000,
            SHTDN_REASON_MAJOR_POWER = 0x00060000,
            SHTDN_REASON_MAJOR_LEGACY_API = 0x00070000,

            // Microsoft minor reasons.
            SHTDN_REASON_MINOR_OTHER = 0x00000000,
            SHTDN_REASON_MINOR_NONE = 0x000000ff,
            SHTDN_REASON_MINOR_MAINTENANCE = 0x00000001,
            SHTDN_REASON_MINOR_INSTALLATION = 0x00000002,
            SHTDN_REASON_MINOR_UPGRADE = 0x00000003,
            SHTDN_REASON_MINOR_RECONFIG = 0x00000004,
            SHTDN_REASON_MINOR_HUNG = 0x00000005,
            SHTDN_REASON_MINOR_UNSTABLE = 0x00000006,
            SHTDN_REASON_MINOR_DISK = 0x00000007,
            SHTDN_REASON_MINOR_PROCESSOR = 0x00000008,
            SHTDN_REASON_MINOR_NETWORKCARD = 0x00000000,
            SHTDN_REASON_MINOR_POWER_SUPPLY = 0x0000000a,
            SHTDN_REASON_MINOR_CORDUNPLUGGED = 0x0000000b,
            SHTDN_REASON_MINOR_ENVIRONMENT = 0x0000000c,
            SHTDN_REASON_MINOR_HARDWARE_DRIVER = 0x0000000d,
            SHTDN_REASON_MINOR_OTHERDRIVER = 0x0000000e,
            SHTDN_REASON_MINOR_BLUESCREEN = 0x0000000F,
            SHTDN_REASON_MINOR_SERVICEPACK = 0x00000010,
            SHTDN_REASON_MINOR_HOTFIX = 0x00000011,
            SHTDN_REASON_MINOR_SECURITYFIX = 0x00000012,
            SHTDN_REASON_MINOR_SECURITY = 0x00000013,
            SHTDN_REASON_MINOR_NETWORK_CONNECTIVITY = 0x00000014,
            SHTDN_REASON_MINOR_WMI = 0x00000015,
            SHTDN_REASON_MINOR_SERVICEPACK_UNINSTALL = 0x00000016,
            SHTDN_REASON_MINOR_HOTFIX_UNINSTALL = 0x00000017,
            SHTDN_REASON_MINOR_SECURITYFIX_UNINSTALL = 0x00000018,
            SHTDN_REASON_MINOR_MMC = 0x00000019,
            SHTDN_REASON_MINOR_TERMSRV = 0x00000020,

            // Flags that end up in the event log code.
            SHTDN_REASON_FLAG_USER_DEFINED = 0x40000000,
            SHTDN_REASON_FLAG_PLANNED = 0x80000000,
            SHTDN_REASON_UNKNOWN = SHTDN_REASON_MINOR_NONE,
            SHTDN_REASON_LEGACY_API = (SHTDN_REASON_MAJOR_LEGACY_API | SHTDN_REASON_FLAG_PLANNED),

            //usual flag combination
            SHTDN_REASON_NORMAL_PLANNED = (SHTDN_REASON_MAJOR_OTHER | SHTDN_REASON_MINOR_OTHER | SHTDN_REASON_FLAG_PLANNED),

            // This mask cuts out UI flags.
            SHTDN_REASON_VALID_BIT_MASK = 0xc0ffffff
        }

    }
}
