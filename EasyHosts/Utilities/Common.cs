using System;
using System.Diagnostics;
using System.Windows.Media;

namespace EasyHosts.Utilities
{
    internal sealed class Common
    {
        internal static bool TryFlushDns(out string flushError)
        {
            bool flushed;
            flushError = string.Empty;

            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        FileName = "ipconfig.exe",
                        Arguments = "/flushdns"
                    }
                };
                p.Start();
                p.WaitForExit();
                flushed = true;
                //string output = p.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                flushed = false;
                flushError = ex.Message;
            }

            return flushed;
        }

        internal static void TryStartProcess(string cmd, string args = "")
        {
            try
            {
                Process.Start(cmd.Trim(), args);
            }
            catch //(Exception)
            {
                // ignored
            }
        }

        internal static Brush GetColorFromHex(string hexCode)
        {
            return new BrushConverter().ConvertFromString(hexCode) as SolidColorBrush;
        }

    }
}