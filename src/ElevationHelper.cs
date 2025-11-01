using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace RCleaner
{
    public static class ElevationHelper
    {
        [SupportedOSPlatform("windows")]
        public static bool IsElevated()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        [SupportedOSPlatform("windows")]
        public static bool RelaunchAsAdmin(string actionName)
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exe)) return false;

                var psi = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = $"--elevatedAction \"{actionName}\""
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
