using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.Utility;
using JJA.Anperi.WpfUtility;

namespace JJA.Anperi.Host
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            WpfUtil.Init("AnperiHostApp");
            WpfUtil.SecondInstanceStarted += WpfUtil_SecondInstanceStarted;
            if (!WpfUtil.IsFirstInstance)
            {
                Shutdown();
            }

            bool createUi = true;
            foreach (string arg in e.Args)
            {
                switch (arg)
                {
                    case "-tray":
                        createUi = false;
                        break;
                    default:
                        Trace.TraceWarning("Got invalid commandline argument. Ignoring '{0}'", arg);
                        break;
                }
            }
            TrayHelper.Instance.DoubleClick += TrayIcon_DoubleClick;
            TrayHelper.Instance.ItemExitClick += TrayIcon_ItemExitClick;
            TrayHelper.Instance.IconVisible = true;
            if (createUi) this.ShowCreateMainWindow<MainWindow>();

            base.OnStartup(e);
        }

        private void TrayIcon_ItemExitClick(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.ShowCreateMainWindow<MainWindow>();
        }

        private void WpfUtil_SecondInstanceStarted(object sender, EventArgs e)
        {
            this.ShowCreateMainWindow<MainWindow>();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                TrayHelper.Instance.IconVisible = false;
            }
            catch (Exception)
            {
                //ignored
            }
            Util.TraceException("Unhandled exception in UI dispatcher", e.Exception);
            MessageBox.Show("Error occured in UI thread, exiting ...", "Error in Anperi", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayHelper.Instance.IconVisible = false;
            WpfUtil.Dispose();
            base.OnExit(e);
        }
    }
}
