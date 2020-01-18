using AdiIRCAPIv2.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch___AdiIRC
{
    public static class IRCUtils
    {
        public static string CutSharp(string value)
        {
            if (value.StartsWith("#"))
            {
                return value.Substring(1);
            }
            return value;
        }

        private static void SendWithLog(IServer server, string message)
        {
            //File.AppendAllText(TwitchApi.TwitchApiTools.logPath.Replace("error.log", "send.log"), message + "\r\n");
            server.SendRaw(message);
        }

        public static void SendMessage(IServer server, string channel, string target, string message)
        {
            SendWithLog(server, $"PRIVMSG #{channel} :@{target} {message}");
            server.SendFakeRaw($":Twitch!Twitch@Twitch.tv NOTICE #{channel} :메시지 전달됨: {target} {message}");
        }

        public static void PerformTimeoutForUser(IServer server, string channel, string target, int seconds, string reason = "", string friendlyMessage = "")
        {
            channel = CutSharp(channel);
            target = CutSharp(target);

            if (friendlyMessage != "")
            {
                SendMessage(server, channel, target, friendlyMessage);
            }

            if (seconds == -2)
            {
                return;
            }
            SendWithLog(server, $"PRIVMSG #{channel} :/timeout {target} {seconds} {reason}");
        }

        public static void PerformBanForUser(IServer server, string channel, string target, string reason = "", string friendlyMessage = "")
        {
            channel = CutSharp(channel);
            target = CutSharp(target);
            SendWithLog(server, $"PRIVMSG #{channel} :/ban {target} {reason}");
            if (friendlyMessage != "")
            {
                SendMessage(server, channel, target, friendlyMessage);
            }
        }
    }
}
