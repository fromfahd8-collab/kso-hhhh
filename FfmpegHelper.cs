using System;
using System.Diagnostics;
using System.IO;

namespace KSO.Modules
{
    public static class FfmpegHelper
    {
        // المسار الجديد: AppData\KSO\ffmpeg.exe
        private static readonly string FfmpegPath = Path.Combine(App.AppDataFolder, "ffmpeg.exe");

        public static bool CheckFfmpeg()
        {
            EnsureFfmpegExists(); // اتأكد انه موجود الاول
            if (!File.Exists(FfmpegPath)) return false;
            try
            {
                using var p = Process.Start(new ProcessStartInfo(FfmpegPath, "-version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p.WaitForExit(1000);
                return true; // لو فتح خلاص
            }
            catch { return false; }
        }

        private static void EnsureFfmpegExists()
        {
            if (File.Exists(FfmpegPath)) return;
            
            // نفكه من EmbeddedResource
            var assembly = typeof(FfmpegHelper).Assembly;
            using var stream = assembly.GetManifestResourceStream("KSO.Resources.ffmpeg.exe");
            if (stream == null) return;

            Directory.CreateDirectory(App.AppDataFolder);
            using var fileStream = new FileStream(FfmpegPath, FileMode.Create);
            stream.CopyTo(fileStream);
        }

        public static bool CompressVideo(string inputPath, string outputPath, int crf = 28, string preset = "fast")
        {
            EnsureFfmpegExists();
            if (!File.Exists(FfmpegPath)) return false;
            // H.264 افضل للتوافق بدل x265
            var args = $"-i \"{inputPath}\" -vcodec libx264 -crf {crf} -preset {preset} -acodec aac -b:a 128k -threads 0 -y \"{outputPath}\"";
            return RunFfmpeg(args);
        }

        public static bool ConvertToMp3(string inputPath, string outputPath)
        {
            EnsureFfmpegExists();
            if (!File.Exists(FfmpegPath)) return false;
            return RunFfmpeg($"-i \"{inputPath}\" -vn -b:a 320k \"{outputPath}\"");
        }

        public static bool ConvertTo720p(string inputPath, string outputPath)
        {
            EnsureFfmpegExists();
            if (!File.Exists(FfmpegPath)) return false;
            return RunFfmpeg($"-i \"{inputPath}\" -vf scale=1280:720 -c:a copy -y \"{outputPath}\"");
        }

        public static bool TrimFirst30Sec(string inputPath, string outputPath)
        {
            EnsureFfmpegExists();
            if (!File.Exists(FfmpegPath)) return false;
            return RunFfmpeg($"-i \"{inputPath}\" -ss 0 -t 30 -c copy -y \"{outputPath}\"");
        }

        public static bool MergeVideoAudio(string videoPath, string audioPath, string outputPath)
        {
            EnsureFfmpegExists();
            if (!File.Exists(FfmpegPath)) return false;
            return RunFfmpeg($"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -map 0:v -map 1:a -shortest -y \"{outputPath}\"");
        }

        private static bool RunFfmpeg(string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = FfmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch { return false; }
        }
    }
}