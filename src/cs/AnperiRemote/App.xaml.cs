using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AnperiRemote.DAL;
using AnperiRemote.Model;
using AnperiRemote.Utility;
using JJA.Anperi.Utility;
using JJA.Anperi.WpfUtility;
using Microsoft.VisualBasic.Logging;

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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Environment.CurrentDirectory = Util.AssemblyDirectory;

#if DEBUG
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            Trace.Listeners.Add(new FileLogTraceListener("filelistener") { Location = LogFileLocation.Custom, CustomLocation = "logs", Append = true, BaseFileName = "AnperiRemote", AutoFlush = true, TraceOutputOptions = TraceOptions.DateTime, Delimiter = "\t|\t"});
#endif
            Trace.TraceInformation("OnStartup called.");
            AliveTrace();

            SettingsModel _ = SettingsModel.Instance;
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                WpfUtil.Init("jp.anperiremote");
                if (!WpfUtil.IsFirstInstance)
                {
                    Shutdown(0);
                    return;
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (e.IsTerminating)
            {
                MessageBox.Show($"Unhandled exception occured:\n\t{e.ExceptionObject.GetType()}: {ex?.Message ?? "<Error getting exception message>"}\nExiting ...",
                    "Unhandled exception in AnperiRemote", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            Util.TraceException("Unhandled exception in AppDomain", ex);
            try
            {
                TrayHelper.Instance.IconVisible = false;
            }
            catch (Exception)
            {
                //ignored
            }
            Trace.Flush();
            //just log and crash anyways
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
            MessageBox.Show($"Unhandled exception in UI Dispatcher:\n\t{e.Exception.GetType()}: {e.Exception.Message}",
                "Unhandled UI Dispatcher exception in AnperiRemote", MessageBoxButton.OK, MessageBoxImage.Error);
            Util.TraceException("Unhandled exception in UI Dispatcher", e.Exception);
            try
            {
                TrayHelper.Instance.IconVisible = false;
            }
            catch (Exception)
            {
                //ignored
            }
            Trace.Flush();
            //just log and crash anyways
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Trace.TraceInformation("OnExit called.");
            TrayHelper.Instance.IconVisible = false;
            SettingsModel.Instance.Save();
            base.OnExit(e);
        }

        private async void AliveTrace()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Trace.TraceInformation("Still active ...");
            }
        }
    }
}
