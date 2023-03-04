using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace Th3Essentials.InfluxDB
{
    public class InfluxDbClient
    {
        private readonly ICoreServerAPI _api;

        private readonly HttpClient _httpClient;

        private readonly string _writeEndpoint;

        public InfluxDbClient(string influxDbUrl, string influxDbToken, string influxDbOrg, string influxDbBucket,
            ICoreServerAPI api)
        {
            _api = api;
            _writeEndpoint = $"write?org={influxDbOrg}&bucket={influxDbBucket}";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{influxDbUrl}/api/v2/")
            };

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {influxDbToken}");
        }

        internal void Dispose()
        {
            _httpClient.Dispose();
        }

        internal void WritePoint(PointData point, WritePrecision? precision)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (precision != null)
                    {
                        var httpResponseMessage = await _httpClient.PostAsync(
                            $"{_writeEndpoint}&precision={precision.ToString().ToLower()}",
                            new StringContent(point.ToLineProtocol(), Encoding.UTF8, "application/json"));
                        if (!httpResponseMessage.IsSuccessStatusCode)
                        {
                            var response = await httpResponseMessage.Content.ReadAsStringAsync();
                            _api.Logger.Warning($"[InfluxDB] {(int)httpResponseMessage.StatusCode} : {response}");
                        }
                    }
                    else
                    {
                        var httpResponseMessage = await _httpClient.PostAsync(_writeEndpoint,
                            new StringContent(point.ToLineProtocol(), Encoding.UTF8, "application/json"));
                        if (!httpResponseMessage.IsSuccessStatusCode)
                        {
                            var response = await httpResponseMessage.Content.ReadAsStringAsync();
                            _api.Logger.Warning($"[InfluxDB] {(int)httpResponseMessage.StatusCode} : {response}");
                        }
                    }
                }
                catch (Exception e)
                {
                    _api.Logger.Warning($"[InfluxDB] {e}");
                }
            });
        }

        internal void WritePoints(List<PointData> points, WritePrecision? precision)
        {
            Task.Run(async () =>
            {
                try
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < points.Count; i++)
                    {
                        var point = points[i];
                        sb.Append(point.ToLineProtocol());
                        if (i <= points.Count - 1)
                        {
                            sb.Append("\n");
                        }
                    }

                    if (precision != null)
                    {
                        var httpResponseMessage = await _httpClient.PostAsync(
                            $"{_writeEndpoint}&precision={precision.ToString().ToLower()}",
                            new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                        if (!httpResponseMessage.IsSuccessStatusCode)
                        {
                            var response = await httpResponseMessage.Content.ReadAsStringAsync();
                            _api.Logger.Warning($"[InfluxDB] {(int)httpResponseMessage.StatusCode} : {response}");
                        }
                    }
                    else
                    {
                        var httpResponseMessage = await _httpClient.PostAsync(_writeEndpoint,
                            new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                        if (!httpResponseMessage.IsSuccessStatusCode)
                        {
                            var response = await httpResponseMessage.Content.ReadAsStringAsync();
                            _api.Logger.Warning($"[InfluxDB] {(int)httpResponseMessage.StatusCode} : {response}");
                        }
                    }
                }
                catch (Exception e)
                {
                    _api.Logger.Warning($"[InfluxDB] {e}");
                }
            });
        }

        public bool HasConnection()
        {
            try
            {
                var httpResponseMessage = _httpClient.GetAsync("orgs").GetAwaiter().GetResult();
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    _api.Logger.Debug("Influxdb connected");
                    return true;
                }

                _api.Logger.Error($"Error connecting to {_httpClient.BaseAddress}, shutting down influxdb service. {httpResponseMessage.StatusCode}");
                return false;
            }
            catch (Exception e)
            {
                _api.Logger.Error(
                    $"Could not connect to {_httpClient.BaseAddress}, shutting down influxdb service. {e.Message}");
                return false;
            }
        }
    }
}