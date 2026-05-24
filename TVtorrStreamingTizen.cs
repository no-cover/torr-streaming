using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Tizen.Applications;

namespace torr_streaming
{
    class Program : ServiceApplication
    {
        private Process _torrProc;
        private string _binaryPath;

        protected override void OnCreate()
        {
            base.OnCreate();

            StageBinary();
            StartTorrServer();

        }

        private void StageBinary()
        {
            string dataDir = Application.Current.DirectoryInfo.Data;
            string resDir = Application.Current.DirectoryInfo.Resource;

            _binaryPath = Path.Combine(dataDir, "TorrServer-linux-arm7");

            string src = Path.Combine(resDir, "TorrServer-linux-arm7");

            File.Copy(src, _binaryPath, true);

            chmod(_binaryPath, 0x1ED);
        }

        private void StartTorrServer()
        {
            if (_torrProc != null && !_torrProc.HasExited)
                return;

            var psi = new ProcessStartInfo
            {
                FileName = _binaryPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            psi.Arguments =
                "--port=8090 " +
                "--path=" + Application.Current.DirectoryInfo.Data;

            _torrProc = Process.Start(psi);

            _torrProc.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Tizen.Log.Info("TorrServer", e.Data);
            };

            _torrProc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Tizen.Log.Error("TorrServer", e.Data);
            };

            _torrProc.BeginOutputReadLine();
            _torrProc.BeginErrorReadLine();

            Tizen.Log.Info("TorrServer", "PID=" + _torrProc.Id);

            string resDir = Application.Current.DirectoryInfo.Resource;
            string versionPath = Path.Combine(resDir, "version");

            string version = "unknown";

            if (File.Exists(versionPath))
            {
                version = File.ReadAllText(versionPath).Trim();
            }

            LaunchToast(
                "TORRSERVER STARTED! \u00B7 Server version: " + version,
                10
            );
        }
        private void LaunchToast(string message, int timeout = 10)
        {
            try
            {
                AppControl ctrl = new AppControl();

                ctrl.ApplicationId = "org.tizen.alert-syspopup";

                ctrl.ExtraData.Add("type", "toast");
                ctrl.ExtraData.Add("timeout", timeout.ToString());
                ctrl.ExtraData.Add("text", message);

                AppControl.SendLaunchRequest(ctrl);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("TorrServer", ex.ToString());
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string path, uint mode);

        protected override void OnTerminate()
        {
            try
            {
                if (_torrProc != null && !_torrProc.HasExited)
                    _torrProc.Kill();
            }
            catch { }

            base.OnTerminate();
        }

        static void Main(string[] args)
        {
            var myProgram = new Program();
            myProgram.Run(args);
        }
    }
}