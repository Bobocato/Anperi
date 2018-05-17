using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnperiRemote.Utility
{
    static class ShutdownHelper
    {
        public static void Shutdown(uint timeout)
        {
            PInvokeImports.OpenProcessToken(Process.GetCurrentProcess().Handle,
                PInvokeImports.TokenAccessRights.TOKEN_ADJUST_PRIVILEGES | PInvokeImports.TokenAccessRights.TOKEN_QUERY,
                out IntPtr hToken);
            PInvokeImports.TokenPrivileges privs = new PInvokeImports.TokenPrivileges
            {
                PrivilegeCount = 1,
                Privileges = new[] { new PInvokeImports.LUIDAndAttributes { Attributes = PInvokeImports.SE_PRIVILEGE_ENABLED } }
            };

            Marshal.ThrowExceptionForHR(PInvokeImports.LookupPrivilegeValue("", PInvokeImports.SE_SHUTDOWN_NAME, out privs.Privileges[0].Luid));
            Marshal.ThrowExceptionForHR(PInvokeImports.AdjustTokenPrivileges(hToken, false, ref privs, 0U, IntPtr.Zero, IntPtr.Zero));
            Marshal.ThrowExceptionForHR(PInvokeImports.InitiateSystemShutdownEx(null, "Shutdown invoked by AnperiRemote.", timeout, true, false,
                PInvokeImports.ShutdownReason.SHTDN_REASON_NORMAL_PLANNED));
        }

        public static void AbortShutdown()
        {
            Marshal.ThrowExceptionForHR(PInvokeImports.AbortSystemShutdown());
        }
    }
}
