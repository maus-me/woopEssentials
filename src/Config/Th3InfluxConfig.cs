namespace Th3Essentials.Config
{
  public class Th3InfluxConfig
  {
    public string InlfuxDBURL = null;

    public string InlfuxDBToken = null;

    public string InlfuxDBBucket = null;

    public string InlfuxDBOrg = null;

    public bool InlfuxDBOverwriteLogTicks = false;

    public int InlfuxDBLogtickThreshold = 20;

    public bool Debug;
  }
}