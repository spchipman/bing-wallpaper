using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

enum Locations
{
    local,
    enAU,
    enCA,
    frCA,
    zhCN,
    deDE,
    esES,
    frFR,
    enIN,
    itIT,
    jaJP,
    enNZ,
    enUK,
    enUS
}

namespace BingWallpaper
{
    public partial class MainForm : Form
    {
        private BingImageProvider _provider;
        private Settings _settings;
        private Image _currentWallpaper;

        public MainForm(BingImageProvider provider, Settings settings)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            _provider = provider;

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            _settings = settings;
            
            // Register application with registry
            SetStartup(_settings.LaunchOnStartup);

            AddTrayIcons();
            
            // Set wallpaper every 24 hours
            var timer = new System.Timers.Timer();
            timer.Interval = 1000 * 60 * 60 * 24; // 24 hours
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += (s, e) => SetWallpaper();
            timer.Start();

            // Set wallpaper on first run
            SetWallpaper();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        /// <summary>
        /// SetStartup will set the application to automatically launch on startup if launch is true,
        /// else it will prevent it from doing so.
        /// </summary>
        public void SetStartup(bool launch)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (launch)
            {
                if (rk.GetValue("BingWallpaper") == null)
                    rk.SetValue("BingWallpaper", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BingWallpaper.exe"));
            }
            else
            {
                if (rk.GetValue("BingWallpaper") != null)
                    rk.DeleteValue("BingWallpaper");
            }
        }

        /// <summary>
        /// SetWallpaper fetches the wallpaper from Bing and sets it
        /// </summary>
        public async void SetWallpaper()
        {
            try
            {
                string mkt = "";
                if (!_settings.Location.Equals("local"))
                {
                    mkt = "&mkt=" + _settings.Location.Substring(0, 2) + "-" + _settings.Location.Substring(2, 2);
                }
                var bingImg = await _provider.GetImage(mkt);

                Wallpaper.Set(bingImg.Img, Wallpaper.Style.Stretched);
                _currentWallpaper = bingImg.Img;
                SetCopyrightTrayLabel(bingImg.Copyright, bingImg.CopyrightLink, bingImg.Title, bingImg.Quiz);

                ShowSetWallpaperNotification();
            }
            catch
            {
                ShowErrorNotification();
            }
        }

        public void SetCopyrightTrayLabel(string copyright, string copyrightLink, string title, string quiz)
        {
            _settings.ImageCopyright = copyright;
            _settings.ImageCopyrightLink = copyrightLink;
            _settings.ImageTitle = title;
            _settings.ImageQuiz = quiz;

            _copyrightLabel.Text = copyright;
            _copyrightLabel.Tag = copyrightLink;

            _titleLabel.Text = title;
            _titleLabel.Tag = quiz;
        }

        #region Tray Icons

        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;
        private MenuItem _copyrightLabel;
        private MenuItem _titleLabel;

        public void AddTrayIcons()
        {
            // Create a simple tray menu with only one item.
            _trayMenu = new ContextMenu();

            // Copyright button
            _copyrightLabel = new MenuItem("Bing Wallpaper");
            _copyrightLabel.Click += (s, e) =>
            {
                if (((MenuItem)s).Tag != null)
                {
                    var url = ((MenuItem)s).Tag.ToString();
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                        System.Diagnostics.Process.Start(url);
                }
            };
            _trayMenu.MenuItems.Add(_copyrightLabel);

            // Title button
            _titleLabel = new MenuItem("");
            _titleLabel.Click += (s, e) =>
            {
                if (((MenuItem)s).Tag != null)
                {
                    var url = ((MenuItem)s).Tag.ToString();
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                        System.Diagnostics.Process.Start(url);
                }
            };
            _trayMenu.MenuItems.Add(_titleLabel);

            // Separator
            _trayMenu.MenuItems.Add("-");
        
            // Force update button
            _trayMenu.MenuItems.Add("Force Update", (s, e) => SetWallpaper());

            // Save image button
            var save = new MenuItem("Save Wallpaper");
            save.Click += (s, e) =>
            {
                if (_currentWallpaper != null)
                {
                    var fileName = string.Join("_", _settings.ImageCopyright.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
                    var dialog = new SaveFileDialog
                    {
                        DefaultExt = "jpg",
                        Title = "Save current wallpaper",
                        FileName = fileName,
                        Filter = "Jpeg Image|*.jpg",
                    };
                    if (dialog.ShowDialog() == DialogResult.OK && dialog.FileName != "")
                    {
                        _currentWallpaper.Save(dialog.FileName, ImageFormat.Jpeg);
                        System.Diagnostics.Process.Start(dialog.FileName);
                    }
                }
            };
            _trayMenu.MenuItems.Add(save);

            // Set Location
            var location = new MenuItem("Set Location");
            var allLocations = (Locations[]) Enum.GetValues(typeof(Locations));
            foreach (var item in allLocations)
            {
                var mi = new MenuItem(item.ToString());
                mi.RadioCheck = true;
                mi.Checked = item.ToString().Equals(_settings.Location);
                mi.Click += OnChangeLocation;
                location.MenuItems.Add(mi);
            }
            _trayMenu.MenuItems.Add(location);

            // Launch on startup button
            var launch = new MenuItem("Launch on Startup");
            launch.Checked = _settings.LaunchOnStartup;
            launch.Click += OnStartupLaunch;
            _trayMenu.MenuItems.Add(launch);

            // Separator
            _trayMenu.MenuItems.Add("-");

            _trayMenu.MenuItems.Add("Exit", (s, e) => Application.Exit());

            // Create a tray icon. Here we are setting the tray icon to be the same as the application's icon
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Bing Wallpaper";
            _trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // open tray icon on left click
            _trayIcon.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    mi.Invoke(_trayIcon, null);
                }
            };

            // Add menu to tray icon and show it.
            _trayIcon.ContextMenu = _trayMenu;
            _trayIcon.Visible = true;
        }

        private void OnStartupLaunch(object sender, EventArgs e)
        {
            var launch = (MenuItem)sender;
            launch.Checked = !launch.Checked;
            SetStartup(launch.Checked);
            _settings.LaunchOnStartup = launch.Checked;
        }

        private void OnChangeLocation(object sender, EventArgs e)
        {
            var location = (MenuItem)sender;
            foreach (MenuItem item in location.Parent.MenuItems)
            {
                item.Checked = false;
            }
            location.Checked = true;
            _settings.Location = location.Text;
            SetWallpaper();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                _trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        #endregion

        #region Notifications

        private void ShowSetWallpaperNotification()
        {
            _trayIcon.BalloonTipText = "Wallpaper has been set to Bing's image of the day!";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(5000);
        }

        private void ShowErrorNotification()
        {
            _trayIcon.BalloonTipText = "Could not update wallpaper, please check your internet connection.";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Error;
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(5000);
        }

        #endregion
    }
}
