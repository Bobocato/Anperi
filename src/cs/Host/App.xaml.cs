using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.Host.View;
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
            bool trayActive = true;
            foreach (string arg in e.Args)
            {
                switch (arg.ToLowerInvariant())
                {
                    case "-tray":
                        createUi = false;
                        break;
                    case "-notray":
                        trayActive = false;
                        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                        break;
                    default:
                        Trace.TraceWarning("Got invalid commandline argument. Ignoring '{0}'", arg);
                        break;
                }
            }
            if (!createUi && !trayActive)
            {
                MessageBox.Show("Can't use -tray and -notray together.", "Anperi configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
            if (trayActive)
            {
                TrayHelper.Instance.DoubleClick += TrayIcon_DoubleClick;
                TrayHelper.Instance.ItemExitClick += TrayIcon_ItemExitClick;
                TrayHelper.Instance.IconVisible = true;
            }
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
