using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace InfluxDB
{
    public class InfluxDBClient
    {
        public bool Disposed;

        private readonly ICoreServerAPI api;

        private readonly HttpClient HttpClient;

        private readonly string WriteEndpoint;

        public InfluxDBClient(string inlfuxDBURL, string inlfuxDBToken, string inlfuxDBOrg, string inlfuxDBBucket, ICoreServerAPI api)
        {
            this.api = api;
            WriteEndpoint = $"write?org={inlfuxDBOrg}&bucket={inlfuxDBBucket}&precision=ms";
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri($"{inlfuxDBURL}/api/v2/")
            };

            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Token {inlfuxDBToken}");
        }

        internal void Dispose()
        {
            HttpClient.Dispose();
        }

        internal void WritePoint(PointData point)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    await HttpClient.PostAsync(WriteEndpoint, new StringContent(point.ToLineProtocol(), Encoding.UTF8, "application/json"));
                });
            }
            catch (Exception e)
            {
                api.Logger.Warning($"[InfluxDB] {e.Message}");
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

                    await HttpClient.PostAsync(WriteEndpoint, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
                });
            }
            catch (Exception e)
            {
                api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }
    }
}