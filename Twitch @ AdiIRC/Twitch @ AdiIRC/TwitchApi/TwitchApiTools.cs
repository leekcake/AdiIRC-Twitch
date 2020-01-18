using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Globalization;
using System.IO;

namespace Twitch___AdiIRC.TwitchApi
{
    public class TwitchApiTools
    {
        public static string logPath;

        static TwitchApiTools()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch
            {

            }
        }

        public static string GetSimpleChannelInformationByName(string userName)
        {
            var id = GetUserId(userName);
            return GetSimpleChannelInformation(id);
        }

        public static Dictionary<string, string> GetSimpleChannelInformationByNames(IEnumerable<string> userNames)
        {
            var users = GetUserIds(userNames);
            var ids = users.Select(user => user.Value).ToList();

            return GetSimpleChannelInformation(ids);
        }

        public static string GetUserId(string userName)
        {
            return GetUserIds(new List<string> { userName }).First().Value;
        }


        public static Dictionary<string, string> GetUserIds(IEnumerable<string> userNames)
        {
            var wc = new WebClient();

            var joinedUserNames = string.Join(",", userNames);

            wc.Headers.Add("Accept: application/vnd.twitchtv.v5+json");
            wc.Headers.Add("Client-ID: 0h7frpcjrc6jdfkrdigesalt76fp9y");
            wc.Encoding = Encoding.UTF8;

            TwitchUsers twitchUsers;

            try
            {
                var responeJson = wc.DownloadString($"https://api.twitch.tv/kraken/users?login={joinedUserNames}");
                twitchUsers = JsonConvert.DeserializeObject<TwitchUsers>(responeJson);
            }
            catch (Exception)
            {
                throw new Exception("Could not connect to twitch api, or bad response.");
            }

            var userIds = new Dictionary<string, string>();
            foreach (var user in twitchUsers.users.Where(user => !userIds.ContainsKey(user.name)))
            {
                userIds.Add(user.name, user._id);
            }

            return userIds;
        }

        public static string GetSimpleChannelInformation(string id)
        {
            return GetSimpleChannelInformation(new List<string> { id }).FirstOrDefault().Value;
        }

        public static Dictionary<string, string> GetSimpleChannelInformation(IEnumerable<string> channelIds)
        {
            var simpleChannelInformation = new Dictionary<string, string>();

            foreach (var channelId in channelIds)
            {
                TwitchChannel twitchChannel;
                try
                {
                    var wc = new WebClient();
                    wc.Headers.Add("Accept: application/vnd.twitchtv.v5+json");
                    wc.Headers.Add("Client-ID: 0h7frpcjrc6jdfkrdigesalt76fp9y");
                    wc.Encoding = Encoding.UTF8;

                    var responeJson = wc.DownloadString($"https://api.twitch.tv/kraken/channels/{channelId}");
                    twitchChannel = JsonConvert.DeserializeObject<TwitchChannel>(responeJson);
                    Console.WriteLine(twitchChannel.name);
                }
                catch (Exception)
                {
                    throw new Exception("Could not connect to twitch api, or bad response.");
                }

                if (!simpleChannelInformation.ContainsKey(twitchChannel.name))
                {
                    simpleChannelInformation.Add(twitchChannel.name, $"[{twitchChannel.game}] {twitchChannel.status}");
                }
            }

            return simpleChannelInformation;
        }

        public static int GetFollowLong(string channelName, string userName)
        {
            try
            {
                var wc = new WebClient();
                wc.Headers.Add("Accept: application/vnd.twitchtv.v5+json");
                wc.Headers.Add("Client-ID: 5z7316926zi0porkdw7b38ubz47a0e");
                wc.Encoding = Encoding.UTF8;

                var url = $"https://api.twitch.tv/kraken/users?login={channelName},{userName}";
                var ids = wc.DownloadString(url);
                dynamic idsJson = JsonConvert.DeserializeObject(ids);
                var channelId = idsJson.users[0]._id.ToString();
                var userId = idsJson.users[1]._id.ToString();

                url = $"https://api.twitch.tv/kraken/users/{userId}/follows/channels/{channelId}";
                wc = new WebClient();
                wc.Headers.Add("Accept: application/vnd.twitchtv.v5+json");
                wc.Headers.Add("Client-ID: 5z7316926zi0porkdw7b38ubz47a0e");
                wc.Encoding = Encoding.UTF8;
                var dataResponse = wc.DownloadString(url);
                dynamic result = JsonConvert.DeserializeObject(dataResponse);

                DateTime time = result.created_at;
                var now = DateTime.Now;

                return now.Subtract(time).Days;
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    //Not Following
                    return -1;
                }
                File.AppendAllText(logPath, $"GetFollowLong({channelName},{userName})" + "\r\n");
                File.AppendAllText(logPath, FlattenException(ex) + "\r\n");
                try
                {
                    var responseStream = ex.Response?.GetResponseStream();
                    string responseText = "";

                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseText = reader.ReadToEnd();
                        }
                    }


                    File.AppendAllText(logPath, "Response: " + responseText + "\r\n");
                }
                catch
                {

                }
                return -1;
            }
        }
        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
