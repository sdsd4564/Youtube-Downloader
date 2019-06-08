using System.Windows;

namespace Youtube_Downloader
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        //public App()
        //{
        //    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
        //}

        //private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        //{
        //    //어셈블리가 .exe파일과 같은 폴더에 있으면 이 메서드는 호출되지 않습니다.
        //    Assembly thisAssembly = Assembly.GetExecutingAssembly();

        //    //1. 어셈블리 이름을 얻습니다.
        //    AssemblyName assemblyName = new AssemblyName(args.Name);
        //    string name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";

        //    //2. Embedded Resources로부터 리소스를 로드합니다.
        //    var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));
        //    if (resources.Any())
        //    {
        //        //거의 대부분의 확률로 항상 1개의 항목만 로드됩니다. 만약 1개이상 로드가되면 이러한 케이스를 별도로 처리해야 합니다.
        //        var resourceName = resources.First();
        //        using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
        //        {
        //            if (stream == null)
        //                return null;
        //            var block = new byte[stream.Length];
        //            stream.Read(block, 0, block.Length);
        //            return Assembly.Load(block);
        //        }
        //    }
        //    return null;
        //}
    }
}