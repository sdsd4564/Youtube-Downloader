using System.Windows;

namespace Youtube_Downloader
{
    public partial class ProgressDialog : Window
    {
        public static ProgressDialog Instance;

        public ProgressDialog(string arg)
        {
            Instance = this;

            // 다운로드 실행
            _ = ProcessAsyncHelper.RunProcessAsync("./youtube-dl.exe", arg, 600000);

            InitializeComponent();
        }
    }
}