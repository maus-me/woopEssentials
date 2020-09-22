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
            interval = 10;
        }

        public void init()
        {
            messages.Add("Welcome to Vintage Story");
            messages.Add("This server is running CBSEssentials");
        }

        internal double getInterval()
        {
            return 1000 * 60 * interval;
        }
    }
}