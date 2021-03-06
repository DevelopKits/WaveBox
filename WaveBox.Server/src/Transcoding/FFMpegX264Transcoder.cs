using System;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;

namespace WaveBox.Transcoding {
    public class FFMpegX264Transcoder : VideoTranscoder {
        //private static Logger logger = LogManager.GetCurrentClassLogger();

        public override TranscodeType Type { get { return TranscodeType.X264; } }

        public override string Codec { get { return "libx264"; } }

        public override string Command { get { return "ffmpeg"; } }

        public override string OutputExtension { get { return "mp4"; } }

        public override string MimeType { get { return "video/x-flv"; } }

        public FFMpegX264Transcoder(IMediaItem item, uint quality, bool isDirect, uint? width, uint? height, bool maintainAspect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds) {
        }

        protected override string GenerateArguments(uint abitrate, uint vbitrate, uint width, uint height) {
            return "-i \"" + Item.FilePath() + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -ar 44100 -ac 2 -v 0 -f mpegts -refs 3 -vcodec " + Codec + " -preset superfast -threads 0 " + OutputPath;
        }
    }
}
