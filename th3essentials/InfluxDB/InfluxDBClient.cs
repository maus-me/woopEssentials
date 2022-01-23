using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace Th3Essentials.InfluxDB
{
    public class InfluxDBClient
    {
        public bool Disposed;

        private readonly ICoreServerAPI _api;

        private readonly HttpClient _httpClient;

        private readonly string _writeEndpoint;

        public InfluxDBClient(string inlfuxDBURL, string inlfuxDBToken, string inlfuxDBOrg, string inlfuxDBBucket, ICoreServerAPI api)
        {
            _api = api;
            _writeEndpoint = $"write?org={inlfuxDBOrg}&bucket={inlfuxDBBucket}&precision=s";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{inlfuxDBURL}/api/v2/")
            };

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {inlfuxDBToken}");
        }

        internal void Dispose()
        {
            _httpClient.Dispose();
        }

        internal void WritePoint(PointData point)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    await _httpClient.PostAsync(_writeEndpoint, new StringContent(point.ToLineProtocol(), Encoding.UTF8, "application/json"));
                });
            }
            catch (Exception e)
            {
                _api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }

        internal void WritePoints(List<PointData> points)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < points.Count; i++)
                    {
                        PointData point = points[i];
                        sb.Append(point.ToLineProtocol());
                        if (i <= points.Count - 1)
                        {
                            sb.Append("\n");
                        }
                    }

                    await _httpClient.PostAsync(_writeEndpoint, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                });
            }
            catch (Exception e)
            {
                _api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }
    }
}