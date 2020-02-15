using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HealthTracking
{
    class Program
    {
        private const string HealthCheckUrl = "http://localhost:5000/health-check";
        private const string InfluxdbWriteUrl = "http://localhost:8086/write?db=telegraf";
        static async Task Main(string[] args)
        {
            var result = await GetHealthStatus();

            await PostToInfluxDb(result.Data);

            Console.WriteLine(JsonConvert.SerializeObject(result));
        }

        private class HealthCheckResult
        {
            public string Status { get; set; }
            public IEnumerable<HealthTrackingResult> Data { get; set; }
        }

        private class HealthTrackingResult
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public int Code { get; set; }
        }

        private static async Task<HealthCheckResult> GetHealthStatus()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(HealthCheckUrl);
                var json = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<HealthCheckResult>(json);
            }
        }

        private static async Task PostToInfluxDb(IEnumerable<HealthTrackingResult> items)
        {
            foreach (var status in items)
            {
                var body = $"health,host=gpf1,service={status.Key} value={status.Code}";

                using (var content = new StringContent(body))
                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(InfluxdbWriteUrl, content);
                }
            }
        }

    }
}
