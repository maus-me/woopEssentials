using Th3Essentials.Config;
using Xunit.Abstractions;

namespace Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestFirstTimeNextDay()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00"),
            ShutdownTimes = new[]
                { TimeSpan.Parse("10:52:00"), TimeSpan.Parse("10:53:00"), TimeSpan.Parse("10:54:00") }
        };
        var now = DateTime.Parse("5/14/2023 11:37:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/15/2023 10:52:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("23:14:58") , timeTillRestart);
    }

    [Fact]
    public void TestTimeBetweenTodayAndNextDay()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00"),
            ShutdownTimes = new[]
                { TimeSpan.Parse("10:10:00"), TimeSpan.Parse("10:30:00"), TimeSpan.Parse("10:40:00") }
        };
        var now = DateTime.Parse("5/14/2023 10:20:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/14/2023 10:30:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("00:09:58") , timeTillRestart);
    }

    [Fact]
    public void TestLastTimeOfToday()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00"),
            ShutdownTimes = new[]
                { TimeSpan.Parse("10:10:00"), TimeSpan.Parse("10:30:00"), TimeSpan.Parse("10:40:00") }
        };
        var now = DateTime.Parse("5/14/2023 10:35:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/14/2023 10:40:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("00:04:58") , timeTillRestart);
    }

    [Fact]
    public void TestFirstTimeOfToday()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00"),
            ShutdownTimes = new[]
                { TimeSpan.Parse("10:10:00"), TimeSpan.Parse("10:30:00"), TimeSpan.Parse("10:40:00") }
        };
        var now = DateTime.Parse("5/14/2023 10:05:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/14/2023 10:10:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("00:04:58") , timeTillRestart);
    }

    [Fact]
    public void TestOnlyOneTimeNext()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00")
        };
        var now = DateTime.Parse("5/14/2023 10:05:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/15/2023 07:43:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("21:37:58") , timeTillRestart);
    }

    [Fact]
    public void TestOnlyOneTimeToday()
    {
        Th3Essentials.Th3Essentials.Config = new Th3Config
        {
            ShutdownTime = TimeSpan.Parse("07:43:00")
        };
        var now = DateTime.Parse("5/14/2023 7:05:02 AM"); 
        
        
        Th3Essentials.Th3Essentials.LoadRestartTime(now);
        Assert.Equal(DateTime.Parse("5/14/2023 07:43:00 AM"), Th3Essentials.Th3Essentials.ShutDownTime);
        var timeTillRestart = Th3Essentials.Th3Essentials.ShutDownTime - now;
        Assert.Equal(TimeSpan.Parse("00:37:58") , timeTillRestart);
    }

    [Fact]
    public void TestTime()
    {
        var time1 = DateTime.Parse("3/08/2025 3:10:10 AM");
        var time2 = DateTime.Parse("3/08/2025 3:01:09 AM");
        var time3 = DateTime.Parse("3/07/2025 7:10:10 AM");
        var time4 = DateTime.Parse("3/08/2025 7:10:09 AM");
        var time5 = DateTime.Parse("3/08/2025 11:10:10 AM");
        var time6 = DateTime.Parse("3/08/2025 11:10:11 AM");
        var time7 = DateTime.Parse("3/09/2025 8:10:10 AM");
        
        var now = DateTime.Parse("3/08/2025 7:10:10 AM");

        var seconds = 86400*1;
        
        var calc1 = time1.AddSeconds(seconds) - now;
        var calc2 = time2.AddSeconds(seconds) - now;
        var calc3 = time3.AddSeconds(seconds) - now;
        var calc4 = time4.AddSeconds(seconds) - now;
        var calc5 = time5.AddSeconds(seconds) - now;
        var calc6 = time6.AddSeconds(seconds) - now;
        var calc7 = time7.AddSeconds(seconds) - now;

        _testOutputHelper.WriteLine(PrettyTime(calc1));
        _testOutputHelper.WriteLine(PrettyTime(calc2));
        _testOutputHelper.WriteLine(PrettyTime(calc3));
        _testOutputHelper.WriteLine(PrettyTime(calc4));
        _testOutputHelper.WriteLine(PrettyTime(calc5));
        _testOutputHelper.WriteLine(PrettyTime(calc6));
        _testOutputHelper.WriteLine(PrettyTime(calc7));
    }

    public static string PrettyTime(TimeSpan span)
    {
        var parts = new List<string>();
        if (span.Days > 0)
            parts.Add($"{span.Days} day{(span.Days == 1 ? "" : "s")}");
        if (span.Hours > 0)
            parts.Add($"{span.Hours} hour{(span.Hours == 1 ? "" : "s")}");
        if (span.Minutes > 0)
            parts.Add($"{span.Minutes} minute{(span.Minutes == 1 ? "" : "s")}");
        if (span.Seconds > 0 || parts.Count == 0) // Always show seconds if nothing else
            parts.Add($"{span.Seconds} second{(span.Seconds == 1 ? "" : "s")}");

        return string.Join(", ", parts);
    }
}