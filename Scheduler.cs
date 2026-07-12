using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace KSO.Modules
{
    public class Scheduler
    {
        private readonly DownloadManager _manager;
        private readonly ConfigManager _configManager; // ضفناه
        private Timer _timer;
        private bool _isRunning;
        private DateTime _lastRun = DateTime.MinValue; // عشان ميشغلش مرتين

        public Scheduler(DownloadManager manager)
        {
            _manager = manager;
            // نقرا من AppData
            _configManager = new ConfigManager(Path.Combine(App.AppDataFolder, "config.json"));
        }

        public void Start()
        {
            _isRunning = true;
            _timer = new Timer(CheckSchedule, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void CheckSchedule(object state)
        {
            if (!_isRunning) return;

            _configManager.Load(); // اعمل Reload كل دقيقة
            var config = _configManager.Config;

            if (config.ScheduleTime.HasValue)
            {
                var now = DateTime.Now;
                var scheduledTime = config.ScheduleTime.Value;

                // نتأكد انه نفس اليوم والوقت ومتشغلش مرتين
                if (now.Date == scheduledTime.Date 
                    && now.Hour == scheduledTime.Hour 
                    && now.Minute == scheduledTime.Minute
                    && _lastRun.Date != now.Date) // اتأكد انه مشتغلش النهاردة
                {
                    _lastRun = now;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _manager.StatusMessage = "بدء الجدولة: تشغيل التحميلات";
                        _manager.StartAll(); // ابدأ كل التحميلات المعلقة
                    });

                    // لو عايز يقفل الجهاز بعد ما يخلص
                    if(config.ShutdownOnComplete)
                    {
                        _manager.PropertyChanged += (s,e) => {
                            if(e.PropertyName == nameof(DownloadManager.StatusMessage) 
                               && _manager.StatusMessage.Contains("Completed"))
                            {
                                Task.Delay(5000).ContinueWith(_ => Process.Start("shutdown", "/s /t 60"));
                            }
                        };
                    }
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
        }
    }
}