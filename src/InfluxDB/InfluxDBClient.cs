using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Vintagestory.API.Server;

namespace InfluxDB
{
    public class InfluxDBClient
    {
        private string inlfuxDBURL;

        private string inlfuxDBToken;

        public bool Disposed;

        ICoreServerAPI api;

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
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create($"{inlfuxDBURL}/api/v2/write?org={inlfuxDBOrg}&bucket={inlfuxDBBucket}&precision=ns");
            req.Headers.Add(HttpRequestHeader.Authorization, $"Token {inlfuxDBToken}");
            req.ContentType = "text/plain; charset=utf-8";
            req.Method = "POST";
            req.Accept = "application/json";

            byte[] data = Encoding.UTF8.GetBytes(point.ToLineProtocol());
            using (System.IO.Stream stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {
                var response = (HttpWebResponse)req.GetResponse();
            }
            catch (Exception e)
            {
                api.Logger.Warning($"[InfluxDB] {e.Message}");
            }
        }

        internal void WritePoints(string inlfuxDBBucket, string inlfuxDBOrg, List<PointData> points)
        {
            foreach (var point in points)
            {
                WritePoint(inlfuxDBBucket, inlfuxDBOrg, point);
            }
        }
    }
}