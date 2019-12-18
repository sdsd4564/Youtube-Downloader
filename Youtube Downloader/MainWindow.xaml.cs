using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Youtube_Downloader.Model;

namespace Youtube_Downloader
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        // youtube-dl argument용
        private string url = string.Empty;

        // 영상 정보 받아오기 타임아웃 시간
        private readonly int TIEMOUT = 60000;

        private Task updateProcess;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            string youtubeDl = Path.Combine("./", "youtube-dl.exe");
            string ffmpeg = Path.Combine("./", "ffmpeg.exe");

            if (!File.Exists("./youtube-dl.exe") || !File.Exists("./ffmpeg.exe"))
            {
                MessageBox.Show("youtube-dl 또는 ffmpeg 파일이 존재하지 않습니다.", "NO FILES");
                Application.Current.Shutdown();
            }
            else
            {
                //var dialog = this.ShowProgressAsync("Update binary", "Please wait...");
                updateProcess = ProcessAsyncHelper.RunProcessAsync("./youtube-dl.exe", "--update", TIEMOUT);
                //updateProcess.ContinueWith((t) => dialog.Result.CloseAsync());
            }

            // Youtube 정보 받아오기 Button Click
            btnUrl.Click += async (sender, e) =>
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
                    var result = ProcessAsyncHelper.RunProcessAsync("./youtube-dl.exe", "--get-thumbnail --get-title " + tbxUrl.Text, TIEMOUT);
                    var formats = ProcessAsyncHelper.RunProcessAsync("./youtube-dl.exe", "-F " + tbxUrl.Text, TIEMOUT);
                    var processTask = Task.WhenAll(result, formats);

                    // Timeout이 발생하기 전 태스크가 processTask & 두 프로세스 리턴값이 not null에 해당
                    if (await Task.WhenAny(Task.Delay(TIEMOUT), processTask) == processTask
                            && result.Result.Output != null
                            && formats.Result.Output != null)
                    {
                        StringReader info = new StringReader(result.Result.Output);
                        StringReader format = new StringReader(formats.Result.Output);

                        tblTitle.Text = info.ReadLine();
                        imgThumbnail.Source = GetThumbnail(info.ReadLine());

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

                        //cbAudioFormat.IsEnabled = AudioFormats.Count != 1;
                        //cbVideoFormat.IsEnabled = VideoFormats.Count != 1;

                        cbAudioFormat.IsHitTestVisible = AudioFormats.Count != 1;
                        cbVideoFormat.IsHitTestVisible = VideoFormats.Count != 1;

                        // 읽어온 영상 정보의 url을 저장
                        url = tbxUrl.Text;
                        OnPropertyChanged("VideoFormats");
                        OnPropertyChanged("AudioFormats");
                    }
                }
                catch { MessageBox.Show("처리 중 오류가 발생했습니다"); }
                finally { IsProgress(false); }
            };

            // 영상 다운로드 Button Click
            btnDown.Click += (sender, e) =>
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

                    sb.Append("-f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 ") // 오디오 추출 arg
                    .Append("-o " + DownloadPath + "/%(title)s.%(ext)s ") // 다운로드 경로
                    .Append("--ffmpeg-location ./ffmpeg.exe ") // ffmpeg bin 경로 arg
                    .Append(url); // url
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

                    var strFormat = (VideoFormats.Count == 1 ? string.Empty : "-f " + videoFormat.FormatNumber) 
                                    + (AudioFormats.Count == 1 ? string.Empty : "+" + audioFormat.FormatNumber);

                    //sb.Append("-f " + videoFormat.FormatNumber + (AudioFormats.Count == 1 ? string.Empty : "+" + audioFormat.FormatNumber) )
                    sb.Append(strFormat)
                    .Append(" -o " + DownloadPath + "/%(title)s.%(ext)s") // 다운로드 경로
                    .Append(" --ffmpeg-location ./ffmpeg.exe") // ffmpeg bin 경로 arg
                    .Append(" --merge-output-format mkv ")
                    .Append(url);
                }

                ProgressDialog dialog = new ProgressDialog(sb.ToString());
                IsEnabled = false;
                // Modal Window
                bool? result = dialog.ShowDialog();

                if (result == true)
                    dialog.Show();
                else
                    IsEnabled = true;
            };
        }

        // 파일 다운로드 경로(C:/User/Download 고정)
        public string DownloadPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

        // ComboBox용 비디오 및 오디오 포맷목록(Binding)
        public ObservableCollection<YoutubeFileFormat> VideoFormats { get; set; } = new ObservableCollection<YoutubeFileFormat>();

        public ObservableCollection<YoutubeFileFormat> AudioFormats { get; set; } = new ObservableCollection<YoutubeFileFormat>();

        #region Thumbnail 만들기

        public BitmapImage GetThumbnail(string url)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
            image.EndInit();

            return image;
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