using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AnperiRemote.DAL;
using AnperiRemote.Model;
using AnperiRemote.Utility;

namespace AnperiRemote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            bool _ = SettingsModel.Instance.AutostartEnabled;
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                WpfUtil.Init("jp.anperiremote");
                if (!WpfUtil.IsFirstInstance)
                {
                    Shutdown(0);
                }
            }
            catch (Exception)
            {
                //ignored
            }
            WpfUtil.SecondInstanceStarted += WpfUtil_SecondInstanceStarted;
            bool createUi = true;
            foreach (string arg in e.Args)
            {
                switch (arg)
                {
                    case "-tray":
                        createUi = false;
                        break;
                    default:
                        Trace.TraceWarning($"Got invalid argument '{arg}', ignoring ...");
                        break;
                }
            }

            TrayHelper.Instance.IconVisible = true;
            TrayHelper.Instance.DoubleClick += TrayIcon_DoubleClick;
            TrayHelper.Instance.ItemExitClick += TrayIcon_ItemExitClick;

            if (createUi)
            {
                this.ShowCreateMainWindow<MainWindow>(out bool _);
            }

            base.OnStartup(e);
        }

        private void WpfUtil_SecondInstanceStarted(object sender, EventArgs e)
        {
            this.ShowCreateMainWindow<MainWindow>(out bool _);
        }

        private void TrayIcon_ItemExitClick(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.ShowCreateMainWindow<MainWindow>(out bool _);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"Unhandled exception in UI Dispatcher:\n\t{e.Exception.GetType()}: {e.Exception.Message}");
            try
            {
                TrayHelper.Instance.IconVisible = false;
            }
            catch (Exception)
            {
                //ignored
            }
            //just log and crash anyways
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayHelper.Instance.IconVisible = false;
            SettingsModel.Instance.Save();
            base.OnExit(e);
        }
    }
}
