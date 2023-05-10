using Th3Essentials;

namespace Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var next = TimeSpan.Zero;
        var nextMin = double.MaxValue;

        var shutdownTImes = new TimeSpan[] { TimeSpan.FromHours(9),TimeSpan.FromHours(12),TimeSpan.FromHours(15),TimeSpan.FromHours(18),TimeSpan.FromHours(21) };
        foreach (var time in shutdownTImes)
        {
            var timeMin = Th3Util.GetTimeTillRestart(time);
            if (timeMin.TotalMinutes < nextMin)
            {
                nextMin = timeMin.TotalMinutes;
                next = time;
            }
        }
        
        Console.WriteLine("");
    }
}