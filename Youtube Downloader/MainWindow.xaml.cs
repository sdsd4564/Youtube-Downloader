using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Youtube_Downloader.Model;

namespace Youtube_Downloader
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        // youtube-dl argument용
        private string url = string.Empty;

        private string youtubeDl = string.Empty;
        private string ffmpeg = string.Empty;

        // 영상 정보 받아오기 타임아웃 시간
        private readonly int TIEMOUT = 60000;

        private Task updateProcess;

        // 파일 다운로드 경로
        public string DownloadPath
        {
            get => ((App)Application.Current).GetConfig("DownloadPath");
        }

        // ComboBox용 비디오 및 오디오 포맷목록(Binding)
        public ObservableCollection<YoutubeFileFormat> VideoFormats { get; set; } = new ObservableCollection<YoutubeFileFormat>();

        public ObservableCollection<YoutubeFileFormat> AudioFormats { get; set; } = new ObservableCollection<YoutubeFileFormat>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            youtubeDl = ((App)Application.Current).YoutubeDlPath;
            ffmpeg = ((App)Application.Current).FFmpegPath;

            if (!File.Exists(youtubeDl) || !File.Exists(ffmpeg))
            {
                MessageBox.Show("youtube-dl 또는 ffmpeg 파일이 존재하지 않습니다.", "NO FILES");
                Application.Current.Shutdown();
            }
            else
            {
                updateProcess = ProcessAsyncHelper.RunProcessAsync(youtubeDl, "--update", TIEMOUT);
                updateProcess.ContinueWith((t) =>
                    Dispatcher.Invoke(() =>
                    {
                        mtProgressUpdate.Visibility = Visibility.Hidden;
                        btnUrl.IsEnabled = true;
                    }));
            }

            tbxUrl.Focus();
            btnDownloadPath.Click += SetDownloadPath;
            btnUrl.Click += GetUrlInfoClick;
            btnDown.Click += DownloadVideo;
        }

        #region Event

        private void SetDownloadPath(object sender, RoutedEventArgs e)
        {
            var fd = new System.Windows.Forms.FolderBrowserDialog();
            fd.ShowDialog();

            ((App)Application.Current).SetConfig("DownloadPath", fd.SelectedPath);
            OnPropertyChanged("DownloadPath");
        }

        /// Youtube 정보 받아오기 Button Click
        private async void GetUrlInfoClick(object sender, RoutedEventArgs e)
        {
            // youtube-dl 업데이트 작업 시 이벤트 취소
            if (!updateProcess.IsCompleted)
                return;

            // Only Access URL Form
            Regex regex = new Regex("^http(s)?://([\\w-]+.)+[\\w-]+(/[\\w- ./?%&=])?$");
            if (!regex.IsMatch(tbxUrl.Text))
            {
                // 형식이 안맞으면 윈도우 애니메이션
                WrongFormAnimation();
                return;
            }

            try
            {
                IsProgress(true);
                url = tbxUrl.Text;

                // 비디오 및 오디오 포맷 목록 초기화
                VideoFormats.Clear();
                VideoFormats.Add(new YoutubeFileFormat(Model.Type.Header, "--Video"));
                cbVideoFormat.SelectedIndex = 0;

                AudioFormats.Clear();
                AudioFormats.Add(new YoutubeFileFormat(Model.Type.Header, "--Audio"));
                cbAudioFormat.SelectedIndex = 0;

                // youtube-dl.exe 프로세스 실행
                // 1. 영상의 썸네일, 제목을 받아옴(youtube-dl --get-thumbnail --get-title <url>)
                // 2. 영상의 포맷 목록을 받아옴(youtube-dl -F <url>)
                var result = ProcessAsyncHelper.RunProcessAsync(youtubeDl, "-R 10 --get-thumbnail --get-title " + tbxUrl.Text, 8000);
                var formats = ProcessAsyncHelper.RunProcessAsync(youtubeDl, "-R 10 -F " + tbxUrl.Text, 8000);
                var processTask = Task.WhenAll(result, formats);

                // Timeout이 발생하기 전 태스크가 processTask & 두 프로세스 리턴값이 not null에 해당
                if (await Task.WhenAny(Task.Delay(TIEMOUT), processTask) == processTask
                        && result.Result.Output != null
                        && formats.Result.Output != null)
                {
                    StringReader info = new StringReader(result.Result.Output);
                    StringReader format = new StringReader(formats.Result.Output);

                    var noData = @"No Received Information";
                    tblTitle.Text = info.ReadLine() ?? noData;
                    if (tblTitle.Text.Equals(noData))
                    {
                        dpForm.IsEnabled = false;
                        imgThumbnail.Source = LoadDefaultImage();
                        return;
                    }

                    // 영상 썸네일 다운로드 작업
                    try
                    {
                        var task = GetThumbnail(info.ReadLine()).ContinueWith((t) =>
                        { imgThumbnail.Dispatcher.Invoke(() => imgThumbnail.Source = t.Result); });

                        // 이미지 다운로드 작업이 지연시간동안 완료되지 않으면 기본 이미지 로드
                        if (await Task.WhenAny(Task.Delay(8000), task) != task)
                            throw task.Exception;
                    }
                    catch { imgThumbnail.Source = LoadDefaultImage(); }

                    while (true)
                    {
                        string line = format.ReadLine();

                        if (string.IsNullOrEmpty(line))
                            break;

                        if (line.Contains("video"))
                            VideoFormats.Add(new YoutubeFileFormat(Model.Type.VideoFormat, line));
                        else if (line.Contains("audio only"))
                            AudioFormats.Add(new YoutubeFileFormat(Model.Type.AudioFormat, line));
                    }

                    cbAudioFormat.IsHitTestVisible = AudioFormats.Count != 1;
                    cbVideoFormat.IsHitTestVisible = VideoFormats.Count != 1;

                    // 읽어온 영상 정보의 url을 저장
                    OnPropertyChanged("VideoFormats");
                    OnPropertyChanged("AudioFormats");
                }
            }
            catch (Exception ex) { MessageBox.Show("처리 중 오류가 발생했습니다\n\n\n" + ex.Message + "\n" + ex.StackTrace); }
            finally { IsProgress(false); }
        }

        /// 영상 다운로드 Button Click
        private void DownloadVideo(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("먼저 URL을 입력해 정보를 불러와주세요.", "ERROR");
                return;
            }

            // Youtube-dl Argument
            StringBuilder sb = new StringBuilder();

            // 오디오 파일받기
            if (rdAudio.IsChecked.Value)
            {
                if (AudioFormats.Count == 1)
                {
                    MessageBox.Show("해당 영상의 오디오 포맷이 존재하지 않습니다.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // 오디오 추출 arg
                sb.Append("-f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 ");
            }
            // 비디오 파일받기
            else if (rdVideo.IsChecked.Value)
            {
                if ((cbVideoFormat.SelectedIndex == 0 && VideoFormats.Count != 1)
                    || (cbAudioFormat.SelectedIndex == 0 && AudioFormats.Count != 1))
                {
                    MessageBox.Show("비디오 또는 오디오 포맷이 선택되지 않았습니다.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var videoFormat = (YoutubeFileFormat)cbVideoFormat.SelectedItem;
                var audioFormat = (YoutubeFileFormat)cbAudioFormat.SelectedItem;

                var strFormat = (VideoFormats.Count == 1 ? string.Empty : videoFormat.FormatNumber)
                                + (AudioFormats.Count == 1 ? string.Empty : "+" + audioFormat.FormatNumber);
                strFormat = strFormat.TrimStart('+');

                sb.Append(string.IsNullOrEmpty(strFormat) ? "" : "-f " + strFormat)
                .Append(" --merge-output-format mkv ");
            }
            else if (rdPrefer.IsChecked.Value)
            {
                sb.Append("--prefer-free-formats ");
            }

            sb.Append("-o " + DownloadPath + "/%(title)s.%(ext)s "); // 다운로드 경로
            sb.Append("--ffmpeg-location \"" + ffmpeg + "\" ");
            sb.Append("-R 10 ");
            sb.Append(url);

            ProgressDialog dialog = new ProgressDialog(sb.ToString()) { Owner = this };
            IsEnabled = false;
            // Modal Window
            bool? result = dialog.ShowDialog();

            if (result == true)
                dialog.Show();
            else
                IsEnabled = true;
        }

        /// 블로그, Github 링크
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            Process.Start(hyperlink.NavigateUri.AbsoluteUri);
        }

        /// TextBox 엔터 이벤트
        private void tbxUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                GetUrlInfoClick(null, null);
        }

        #endregion Event

        #region Thumbnail, 기본 이미지 로딩

        public static Task<BitmapSource> GetThumbnail(string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var tcs = new TaskCompletionSource<BitmapSource>();

            BitmapImage image = new BitmapImage(uri);
            if (image.IsDownloading)
            {
                image.DownloadCompleted += (sender, e) => tcs.SetResult(image);
                image.DownloadFailed += (sender, e) => tcs.SetException(e.ErrorException);
            }
            else
                tcs.SetResult(image);

            return tcs.Task;
        }

        public static BitmapSource LoadDefaultImage()
        {
            BitmapSource bs;
            var ip = Properties.Resources.frame_landscape.GetHbitmap();
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        #endregion Thumbnail 만들기

        #region Progress 원 표시 및 컨트롤 UI 활성

        private void IsProgress(bool isProgress)
        {
            IsEnabled = !isProgress;
            mtProgressRing.Visibility = isProgress ? Visibility.Visible : Visibility.Collapsed;
            dpForm.IsEnabled = !isProgress;
        }

        #endregion Progress 원 표시 및 컨트롤 UI 활성

        #region Window Animation

        private void WrongFormAnimation()
        {
            DoubleAnimation animation = new DoubleAnimation(this.Left, this.Left + 15, TimeSpan.FromMilliseconds(50), FillBehavior.Stop)
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
            };

            BeginAnimation(LeftProperty, animation);
        }

        #endregion Window Animation

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public void OnPropertyChanged(string name)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion PropertyChanged
    }
}