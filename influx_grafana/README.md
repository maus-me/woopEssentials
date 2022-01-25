# Influx + Grafana

Th3Essentials supports logging server metrics, to use this feature, you'll need an influxdb and grafana service running. Here are instructions on how to spin them up using docker-compose.

## Docker Compose configuration

Edit `.env` file to set docker volumes paths and port mappings:

`INFLUXDB_PORT` - port to access influxdb web UI
`INFLUXDB_DATA` - path to store influxdb data (local volume)
`INFLUXDB_CONFIG` - path to store influxdb config (local volume)
`GRAFANA_PORT` - port to access grafana web UI
`GRAFANA_DATA` - path to store grafana data (local volume)

## Run the services

To start the services:

```
docker-compose up -d
```

You can now check the services status by inspecting docker containers (`docker ps`)

## Influx DB configuration

1. Login to InfluxDB web UI (by default `http://localhost:8086`), create the account.
2. For bucket name choose `serverstats`, otherwise you'll need to modify grafana dashboard json.
3. Get API token from `Data -> Buckets -> API Tokens` - you'll need it for `Th3Config.json` and grafana

## Grafana configuration

Login to grafana web UI (by default `http://localhost:3000`) using default credentials `admin/admin`

### Data source configuration

1. Go to `Configuration -> Data Sources`
2. Choose `InfluxDB`:
  - Set `Query Language` to `Flux`
  - Set `URL` to `http://influxdb:8086` (or other port set in `$INFLUXDB_PORT`)
  - Set `Organization` to the one set in account creation stage
  - Set `Token` to the influxdb API Token 
  - Click 'Save & test'

### Dashboard configuration

1. Go to `Dashboards -> Browse` 
2. Click `Import`
3. Paste the contents of `VS_Metrics_Dashboard_Grafana.json` and click 'Load'
