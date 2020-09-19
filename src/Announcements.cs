using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System;

namespace CBSEssentials.Announcements
{
    class Announcements : ModSystem
    {
        private ICoreServerAPI coreServer;
        private List<string> messages = new List<string>();
        private string path;
        private int currentMsg = 0;
        private int interval = 10;

        /*  public override bool ShouldLoad(EnumAppSide forSide)
         {
             return forSide == EnumAppSide.Server;
         } */

        /* public override void StartServerSide(ICoreServerAPI api)
        {
            path = api.DataBasePath; //Pfad vom VintageStoryData Folder
            Startup(); //checken ob nötige Files da sind

            base.StartServerSide(api);
            coreServer = api;

            using (StreamReader configfile = new StreamReader(path + "/messages.config", true)) //config lesen
            {
                string input;
                string[] split;

                input = configfile.ReadLine();
                split = input.Split(new char[] { ' ' });
                interval = Convert.ToInt32(split[1]); //sehr professionelles auslesen
            }

            using (StreamReader configfile = new StreamReader(path + "/messages.txt", true)) //Messages lesen
            {
                string input;

                while ((input = configfile.ReadLine()) != null)
                {
                    messages.Add(input);
                }
            }

            Timer announcer = new Timer();
            announcer.AutoReset = true;
            announcer.Interval = 1000 * 60 * interval;
            announcer.Elapsed += new ElapsedEventHandler(announceMsg);
            announcer.Enabled = true;
        }
 */
        private void announceMsg(object source, ElapsedEventArgs args) //here is where the magic happens
        {
            if (currentMsg >= messages.Count)
            {
                currentMsg = 1;
                coreServer.BroadcastMessageToAllGroups("<strong>[Info]</strong> " + messages[0], EnumChatType.Notification);
                return;
            }

            coreServer.BroadcastMessageToAllGroups("<strong>[Info]</strong> " + messages[currentMsg], EnumChatType.Notification);
            currentMsg++;
        }

        private void Startup()
        {
            if (!File.Exists(path + "/messages.txt"))
                File.Create(path + "/messages.txt");

            if (!File.Exists(path + "/messages.config"))
            {
                using (StreamWriter sw = File.CreateText(path + "/messages.config"))
                {
                    sw.WriteLine("interval 10");
                }

            }
        }
    }
}