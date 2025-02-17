using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System.Diagnostics;
using System.Text;

namespace DeleteContactService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetricsController : ControllerBase
    {
        private static readonly Histogram RequestDuration = Metrics.CreateHistogram(
             "delete_contact_request_duration_milliseconds",
             "Histograma de latência do endpoint de delete",
             new HistogramConfiguration
             {
                 LabelNames = new[] { "path", "method" },
                 Buckets = Histogram.LinearBuckets(start: 10, width: 100, count: 10)
             }
        );

        private static readonly Gauge CpuUsageGauge = Metrics.CreateGauge(
            "cpu_usage_percentage_delete",
            "Uso da CPU em tempo real em porcentagem"
        );

        private static readonly Gauge MemoryUsageGauge = Metrics.CreateGauge(
            "memory_usage_bytes_delete",
            "Uso de memória em tempo real em bytes"
        );

        private static readonly Stopwatch Stopwatch = new();
        private readonly IHttpClientFactory _httpClientFactory;

        public MetricsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private async Task<double> MeasureDeleteLatency()
        {
            var client = _httpClientFactory.CreateClient();
            var stopwatch = Stopwatch.StartNew();

            var fixedId = "00000000-0000-0000-0000-000000000001";
            var response = await client.DeleteAsync($"http://delete-contact-service/DeleteContact/Delete/{fixedId}");


            stopwatch.Stop();
            var latency = stopwatch.Elapsed.TotalSeconds;

            RequestDuration.WithLabels("/DeleteContact/Delete/{id}", "DELETE").Observe(latency);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request to /DeleteContact/Delete/{fixedId} failed with status code {response.StatusCode}");
            }

            return latency * 1000;
        }

        private float GetCpuUsage()
        {
            var cpuStat1 = System.IO.File.ReadAllText("/proc/stat").Split("\n")[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            System.Threading.Thread.Sleep(100);
            var cpuStat2 = System.IO.File.ReadAllText("/proc/stat").Split("\n")[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            float idle1 = float.Parse(cpuStat1[4]);
            float total1 = cpuStat1.Skip(1).Select(float.Parse).Sum();

            float idle2 = float.Parse(cpuStat2[4]);
            float total2 = cpuStat2.Skip(1).Select(float.Parse).Sum();

            return (1 - ((idle2 - idle1) / (total2 - total1))) * 100;
        }

        private long GetMemoryUsage()
        {
            var memoryUsage = System.IO.File.ReadAllText("/sys/fs/cgroup/memory/memory.usage_in_bytes");
            return long.Parse(memoryUsage);
        }


        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            var metricsBuffer = new StringBuilder();

            await using (var stream = new MemoryStream())
            {
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    metricsBuffer.Append(await reader.ReadToEndAsync());
                }
            }

            return Content(metricsBuffer.ToString(), "text/plain");
        }

        [HttpGet("latency/DeleteContact")]
        public async Task<IActionResult> GetDeleteContactLatency()
        {
            try
            {
                var latencyInMs = await MeasureDeleteLatency();
                return Ok($"DeleteContact Latency: {latencyInMs} ms");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao medir a latência: {ex.Message}");
            }
        }

        [HttpGet("CPU")]
        public IActionResult GetCpuUsageMetric()
        {
            var cpuUsage = GetCpuUsage();
            CpuUsageGauge.Set(cpuUsage);

            return Ok($"Uso da CPU: {cpuUsage}%");
        }

        [HttpGet("Memory")]
        public IActionResult GetMemoryUsageMetric()
        {
            var memoryUsage = GetMemoryUsage();
            MemoryUsageGauge.Set(memoryUsage);

            return Ok($"Uso da memória: {memoryUsage / (1024 * 1024)} MB");
        }
    }
}
