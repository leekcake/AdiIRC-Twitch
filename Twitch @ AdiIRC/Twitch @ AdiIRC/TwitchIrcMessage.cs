using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AdiIRCAPIv2.Arguments.ChannelMessages;
using Twitch___AdiIRC.TwitchApi;

namespace Twitch___AdiIRC
{
    public class TwitchIrcMessage
    {
        public class FollowCache
        {
            public int FollowDay {
                get; private set;
            }
            public int Encounter {
                get; private set;
            } = 0;

            public FollowCache(int day)
            {
                FollowDay = day;
            } 

            public void NewEncount()
            {
                Encounter++;
            }
            public string Display()
            {
                if(Encounter < 10 && FollowDay < 7)
                {
                    if (FollowDay == -1) return "#XX";
                    return "#" + FollowDay;
                }
                else
                {
                    if (FollowDay == -1) return "XX";
                    return FollowDay + "";
                }
            }
        }

        private static Dictionary<string, Dictionary<string, FollowCache>> FollowData = new Dictionary<string, Dictionary<string, FollowCache>>();
        public static FollowCache GetFollowData(string channel, string id)
        {
            if (!FollowData.ContainsKey(channel))
            {
                FollowData[channel] = new Dictionary<string, FollowCache>();
            }
            var dict = FollowData[channel];

            if(dict.ContainsKey(id))
            {
                return dict[id];
            }

            var follow = new FollowCache(TwitchApiTools.GetFollowLong(channel, id));
            dict[id] = follow;
            return follow;
        }

        public string Message;
        public string Channel;
        public string UserName;
        private string userDisplayName;
        public string UserDisplayName {
            get {
                StringBuilder result = new StringBuilder();
                if (userDisplayName != null) result.Append(userDisplayName + "(" + UserName + ")");
                else result.Append(UserName);

                if( DisplayFollowLong)
                {
                    //#e_yeon -> e_yeon
                    var channelName = Channel.Substring(1);
                    if (channelName != UserName)
                    {
                        var follow = GetFollowData(channelName, UserName);
                        follow.NewEncount();
                        result.Append("#");
                        result.Append(follow.Display());
                    }
                }

                return result.ToString();
            }
        }

        public string UserMask;
        public string BadgeList;
        public bool HasEmotes;
        public bool HasBadges;
        public Dictionary<string, string> Tags;
        public List<TwitchEmote> Emotes;

        public bool DisplayFollowLong;

        public bool NeedtoEditMessage {
            get {
                return DisplayFollowLong || HasEmotes || userDisplayName != null;
            }
        }

        private static readonly string _emoteRegex = @"((\w+):(\d+)-(\d+))";
        private static readonly string _messageRegex = @"@(.+?) :((.+)!.+?) PRIVMSG (#.+?) :(.+)";        

        public TwitchIrcMessage(ChannelNormalMessageArgs argument)
        {
            Channel = argument.Channel.Name;
            //Assign information we care about to fields.
            Message = argument.Message;
            Tags = (Dictionary<string,string>) argument.MessageTags;
            UserMask = argument.User.Host;
            UserName = argument.User.Nick;

            if(Tags.ContainsKey("display-name"))
            {
                userDisplayName = Tags["display-name"];
            }

            //Parse the Tags for emotes
            //I'm being lazy on int.parse security so i'm throwing in a
            //catchall Exception 
            try
            {
                HasEmotes = ExtractEmotes();
            }
            catch (Exception)
            {
                HasEmotes = false;
            }

            BadgeList = BadgesinTags(Tags);
            HasBadges = !string.IsNullOrWhiteSpace(BadgeList);
        }

        public TwitchIrcMessage(string message)
        {
            //Parse a raw twitch message into a the seperate parts I care about.

            var messageMatch = Regex.Match(message, _messageRegex);

            if (!messageMatch.Success)
            {
                throw new ArgumentException("Not a valid IRCv3 with tags message.");
            }

            //Simple assignments.    
            UserMask = messageMatch.Groups[2].ToString();
            UserName = messageMatch.Groups[3].ToString();
            Channel = messageMatch.Groups[4].ToString();
            Message = messageMatch.Groups[5].ToString();

            //Tags
            Tags = TwitchRawEventHandlers.ParseTagsFromString(message);

            if (Tags.ContainsKey("display-name"))
            {
                userDisplayName = Tags["display-name"];
            }

            //Parse the Tags for emotes
            //I'm being lazy on int.parse security so i'm throwing in a
            //catchall Exception 
            try
            {
                ExtractEmotes();
            }
            catch (Exception)
            {
                HasEmotes = false;
            }

            BadgeList = BadgesinTags(Tags);
            HasBadges = !string.IsNullOrWhiteSpace(BadgeList);
        }

        public static string BadgesinTags(Dictionary<string,string> tags)
        {
            var badgeList = "";

            if (!tags.ContainsKey("badges"))
            {                
                return null;
            }

            var badges = tags["badges"];

            if (badges.Contains("broadcaster/1"))
            {
                badgeList += "📺";
            }

            if (badges.Contains("staff/1"))
            {
                badgeList += "🔧";
            }

            if (badges.Contains("admin/1"))
            {
                badgeList += "🛡️";
            }

            if (badges.Contains("global_mod/1"))
            {
                badgeList += "⚔️";
            }

            if (badges.Contains("moderator/1"))
            {
                badgeList += "🗡️";
            }

            if(badges.Contains("vip/"))
            {
                badgeList += "💎";
            }

            if (badges.Contains("subscriber/"))
            {
                badgeList += "⭐";
            }

            if (badges.Contains("turbo/1"))
            {
                badgeList += "⚡";
            }

            if (badges.Contains("prime/1"))
            {
                badgeList += "👑";
            }

            return badgeList;
        }

        private bool ExtractEmotes()
        {
            //Early exit if the tags does not contain any emotes.
            if (!Tags.ContainsKey("emotes"))
            {
                return false;
            }

            Emotes = new List<TwitchEmote>();

            var emoteMatches = Regex.Matches(Tags["emotes"], _emoteRegex);

            //Early exit if no emotes were actually matched.
            if (emoteMatches.Count <= 0)
            {
                return false;
            }

            //Emotes are received as an id and the partof the message they match too
            //So we substring the actual name of the mote out of the message.
            foreach (Match match in emoteMatches)
            {
                var emoteId = match.Groups[2].ToString();
                var startIndex = int.Parse(match.Groups[3].ToString());
                var endIndex = int.Parse(match.Groups[4].ToString());

                var emoteName = Message.Substring(startIndex, endIndex - startIndex + 1);

                var emote = new TwitchEmote { Id = emoteId, Name = emoteName };

                Emotes.Add(emote);
            }

            return true;
        }
    }
}
