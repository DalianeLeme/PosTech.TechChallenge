using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace UpdateContactService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetricsController : ControllerBase
    {
        private static readonly Histogram RequestDuration = Metrics.CreateHistogram(
             "update_contact_request_duration_milliseconds",
             "Histograma de latência do endpoint de update",
             new HistogramConfiguration
             {
                 LabelNames = new[] { "path", "method" },
                 Buckets = Histogram.LinearBuckets(start: 10, width: 100, count: 10)
             }
        );

        private static readonly Gauge CpuUsageGauge = Metrics.CreateGauge(
            "cpu_usage_percentage_update",
            "Uso da CPU em tempo real em porcentagem"
        );

        private static readonly Gauge MemoryUsageGauge = Metrics.CreateGauge(
            "memory_usage_bytes_update",
            "Uso de memória em tempo real em bytes"
        );

        private readonly IHttpClientFactory _httpClientFactory;

        public MetricsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private async Task<double> MeasureUpdateContactLatency()
        {
            var client = _httpClientFactory.CreateClient();
            var stopwatch = Stopwatch.StartNew();

            var payload = new StringContent(
                JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("b3d4f72b-77ed-4c54-95d4-94a7c1a95c2b"),
                    name = "Updated Name",
                    email = "updated@example.com",
                    ddd = 11,
                    phone = "987654321"
                }),
                Encoding.UTF8,
                "application/json");

            // Alterado para usar o DNS do Kubernetes
            var response = await client.PutAsync("http://update-contact-service/UpdateContact/Update", payload);

            stopwatch.Stop();
            var latency = stopwatch.Elapsed.TotalSeconds;

            RequestDuration.WithLabels("/UpdateContact/Update", "PUT").Observe(latency);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Request to /UpdateContact/Update failed with status code {response.StatusCode}: {errorContent}");
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

        [HttpGet("latency/UpdateContact")]
        public async Task<IActionResult> GetUpdateContactLatency()
        {
            try
            {
                var latencyInMs = await MeasureUpdateContactLatency();
                return Ok($"UpdateContact Latency: {latencyInMs} ms");
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
