using System;

namespace Th3Essentials
{
  public class Th3Util
  {
    public static TimeSpan GetTimeTillRestart()
    {
      DateTime now = DateTime.Now;
      DateTime restartDate = new DateTime(now.Year, now.Month, now.Day, Th3Essentials.Config.ShutdownTime.Hours, Th3Essentials.Config.ShutdownTime.Minutes, Th3Essentials.Config.ShutdownTime.Seconds);

      if (now.TimeOfDay > Th3Essentials.Config.ShutdownTime)
      {
        restartDate = restartDate.AddDays(1);
      }
      return restartDate - now;
    }
  }
}