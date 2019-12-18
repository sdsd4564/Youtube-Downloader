using System;
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
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
            if (!File.Exists("./youtube-dl.exe"))
                File.WriteAllBytes("./youtube-dl.exe", Youtube_Downloader.Properties.Resources.youtube_dl);
            if (!File.Exists("./ffmpeg.exe"))
                File.WriteAllBytes("./ffmpeg.exe", Youtube_Downloader.Properties.Resources.ffmpeg);
        }

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
    }
}