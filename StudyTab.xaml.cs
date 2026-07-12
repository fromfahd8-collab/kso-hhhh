using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using KSO.Modules;

namespace KSO
{
    public partial class StudyTab : UserControl
    {
        private readonly MainWindow _main; // هناخد المين ويندو عشان نجيب الكونفج والتحميل

        public StudyTab(MainWindow main)
        {
            InitializeComponent();
            _main = main;
        }

        // 1. البحث: هنفتح يوتيوب في المتصفح المدمج
        private void BtnSearchCourse_Click(object sender, RoutedEventArgs e)
        {
            string query = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) 
            {
                MessageBox.Show("أدخل اسم الكورس + اسم المادة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // نفتح تبويب المتصفح ونبحث
            var tabControl = (TabControl)this.Parent;
            tabControl.SelectedIndex = 1; // تبويب المتصفح

            string searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";
            _main.browser.CoreWebView2?.Navigate(searchUrl);
        }

        // 2. انشاء مجلد باسم المادة
        private void BtnCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderName = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(folderName))
            {
                MessageBox.Show("أدخل اسم المادة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string path = Path.Combine(_main.Config.DownloadPath, folderName);
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path); // افتح المجلد
            MessageBox.Show($"تم إنشاء المجلد:\n{path}", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 3. تحميل البلاي ليست: هنبعت اللينك للـ DownloadManager
        private void BtnDownloadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            string query = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("أدخل اسم المادة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // نعمل مجلد للكورس
            string folder = Path.Combine(_main.Config.DownloadPath, folderName);
            Directory.CreateDirectory(folder);

            // نعمل سيرش بلاي ليست يوتيوب
            string playlistUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}&sp=EgIQAw%3D%3D";
            
            // نضيفه للتحميل
            _main.AddDownload(playlistUrl);
            MessageBox.Show($"تمت إضافة البلاي ليست للتحميل في مجلد: {folderName}", "KSO", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}