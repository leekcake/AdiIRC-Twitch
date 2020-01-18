using System;
using System.IO;
using System.Net;

namespace Twitch___AdiIRC
{
    public class TwitchEmote
    {
        public string Id;
        public string Name;
        public string URL => $"http://static-cdn.jtvnw.net/emoticons/v1/{Id}/1.0";

        public bool DownloadEmote(string filepath)
        {
            
            try
            {
                var wc = new WebClient();
                wc.DownloadFile(URL, filepath);
            }
            catch (Exception ex)
            {
                File.AppendAllText(TwitchApi.TwitchApiTools.logPath, TwitchApi.TwitchApiTools.FlattenException(ex));
                return false;
            }

            return true;
        }
    }
}
