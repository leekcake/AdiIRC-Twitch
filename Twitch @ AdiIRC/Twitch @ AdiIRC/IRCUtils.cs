using AdiIRCAPIv2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch___AdiIRC
{
    public class IRCUtils
    {
        public static void PerformTimeoutForUser(IServer server, string channel, string target, string reason = "")
        {
            foreach(var chan in server.GetChannels)
            {
                return;
            }
        }
    }
}
