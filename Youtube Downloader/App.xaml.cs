using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Youtube_Downloader
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        /// Youtube-DL 경로
        public string YoutubeDlPath { get; }

        /// FFmpeg 경로
        public string FFmpegPath { get; }

        /// 설정 파일 경로(Download 경로)
        private string ConfigPath { get; }

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);

            var name = Assembly.GetEntryAssembly().GetName().Name;
            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var binPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), name, version);
            Directory.CreateDirectory(binPath);

            YoutubeDlPath = Path.Combine(binPath, "youtube-dl.exe");
            FFmpegPath = Path.Combine(binPath, "ffmpeg.exe");
            ConfigPath = Path.Combine(binPath, name + ".config");

            /// 다운로드 경로 첫 설정 시 \Users\{UserName}\Downloads로 설정
            if (string.IsNullOrEmpty(GetConfig("DownloadPath")))
                SetConfig("DownloadPath", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");

            if (!File.Exists(YoutubeDlPath))
                File.WriteAllBytes(YoutubeDlPath, Youtube_Downloader.Properties.Resources.youtube_dl);
            if (!File.Exists(FFmpegPath))
                File.WriteAllBytes(FFmpegPath, Youtube_Downloader.Properties.Resources.ffmpeg);
        }

        /// dll 파일 리소스 embedded
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";

            var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));

            if (resources.Count() > 0)
            {
                string resourceName = resources.First();
                using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] assembly = new byte[stream.Length];
                        stream.Read(assembly, 0, assembly.Length);
                        Console.WriteLine("Dll file load : " + resourceName);
                        return Assembly.Load(assembly);
                    }
                }
            }

            return null;
        }

        #region 설정(.config)  관리

        public void SetConfig(string key, string value)
        {
            try
            {
                Configuration config = OpenConfiguration(ConfigPath);

                if (config.AppSettings.Settings.AllKeys.Contains(key))
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            }
            catch { }
        }

        public string GetConfig(string key)
        {
            Configuration config = OpenConfiguration(ConfigPath);

            string val = string.Empty;

            if (config.AppSettings.Settings.AllKeys.Contains(key))
                val = config.AppSettings.Settings[key].Value;

            return val;
        }

        private Configuration OpenConfiguration(string configFile)

        {
            Configuration config;
            if (string.IsNullOrWhiteSpace(configFile))
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            else
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = configFile;
                config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            }

            return config;
        }

        #endregion 설정(.config)  관리
    }
}