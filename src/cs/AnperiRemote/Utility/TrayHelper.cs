﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnperiRemote.Utility
{
    class TrayHelper
    {
        private static readonly Lazy<TrayHelper> _instance = new Lazy<TrayHelper>();
        public static TrayHelper Instance => _instance.Value;
        private static string ResourceName => "AnperiRemote.Resources.tray.ico";

        private NotifyIcon _trayIcon;


        public NotifyIcon CreateIcon(string iconResourceName)
        {
            if (_trayIcon == null)
            {
                _trayIcon = new NotifyIcon();
                _trayIcon.DoubleClick += OnDoubleClick;
                var menu = new ContextMenu();
                menu.MenuItems.Add(new MenuItem("Exit", OnItemExitClick));
                _trayIcon.ContextMenu = menu;
            }
            _trayIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(iconResourceName) ?? throw new InvalidOperationException("Error reading resource stream."));
            return _trayIcon;
        }

        public bool IconVisible
        {
            get => _trayIcon?.Visible ?? false;
            set
            {
                if (value)
                {
                    if (_trayIcon == null)
                    {
                        _trayIcon = CreateIcon(ResourceName);
                    }
                    _trayIcon.Visible = true;
                }
                else
                {
                    if (_trayIcon != null) _trayIcon.Visible = false;
                }
            }
        } 

        public event EventHandler ItemExitClick;
        public event EventHandler DoubleClick;

        protected virtual void OnItemExitClick(object icon, EventArgs e)
        {
            ItemExitClick?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDoubleClick(object icon, EventArgs e)
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
