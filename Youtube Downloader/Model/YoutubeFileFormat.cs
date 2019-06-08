using System;

namespace Youtube_Downloader.Model
{
    public class YoutubeFileFormat
    {
        // 포맷 목록 내용
        public string Content { get; private set; }

        // 포맷 번호, youtube-dl argument용
        public string FormatNumber { get; private set; }

        /// <summary>
        /// 포맷 아이템 생성자
        /// </summary>
        /// <param name="formatType">포맷 타입(Video, Audio)</param>
        /// <param name="line">프로세스 출력 라인</param>
        public YoutubeFileFormat(Type formatType, string line)
        {
            // 구분자, 공백을 기준으로 문자열 Split
            char[] chars = { ' ' };
            string[] splitted = line.Split(chars, StringSplitOptions.RemoveEmptyEntries);

            FormatNumber = splitted[0];

            /// 137    mp4    1920x1080  1080p 5076k , avc1.640028, 24fps, video only, 113.44MiB
            if (formatType == Type.VideoFormat)
                Content = " " + splitted[2] + " " + splitted[3] + " " + splitted[4] + " " + splitted[splitted.Length - 1];
            /// 251    webm    audio only DASH audio  158k , opus @160k, 5.34MiB
            else if (formatType == Type.AudioFormat)
                Content = " " + splitted[6] + " " + splitted[splitted.Length - 1];
            /// 목록 최상단 ComboBox 헤더용
            else if (formatType == Type.Header)
                Content = line;
        }

        // ComboBox Binding 표시용
        public override string ToString() => Content;
    }

    public enum Type
    {
        VideoFormat,
        AudioFormat,
        Header
    }
}