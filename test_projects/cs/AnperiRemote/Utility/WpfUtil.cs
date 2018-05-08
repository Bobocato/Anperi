using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AnperiRemote.Utility
{
    static class WpfUtil
    {
        private static string _syncNamesBase;
        private static string MutexName => _syncNamesBase + "_isFirstInstanceMutex";
        private static string EventSignalName => _syncNamesBase + "_notifyFirstInstanceEvent";
        private static Mutex _mutexCheckIfFirstInstance;
        private static EventWaitHandle _evtNotifiedFromOtherProcess;
        private static CancellationTokenSource _cancellationTokenSource;
        private static bool _isFirstInstance;
        private static bool _isInitialized;

        public static event EventHandler SecondInstanceStarted;

        /// <summary>
        /// Initializes all the functionality surrounding the IsFirstInstance and SecondInstance. Be sure to call <see cref="Dispose"/> before the program ends.
        /// </summary>
        /// <param name="systemUniqueIdentifierName">A system unique identifier for this program.</param>
        /// <param name="notifyIfNotFirst">If the first program should be notified that this instance got started.</param>
        public static void Init(string systemUniqueIdentifierName, bool notifyIfNotFirst = true)
        {
            if (_isInitialized) throw new InvalidOperationException("You already initialized this class.");
            _cancellationTokenSource = new CancellationTokenSource();
            _syncNamesBase = systemUniqueIdentifierName;
            bool isFirst = CheckIfFirstInstance(MutexName);
            if (!isFirst && notifyIfNotFirst)
            {
                try
                {
                    _evtNotifiedFromOtherProcess = EventWaitHandle.OpenExisting(EventSignalName);
                    _evtNotifiedFromOtherProcess.Set();
                }
                catch(Exception) { //ignored
                }
            }
            else if (isFirst)
            {
                try
                {
                    _evtNotifiedFromOtherProcess = new EventWaitHandle(false, EventResetMode.ManualReset,
                        EventSignalName, out bool createdNew);
                    if (createdNew)
                    {
                        Task.Run(() => WaitForEvent(_evtNotifiedFromOtherProcess, _cancellationTokenSource.Token));
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException("Error creating event for listening to notify SecondInstanceStarted. The base exception is the innerException.", ex);
                }
            }
            _isInitialized = true;
        }

        private static void WaitForEvent(EventWaitHandle ewh, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ewh.WaitOne();
                if (!token.IsCancellationRequested)
                {
                    Application.Current?.Dispatcher?.Invoke(OnSecondInstanceStarted);
                    ewh.Reset();
                }
            }
        } 

        /// <summary>
        /// Disposes of all used native resources.
        /// </summary>
        public static void Dispose()
        {
            _evtNotifiedFromOtherProcess?.Dispose();
            _mutexCheckIfFirstInstance?.ReleaseMutex();
            _mutexCheckIfFirstInstance?.Dispose();
        }

        /// <summary>
        /// Shows the main window or creates it with the given Type.
        /// </summary>
        /// <typeparam name="TWindow">The type for window to use. Throws an exception if the current Window exists and doesnt have correct type.</typeparam>
        /// <param name="createdNew">If a new window was created or if the MainWindow already had a window.</param>
        /// <returns></returns>
        public static TWindow ShowCreateMainWindow<TWindow>(this Application app, out bool createdNew) where TWindow : Window
        {
            createdNew = app.MainWindow == null;
            if (createdNew)
            {
                TWindow window = Activator.CreateInstance<TWindow>();
                window.Show();
                Application.Current.MainWindow = window;
                return window;
            }
            else
            {
                TWindow currentWindow = app.MainWindow as TWindow;
                if (currentWindow == null)
                {
                    throw new InvalidOperationException(
                        $"The current window is not of the type {typeof(TWindow).FullName}");
                }
                else
                {
                    if (currentWindow.WindowState == WindowState.Minimized) currentWindow.WindowState = WindowState.Normal;
                    currentWindow.Activate();
                }
                return currentWindow;
            }
        }

        /// <summary>
        /// Can only be used after <see cref="Init"/> has been called once.
        /// </summary>
        public static bool IsFirstInstance => _mutexCheckIfFirstInstance == null
            ? throw new InvalidOperationException("You need to call InitSingleInstanceChecks first.")
            : _isFirstInstance;

        /// <summary>
        /// This checks if a mutex with the given name already exists. Effectively checking if this is the first Instance.
        /// For all following checks after the first one use <see cref="IsFirstInstance"/>
        /// </summary>
        /// <param name="mutexName">A system-unique name for your program mutex.</param>
        /// <returns>If this is the first instance.</returns>
        private static bool CheckIfFirstInstance(string mutexName)
        {
            if (_mutexCheckIfFirstInstance != null) throw new InvalidOperationException("You can only call this once. For following checks use IsFirstInstance");
            try
            {
                _mutexCheckIfFirstInstance = new Mutex(false, mutexName);
                _isFirstInstance = _mutexCheckIfFirstInstance.WaitOne(0, false);
            }
            catch (AbandonedMutexException)
            {
                /* ignored because we got it and it doesn't protect any data 
                 * (aside the data of the previous program (from which the UI thread apparently already died)*/
                return true;
            }
            return _isFirstInstance;
        }

        private static void OnSecondInstanceStarted()
        {
            SecondInstanceStarted?.Invoke(null, EventArgs.Empty);
        }
    }
}
