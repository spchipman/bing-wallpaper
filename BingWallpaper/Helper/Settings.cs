using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace BingWallpaper
{
    public class Settings
    {
        private Options _options;
        private string _settingsPath;

        public Settings()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");

            try
            {
                using (var stream = new FileStream(_settingsPath, FileMode.Open))
                {
                    var ser = new DataContractJsonSerializer(typeof(Options));
                    _options = (Options)ser.ReadObject(stream);
                }
            }
            catch (FileNotFoundException)
            {
                _options = new Options();
            }
            catch (SerializationException)
            {
                _options = new Options();
            }
        }

        #region User settings

        public bool LaunchOnStartup
        {
            get { return _options.LaunchOnStartup; }
            set
            {
                _options.LaunchOnStartup = value;
                Save();
            }
        }

        public bool AutoSaveWallpaperToPictures
        {
            get { return _options.AutoSaveWallpaperToPictures; }
            set
            {
                _options.AutoSaveWallpaperToPictures = value;
                Save();
            }
        }

        public bool PauseSettingWallpaper
        {
            get { return _options.PauseSettingWallpaper; }
            set
            {
                _options.PauseSettingWallpaper = value;
                Save();
            }
        }

        public string Location
        {
            get { return _options.Location; }
            set
            {
                _options.Location = value;
                Save();
            }
        }

        #endregion

        private void Save()
        {
            using (var stream = new FileStream(_settingsPath, FileMode.Create))
            {
                var ser = new DataContractJsonSerializer(typeof(Options));
                ser.WriteObject(stream, _options);
            }
        }

        [DataContract]
        private class Options
        {
            [DataMember]
            public bool AutoSaveWallpaperToPictures = false;
            [DataMember]
            public bool PauseSettingWallpaper = false;
            [DataMember]
            public bool LaunchOnStartup = true;
            [DataMember]
            public string Location = "local";
        }
    }
}
