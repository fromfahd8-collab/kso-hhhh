using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KSO.Modules
{
    public class HistoryItem
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public int Threads { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Md5 { get; set; }
    }

    public class DownloadHistory
    {
        private readonly string _historyPath;

        public DownloadHistory(string historyPath = null)
        {
            _historyPath = historyPath ?? Path.Combine(App.AppDataFolder, "downloads_history.json");
        }

        public List<HistoryItem> Load()
        {
            if (File.Exists(_historyPath))
            {
                try { return JsonConvert.DeserializeObject<List<HistoryItem>>(File.ReadAllText(_historyPath)) ?? new List<HistoryItem>(); }
                catch { return new List<HistoryItem>(); }
            }
            return new List<HistoryItem>();
        }

        public void Add(DownloadItem item)
        {
            var history = Load();
            // منع التكرار
            history.RemoveAll(h => h.Url == item.Url);
            
            history.Add(new HistoryItem
            {
                Url = item.Url,
                FileName = item.FileName,
                TotalBytes = item.TotalBytes,
                DownloadedBytes = item.DownloadedBytes,
                Threads = item.Threads,
                Status = item.Status,
                Date = DateTime.Now,
                Md5 = item.Md5
            });
            
            // نحتفظ باخر 1000 تحميل بس
            if(history.Count > 1000) history = history.Skip(history.Count - 1000).ToList();
            
            Save(history);
        }

        public void Save(List<DownloadItem> items)
        {
            var history = items.Select(item => new HistoryItem
            {
                Url = item.Url,
                FileName = item.FileName,
                TotalBytes = item.TotalBytes,
                DownloadedBytes = item.DownloadedBytes,
                Threads = item.Threads,
                Status = item.Status,
                Date = DateTime.Now,
                Md5 = item.Md5
            }).ToList();
            Save(history);
        }

        public void Save(IEnumerable<HistoryItem> items)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_historyPath));
            File.WriteAllText(_historyPath, JsonConvert.SerializeObject(items, Formatting.Indented));
        }

        public void ExportToCsv(string path)
        {
            var items = Load();
            using var writer = new StreamWriter(path);
            writer.WriteLine("URL,FileName,Size,Status,Date,MD5");
            foreach (var item in items)
            {
                writer.WriteLine($"\"{item.Url}\",\"{item.FileName}\",{item.TotalBytes},\"{item.Status}\",{item.Date:yyyy-MM-dd HH:mm:ss},\"{item.Md5}\"");
            }
        }
    }
}