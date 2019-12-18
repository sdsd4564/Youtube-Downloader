using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Youtube_Downloader
{
    public partial class ProgressDialog : Window
    {
        public static ProgressDialog Instance;

        public Task<ProcessAsyncHelper.ProcessResult> process;

        public ProgressDialog(string arg)
        {
            Instance = this;

            // 다운로드 실행
            process = ProcessAsyncHelper.RunProcessAsync("./youtube-dl.exe", arg, 600000);

            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}