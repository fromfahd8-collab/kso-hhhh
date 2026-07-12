using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KSO.Modules
{
    public class DownloadManager : INotifyPropertyChanged
    {
        private readonly ObservableCollection<DownloadItem> _downloads;
        private readonly DownloadHistory _history;
        private readonly ConfigData _config;
        private HttpClient _httpClient;
        private string _statusMessage = "جاهز";

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public ObservableCollection<DownloadItem> Downloads => _downloads;
        public event Action<int, string> RowAdded;

        public DownloadManager(ObservableCollection<DownloadItem> downloads, DownloadHistory history, ConfigData config)
        {
            _downloads = downloads;
            _history = history;
            _config = config;
            UpdateHttpClient(); // نعمل الكلاينت اول مرة
        }

        private void UpdateHttpClient()
        {
            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_config.Proxy))
            {
                handler.Proxy = new WebProxy(_config.Proxy);
                handler.UseProxy = true;
            }
            _httpClient = new HttpClient(handler, true);
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        public void AddDownload(string url, string quality, int threads)
        {
            if (_config.NoDuplicate && _downloads.Any(d => d.Url == url))
            {
                StatusMessage = "التحميل موجود بالفعل";
                return;
            }

            var item = new DownloadItem(url, threads, _httpClient, _config)
            {
                Quality = quality,
                DownloadFolder = _config.DownloadPath
            };
            
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DownloadItem.Status))
                    StatusMessage = $"{item.FileName}: {item.Status}";
            };

            _downloads.Add(item);
            _history.Add(item);
            StatusMessage = $"بدء تحميل: {item.FileName}";
            RowAdded?.Invoke(_downloads.Count - 1, item.FileName);
            _ = item.StartDownloadAsync();
        }

        public void StopAll()
        {
            foreach (var item in _downloads)
                item.Cancel();
            StatusMessage = "تم إيقاف جميع التحميلات";
        }

        public void PauseAll()
        {
            foreach (var item in _downloads.Where(i => i.Status == "Downloading"))
                item.Pause();
        }

        public void ResumeAll()
        {
            foreach (var item in _downloads.Where(i => i.Status == "Paused"))
                item.Resume();
        }

        public static int CalculateOptimalThreads(double speedMbps, int totalFiles)
        {
            int totalThreads = speedMbps switch
            {
                <= 10 => 16,
                <= 30 => 32,
                <= 100 => 64,
                <= 500 => 100,
                _ => 200 // يوصل 200 خيط
            };

            totalThreads = Math.Min(totalThreads, 1000000);
            int perFile = totalThreads / Math.Max(1, totalFiles);
            return Math.Max(perFile, 2);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}