using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace InfluxDB
{
    public class InfluxDBClient
    {
        private readonly string inlfuxDBURL;

        private readonly string inlfuxDBToken;

        public bool Disposed;
        private readonly ICoreServerAPI api;

        public InfluxDBClient(string inlfuxDBURL, string inlfuxDBToken, ICoreServerAPI api)
        {
            this.api = api;
            this.inlfuxDBURL = inlfuxDBURL;
            this.inlfuxDBToken = inlfuxDBToken;
        }

        internal void Dispose()
        {

        }

        internal void WritePoint(string inlfuxDBBucket, string inlfuxDBOrg, PointData point)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create($"{inlfuxDBURL}/api/v2/write?org={inlfuxDBOrg}&bucket={inlfuxDBBucket}&precision=ms");
                    req.Headers.Add(HttpRequestHeader.Authorization, $"Token {inlfuxDBToken}");
                    req.ContentType = "text/plain; charset=utf-8";
                    req.Method = "POST";
                    req.Accept = "application/json";

                    byte[] data = Encoding.UTF8.GetBytes(point.ToLineProtocol());
                    using (System.IO.Stream stream = req.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    await req.GetResponseAsync();
                });
            }
            catch (Exception e)
            {
                api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }

        internal void WritePoints(string inlfuxDBBucket, string inlfuxDBOrg, List<PointData> points)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create($"{inlfuxDBURL}/api/v2/write?org={inlfuxDBOrg}&bucket={inlfuxDBBucket}&precision=ns");
                    req.Headers.Add(HttpRequestHeader.Authorization, $"Token {inlfuxDBToken}");
                    req.ContentType = "text/plain; charset=utf-8";
                    req.Method = "POST";
                    req.Accept = "application/json";

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

                    byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                    using (System.IO.Stream stream = req.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    await req.GetResponseAsync();
                });
            }
            catch (Exception e)
            {
                api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }
    }
}