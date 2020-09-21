using System;
using System.Collections.Generic;

namespace CBSEssentials.Announcements
{
    public class AnnouncementsConfig
    {
        public List<string> messages;
        public int interval;

        public AnnouncementsConfig()
        {
            messages = new List<string>();
            messages.Add("Welcome to Vintage Story");
            messages.Add("This server is running CBSEssentials");
            interval = 10;
        }

        internal double getInterval()
        {
            return 1000 * 60 * interval;
        }
    }
}