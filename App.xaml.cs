using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace KSO
{
    public partial class App : Application
    {
        public static string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KSO");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. نعمل فولدر KSO في AppData
            Directory.CreateDirectory(AppDataFolder);

            // 2. نفك الملفات اللي جوا الـ exe
            ExtractResource("KSO.Resources.ffmpeg.exe", Path.Combine(AppDataFolder, "ffmpeg.exe"));
            ExtractResource("KSO.Resources.config.json", Path.Combine(AppDataFolder, "config.json"));
            ExtractResource("KSO.Resources.lang.json", Path.Combine(AppDataFolder, "lang.json"));
        }

        private void ExtractResource(string resourceName, string outputPath)
        {
            // لو الملف موجود خلاص منفكوش تاني
            if (File.Exists(outputPath)) return;

            var assembly = Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show($"خطأ: لم يتم العثور على {resourceName}");
                    return;
                }
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }
}