﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Youtube_Downloader
{
    public partial class ProgressDialog : MetroWindow
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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!process.IsCompleted)
            {
                var message = this.ShowModalMessageExternal("Stop", "해당 파일을 받고 있습니다. 종료할까요?", MessageDialogStyle.AffirmativeAndNegative);
                if (message == MessageDialogResult.Affirmative)
                    ProcessAsyncHelper.StopProcess();
                else
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}