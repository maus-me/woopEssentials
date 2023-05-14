using Th3Essentials.Config;

namespace Tests;

public class UnitTest1
{
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
}