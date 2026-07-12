using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using System.Timers;
using KSO.Modules;
using Microsoft.WebView2.Core;

namespace KSO
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DownloadManager _downloadManager;
        private readonly ConfigManager _configManager;
        private readonly LocalizationManager _localization;
        private readonly DownloadHistory _history;
        private readonly Scheduler _scheduler;
        private readonly System.Timers.Timer _resourceTimer;
        private readonly HttpClient _httpClient = new(); // واحد مشترك لكل التحميلات
        private bool _isHidden = false;

        public ObservableCollection<DownloadItem> Downloads { get; } = new();
        public ICollectionView DownloadsView { get; private set; }

        public ConfigData Config => _configManager.Config;
        public LocalizationData Lang => _localization.CurrentLang;

        private string _statusBarText = "جاهز";
        public string StatusBarText
        {
            get => _statusBarText;
            set { _statusBarText = value; OnPropertyChanged(nameof(StatusBarText)); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _configManager = new ConfigManager(Path.Combine(App.AppDataFolder, "config.json"));
            _localization = new LocalizationManager(Path.Combine(App.AppDataFolder, "lang.json"));
            _history = new DownloadHistory(Path.Combine(App.AppDataFolder, "downloads_history.json"));
            _downloadManager = new DownloadManager(Downloads, _history, Config, _httpClient);
            _scheduler = new Scheduler(_downloadManager);

            // مهم: لازم نعمل new للـ StudyTab ونمرر this
            studyTab = new StudyTab(this);

            DownloadsView = CollectionViewSource.GetDefaultView(Downloads);
            DownloadsView.Filter = FilterDownloads;

            _downloadManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DownloadManager.StatusMessage))
                    Dispatcher.Invoke(() => StatusBarText = _downloadManager.StatusMessage);
            };

            _ = SmartSpeedTest();
            RestoreHistory();
            CheckPassword();
            _scheduler.Start();
            UpdateLanguage();
            LoadSettingsFromConfig();
            ApplyTheme();

            _ = browser.EnsureCoreWebView2Async(null);
            browser.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if(e.IsSuccess) browser.CoreWebView2.Navigating += Browser_Navigating;
            };

            _resourceTimer = new System.Timers.Timer(2000);
            _resourceTimer.Elapsed += UpdateResourceUsage;
            _resourceTimer.Start();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.H && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ToggleHiddenMode();
                if (e.Key == Key.K && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) { this.Show(); this.WindowState = WindowState.Normal; }
                if (e.Key == Key.Q && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) this.Close();
                if (e.Key == Key.D && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ClipboardDownload();
            };
        }

        private void ApplyTheme() { }

        private void RestoreHistory()
        {
            foreach(var item in _history.Load())
            {
                var dl = new DownloadItem(item.Url, item.Threads, _httpClient, Config);
                dl.FileName = item.FileName;
                dl.Status = item.Status;
                Downloads.Add(dl);
            }
        }

        private void CheckPassword()
        {
            if(!string.IsNullOrEmpty(Config.Password) && !Config.WelcomeShown)
            {
                var dlg = new PasswordWindow();
                if(dlg.ShowDialog() != true) this.Close();
                Config.WelcomeShown = true;
                _configManager.Save();
            }
        }

        private void LoadSettingsFromConfig()
        {
            spinThreads.Value = Config.MaxThreads;
            timeSchedule.Value = Config.ScheduleTime;
            spinSpeedLimit.Value = Config.SpeedLimitKB;
            txtProxy.Text = Config.Proxy;
            txtPassword.Password = Config.Password;
            chkSmartMode.IsChecked = Config.SmartMode;
            chkAutoCompress.IsChecked = Config.AutoCompress;
            chkDeleteOriginal.IsChecked = Config.DeleteOriginal;
            chkNoDuplicate.IsChecked = Config.NoDuplicate;
            chkShutdown.IsChecked = Config.ShutdownOnComplete;
            btnPath.Content = Config.DownloadPath;
            
            cmbCompressMode.SelectedIndex = cmbCompressMode.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => i.Content.ToString() == Config.CompressMode);
            if(cmbCompressMode.SelectedIndex == -1) cmbCompressMode.SelectedIndex = 1;

            cmbQuality.SelectedIndex = cmbQuality.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => i.Content.ToString() == Config.Quality);
            if(cmbQuality.SelectedIndex == -1) cmbQuality.SelectedIndex = 2;
        }

        private async Task SmartSpeedTest()
        {
            if(!Config.SmartMode) return;
            try
            {
                StatusBarText = "جاري اختبار السرعة...";
                var speed = await SpeedTest.MeasureDownloadSpeedAsync();
                int threads = DownloadManager.CalculateOptimalThreads(speed, 1);
                Config.MaxThreads = threads;
                Dispatcher.Invoke(() =>
                {
                    spinThreads.Value = threads;
                    StatusBarText = $"السرعة: {speed:F1} Mbps - خيوط: {threads}";
                });
                _configManager.Save();
            }
            catch { StatusBarText = "فشل اختبار السرعة. تم استخدام 32 خيط"; }
        }
        
        private void Browser_Navigating(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var url = e.Uri;
            if (url.Contains("youtube.com/watch") || url.Contains("youtu.be/") || url.Contains(".mp4") || url.Contains(".m3u8"))
            {
                if (MessageBox.Show($"{Lang.AutoCapture}\n{url}", "KSO", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    AddDownload(url);
            }
        }

        private bool FilterDownloads(object obj)
        {
            if (obj is not DownloadItem item) return false;
            string filter = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";
            if (filter == "Downloading" && !item.Status.Contains("Downloading")) return false;
            if (filter == "Finished" && item.Status != "Completed" && item.Status != "Compressed") return false;
            if (filter == "Error" && item.Status.Contains("Error")) return false;
            if (!string.IsNullOrEmpty(txtSearch.Text)) return item.FileName.ToLower().Contains(txtSearch.Text.ToLower());
            return true;
        }

        public void AddDownload(string url) // public عشان StudyTab يستخدمه
        {
            if(Config.NoDuplicate && Downloads.Any(d => d.Url == url)) return;
            string quality = (cmbQuality.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "1080p";
            int threads = DownloadManager.CalculateOptimalThreads(Config.MaxThreads, Downloads.Count(d => d.Status == "Downloading") + 1);
            _downloadManager.AddDownload(url, quality, threads);
        }

        // --- Events ---
        private void BtnAdd_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(txtUrl.Text)) { AddDownload(txtUrl.Text.Trim()); txtUrl.Clear(); } }
        private void TxtUrl_KeyDown(object sender, KeyEventArgs e) { if(e.Key == Key.Enter) BtnAdd_Click(null, null); }
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => DownloadsView.Refresh();
        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => DownloadsView.Refresh();
        private void BtnPath_Click(object sender, RoutedEventArgs e) { var dialog = new System.Windows.Forms.FolderBrowserDialog(); if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) { Config.DownloadPath = dialog.SelectedPath; _configManager.Save(); btnPath.Content = dialog.SelectedPath; } }
        private void BtnLang_Click(object sender, RoutedEventArgs e) { _localization.ToggleLanguage(); UpdateLanguage(); Config.Language = _localization.CurrentCode; _configManager.Save(); }
        private void UpdateLanguage() { Title = Lang.Title; }
        private void Window_Closing(object sender, CancelEventArgs e) { _downloadManager.StopAll(); _scheduler.Stop(); _resourceTimer?.Stop(); _httpClient.Dispose(); _configManager.Save(); _history.Save(Downloads.ToList()); }
        private void UpdateResourceUsage(object sender, ElapsedEventArgs e) { Dispatcher.Invoke(() => { try { using var proc = Process.GetCurrentProcess(); lblResources.Text = $"RAM: {proc.WorkingSet64 / 1024 / 1024} MB"; } catch { } }); }
        private void ToggleHiddenMode() { _isHidden = !_isHidden; this.Visibility = _isHidden ? Visibility.Hidden : Visibility.Visible; this.ShowInTaskbar = !_isHidden; }
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e) { Config.Password = txtPassword.Password; _configManager.Save(); }
        
        // متعدل: فتح AboutDialog
        private void BtnAbout_Click(object sender, RoutedEventArgs e) 
        { 
            AboutDialog.ShowDialogWindow(this);
        }

        private void BtnReDownload_Click(object sender, RoutedEventArgs e) { if(lvDownloads.SelectedItem is DownloadItem item) AddDownload(item.Url); }
        private void BtnExportCsv_Click(object sender, RoutedEventArgs e) { var dlg = new Microsoft.Win32.SaveFileDialog(){Filter="CSV|*.csv"}; if(dlg.ShowDialog()==true) _history.ExportToCsv(dlg.FileName); }
        private void BtnClearCache_Click(object sender, RoutedEventArgs e) { try{ Directory.Delete(Path.Combine(App.AppDataFolder,"Cache"),true);}catch{} }
        private void BtnClearTemp_Click(object sender, RoutedEventArgs e) { try{ Directory.Delete(Path.GetTempPath()+"KSO_Downloads",true);}catch{} }
        private void Window_DragEnter(object sender, DragEventArgs e) { if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy; }
        private void Window_Drop(object sender, DragEventArgs e) { if(e.Data.GetData(DataFormats.FileDrop) is string[] files) foreach(var f in files) if(File.Exists(f)) AddDownload(f); }
        private void ClipboardDownload() { if(Clipboard.ContainsText()) AddDownload(Clipboard.GetText()); }

        // --- Config Events ---
        private void ChkSmartMode_Checked(object sender, RoutedEventArgs e) { Config.SmartMode = true; _configManager.Save(); }
        private void ChkSmartMode_Unchecked(object sender, RoutedEventArgs e) { Config.SmartMode = false; _configManager.Save(); }
        private void ChkAutoCompress_Checked(object sender, RoutedEventArgs e) { Config.AutoCompress = true; _configManager.Save(); }
        private void ChkAutoCompress_Unchecked(object sender, RoutedEventArgs e) { Config.AutoCompress = false; _configManager.Save(); }
        private void ChkDeleteOriginal_Checked(object sender, RoutedEventArgs e) { Config.DeleteOriginal = true; _configManager.Save(); }
        private void ChkDeleteOriginal_Unchecked(object sender, RoutedEventArgs e) { Config.DeleteOriginal = false; _configManager.Save(); }
        private void ChkNoDuplicate_Checked(object sender, RoutedEventArgs e) { Config.NoDuplicate = true; _configManager.Save(); }
        private void ChkNoDuplicate_Unchecked(object sender, RoutedEventArgs e) { Config.NoDuplicate = false; _configManager.Save(); }
        private void ChkShutdown_Checked(object sender, RoutedEventArgs e) { Config.ShutdownOnComplete = true; _configManager.Save(); }
        private void ChkShutdown_Unchecked(object sender, RoutedEventArgs e) { Config.ShutdownOnComplete = false; _configManager.Save(); }
        private void ChkBrowser_Checked(object sender, RoutedEventArgs e) => browser.Visibility = Visibility.Visible;
        private void ChkBrowser_Unchecked(object sender, RoutedEventArgs e) => browser.Visibility = Visibility.Collapsed;
        private void CmbCompressMode_SelectionChanged(object sender, SelectionChangedEventArgs e) { Config.CompressMode = (cmbCompressMode.SelectedItem as ComboBoxItem)?.Content.ToString(); _configManager.Save(); }
        private void CmbQuality_SelectionChanged(object sender, SelectionChangedEventArgs e) { Config.Quality = (cmbQuality.SelectedItem as ComboBoxItem)?.Content.ToString(); _configManager.Save(); }
        private void SpinThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) { if(spinThreads.Value.HasValue) { Config.MaxThreads = spinThreads.Value.Value; _configManager.Save(); } }
        private void SpinSpeedLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) { if(spinSpeedLimit.Value.HasValue) { Config.SpeedLimitKB = spinSpeedLimit.Value; _configManager.Save(); } }
        private void TxtProxy_TextChanged(object sender, TextChangedEventArgs e) { Config.Proxy = txtProxy.Text; _configManager.Save(); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}