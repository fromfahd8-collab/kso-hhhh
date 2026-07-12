using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpeedTest; // مكتبة SpeedTest.Net

namespace KSO.Modules
{
    public static class SpeedTest
    {
        public static async Task<double> MeasureDownloadSpeedAsync()
        {
            try
            {
                var speedTest = new SpeedTestClient();
                
                // 1. جيب السيرفرات واختار افضل واحد
                var servers = await speedTest.GetServersAsync();
                var bestServer = servers
                    .OrderBy(s => s.Latency)
                    .FirstOrDefault();

                if (bestServer == null) return 20.0;

                // 2. اختبار التحميل 10 ثواني
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var result = await speedTest.TestDownloadSpeedAsync(bestServer, cts.Token);

                // النتيجة بترجع Bytes/sec نحولها Mbps
                double mbps = (result * 8.0) / 1024.0 / 1024.0; 
                return Math.Round(mbps, 1);
            }
            catch (Exception)
            {
                // لو فشل نرجع 20mbps عشان يدي 32 خيط افتراضي
                return 20.0;
            }
        }
    }
}