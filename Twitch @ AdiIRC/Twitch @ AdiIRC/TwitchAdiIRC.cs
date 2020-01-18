﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Timer = System.Threading.Timer;
using Twitch___AdiIRC.TwitchApi;
using AdiIRCAPIv2.Arguments.Aliasing;
using AdiIRCAPIv2.Arguments.Channel;
using AdiIRCAPIv2.Arguments.ChannelMessages;
using AdiIRCAPIv2.Arguments.Connection;
using AdiIRCAPIv2.Arguments.WindowInteraction;
using AdiIRCAPIv2.Enumerators;
using AdiIRCAPIv2.Interfaces;
using Twitch___AdiIRC.Forms;
using Twitch___AdiIRC.Menu;

namespace Twitch___AdiIRC
{
    //Inerit form IPlugin to be an AdiIRC plugin.
    public class TwitchAdiIrc : IPlugin
    {
        //Mandatory information fields.
        public string PluginDescription => "Provides simple additional features like emotes for twitch chat integration.";
        public string PluginAuthor => "Xesyto | Edited by leekcake";
        public string PluginName => "Twitch @ AdiIRC";
        public string PluginVersion => "7";
        public string PluginEmail => "s.oudenaarden@gmail.com | leekcake@protonmail.com";

        private IPluginHost _host;
        
        private Timer _topicTimer;
        private List<string> _handledEmotes;

        private Settings _settings;        
        private SettingsForm _settingsForm;

        public void Initialize(IPluginHost host)
        {
            //Store the host in a private field, we want to be able to access it later
            _host = host;

            //Fetch the Config folder and attach the correct path to 
            //Twitch @AdiIRC's config file
            var settingsPath = _host.ConfigFolder + @"\Plugins\TwitchConfig\Config.json";
            TwitchApiTools.logPath = _host.ConfigFolder + @"\Plugins\error.log";

            //Either Load an existing config file or create a new one with default values.
            if (File.Exists(settingsPath))
            {
                _settings = Settings.Load(settingsPath);
            }
            else
            {
                _settings = new Settings {Path = settingsPath};
                _settings.Save();
            }

            //Ensure there is a directory to save emotes into. 
            if (!Directory.Exists(_host.ConfigFolder + @"\TwitchEmotes"))
            {
                Directory.CreateDirectory(_host.ConfigFolder + @"\TwitchEmotes");
            }

            //Intialise private fields
            _handledEmotes = new List<string>();            
            _settingsForm = new SettingsForm(_settings);

            //Register a command to show the settings form
            _host.HookCommand("/twitch@",OnCommand);

            //Register Delegates
            _host.OnChannelJoin += OnChannelJoin;            
            _host.OnMenu += OnMenu;            
            _host.OnChannelNormalMessage += OnChannelNormalMessage;    
            _host.OnEditboxKeyUp += OnEditboxKeyUp;
            _host.OnStringDataReceived += OnStringDataReceived;
            _host.OnStringDataSent += OnStringDataSent;

            //Start a timer to update all channel topics regularly
            _topicTimer = new Timer(state => TopicUpdate(), true, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10));
        }

        /* Twitch uses many messages that are not technically part of the IRC
         *  protocol. A few Examples:
         *
         *
         * USERNOTICE to notify about (re)-subs
         * CLEARCHAT to timeout/ban people
         * USERSTATE to inform about users states
         * ROOMSTATE to inform about channel states
         * WHISPER for bot private messages
         *            
         * Because AdirIRC doesn't recongize those as real irc messages
         *  we're goign to handle that very low level instead of
         *  higher up in events like ChannelNormalMessage.

         * We'll then either rewrite those messages into things AdiIRC does 
         *  understand using SendFakeRaw or take other actions 
         *  and finally Eat the original Raw Event.
         */
        private void OnStringDataReceived(StringDataReceivedArgs argument)
        {                        
            //Check if this event was fired on twitch, if not this plugin should 
            //never touch it so fires an early return.
            if (!IsTwitchServer(argument.Server))
            {
                return;
            }
            
            //We'll need these later, frequently.
            var server = argument.Server;
            var rawMessage = argument.Data;
            var tags = TwitchRawEventHandlers.ParseTagsFromString(rawMessage);

            //Regexes are fairly expensive so we do an initial check with .Contains. 
            //Only after that do we dispatch to a specific handler for that kind of Message

            //NOTICE is a normal irc message but due to how twitch sends them 
            //they don't arrive in the channel windows, but in the server.
            if (rawMessage.Contains("NOTICE"))
            {
                //Returns True if it succesfully handled a NOTICE message
                if (TwitchRawEventHandlers.Notice(server, rawMessage))
                {
                    //By setting the Data of the event to null AdiIRC will no longer parse this Message further.
                    argument.Data = null;
                    return;
                }
            }

            //CLEARCHAT is a message used by twitch to Timeout/Ban people, and clear their
            //Text lines, We won't clear the text but will display the ban information
            if (rawMessage.Contains("CLEARCHAT"))
            {
                //Returns True if it succesfully handled a Clearchat
                if (TwitchRawEventHandlers.ClearChat(server, rawMessage,_settings.ShowTimeouts, tags))
                {
                    //By setting the Data of the event to null AdiIRC will no longer parse this Message further.
                    argument.Data = null;
                    return;
                }                
            }

            //USERNOTICE is a message used by twitch to inform about (Re-)Subscriptions            
            if (rawMessage.Contains("USERNOTICE"))
            {
                //Returns True if it succesfully handled a Clearchat
                if (TwitchRawEventHandlers.Usernotice(server, rawMessage, _settings.ShowSubs, tags))
                {
                    //By setting the Data of the event to null AdiIRC will no longer parse this Message further.
                    argument.Data = null;
                    return;
                }
            }

            //WHISPER is a message used by twitch to handle private messsages between users ( and bots )
            //But its not a normal IRC message type, so they have to be rewritten into PRIVMSG's
            if (rawMessage.Contains("WHISPER"))
            {
                //Returns True if it succesfully handled a WHISPER
                if (TwitchRawEventHandlers.WhisperReceived(server, rawMessage))
                {
                    //By setting the Data of the event to null AdiIRC will no longer parse this Message further.
                    argument.Data = null;
                    return;
                }
            }

            //PRIVMSG is how irc handles normal text messsasges between users and to channels
            //We need to hook into these to add unicode icon badges to usernames
            if (rawMessage.Contains("PRIVMSG"))
            {                                
                //Check if we should show badges before doing work.
                if (!_settings.ShowBadges && !_settings.ShowFollowLong)
                {
                    return;
                }
                
                //Parse message into a TwitchMessage
                var twitchMessage = new TwitchIrcMessage(rawMessage);
                twitchMessage.DisplayFollowLong = _settings.ShowFollowLong;

                //Check if there are badges or user has custom displayName, if so, insert them into event.
                if (twitchMessage.NeedtoEditMessage)
                {
                    var newName = twitchMessage.BadgeList + twitchMessage.UserDisplayName;
                    argument.Data = rawMessage.Replace($":{twitchMessage.UserName}!", $":{newName}!");
                    //argument.Data = rawMessage.Replace(twitchMessage.RawMessage, twitchMessage.Message);
                }
            }

            //Final filter on some message types Twitch@AdiIRC does not need to handle but that are not proper IRC messages.
            if (rawMessage.Contains("ROOMSTATE") || rawMessage.Contains("USERSTATE")  || rawMessage.Contains("HOSTTARGET") || rawMessage.Contains("GLOBALUSERSTATE") )
            {
                //Silently eat these messages and do nothing. They only cause empty * lines to appear in the server tab and Twitch@AdiIRC does not use them
                argument.Data = null;
            }
        }

        private void OnChannelNormalMessage(ChannelNormalMessageArgs argument)
        {
            //Check if this event was fired on twitch, if not this plugin should 
            //never touch it so fires an early return.
            if (!IsTwitchServer(argument.Channel.Server))
            {
                return;
            }
            
            //Convert to a TwitchIrcMessage which handles parsing all the information
            var twitchMessage = new TwitchIrcMessage(argument);
            
            //Check if there are any emotes, if so iterate over them all and Register them.
            if (twitchMessage.HasEmotes)
            {                              
                foreach (var emote in twitchMessage.Emotes)
                {                    
                    RegisterEmote(emote);
                }
            }

            //Check if there are any bits, if so register them as emotes and show a notice
            if (_settings.ShowCheers && twitchMessage.Tags.ContainsKey("bits"))
            {                
                if (RegisterBits(twitchMessage.Tags["bits"]))
                {                    
                    var emoteName = "cheer" + twitchMessage.Tags["bits"];
                    var bitsMessage = twitchMessage.Tags["bits"] + " bits";

                    var notice = $":Twitch!Twitch@tmi.twitch.tv NOTICE {argument.Channel.Name} :{twitchMessage.UserName} {emoteName} just cheered for {bitsMessage}! {emoteName}";
                    argument.Channel.Server.SendFakeRaw(notice);
                }
            }
        }
       
        private void OnStringDataSent(StringDataSentArgs argument)
        {
            //Check if this event was fired on twitch, if not this plugin should 
            //never touch it so fires an early return.
            if (!IsTwitchServer(argument.Server))
            {
                return;
            }

            //Private messasges to users are not handles normally through twitch
            //Instead they require a /w command to the jtv user.
            //And then Twitch sends a WHISPER message to that user in your stead.

            //So here we catch all PRIVMSG events the client sends to users
            //And translate them into /w commands to jtv

            var whisperRegex = @"PRIVMSG ((?!jtv )[^#]\S*) \x3A(.+)$";
            var whisperMatch = Regex.Match(argument.Data, whisperRegex);

            if (whisperMatch.Success)
            {
                var target = whisperMatch.Groups[1].ToString();
                var message = whisperMatch.Groups[2].ToString();

                var newMessage = $"PRIVMSG jtv :/w {target} {message}";
                argument.Server.SendRaw(newMessage);

                //Supress event.
                argument.Data = null;
            }
        }

        private void OnMenu(MenuEventArgs argument)
        {            
            //Query windows have a null window, break early on these
            if (argument.Window == null)
            {
                return;
            }

            //The plugin should only add its config menu too relevant entries, 
            //it makes no sense to add it to say the rightclick menu of a link
            //For now that means the Commands menu and the rightclick menu of the twitch Server windows.
            if ( (IsTwitchServer(argument.Window.Server) && argument.MenuType == MenuType.Server) || argument.MenuType == MenuType.Menubar)
            {                
                var menuItems = argument.MenuItems;

                if (menuItems == null)
                {
                    return;
                }
             
                var toolStripMenuItem = new ToolStripMenuItem("Twitch@AdiIRC");
                toolStripMenuItem.Click += delegate {
                    _settingsForm.Show();
                };
             
                menuItems.Add(new ToolStripSeparator());
                menuItems.Add(toolStripMenuItem);
            }
            else if((IsTwitchServer(argument.Window.Server) && argument.MenuType == MenuType.ChannelLink))
            {
                ChannelLinkMenuHandler.InsertMenuTo(argument);
            }
        }

        private void OnCommand(RegisteredCommandArgs argument)
        {
            _settingsForm.Show();
        }
        
        private void OnEditboxKeyUp(EditboxKeyUpArgs argument)
        {
            //Check if this event was fired on twitch, or if the config says we should not adjust autocomplete
            if (!IsTwitchServer(argument.Window.Server) || !_settings.AutoComplete)
            {
                return;
            }

            //Early exit if its not a tab key 
            if (argument.KeyEventArgs.KeyCode != Keys.Tab)
            {
                return;
            }            

            //Check if we're operating in a channel window, otherwise exit.
            var channel = argument.Window as IChannel;
            if (channel == null)
            {
                return;
            }

            //Don't do work on an empty string
            if (string.IsNullOrWhiteSpace(argument.Editbox.Text))
            {
                return;
            }

            var editBoxCursor = argument.Editbox.SelectionStart;            
            var text = argument.Editbox.Text;


            //Search for the wordstart.
            var wordStartTuple = FindStringWordStartIndex(text, _host.EditboxOptions.TabCompleteSuffixFirstWord, editBoxCursor);

            if (wordStartTuple.Item2 == editBoxCursor)
            {
                //The FindStringWordStartIndex did not offset the initial word cursor 
                //so it did not find a suffix. That means we can try and find the second suffix type.

                wordStartTuple = FindStringWordStartIndex(text, _host.EditboxOptions.TabCompleteSuffix, editBoxCursor);
            }

            var i = wordStartTuple.Item1;
            editBoxCursor = wordStartTuple.Item2;

            //Substring to get a word           
            var word = text.Substring(i, editBoxCursor - i);
            var isValidName = false;

            //See if the word is a valid nickname. 
            foreach (IUser user in channel.GetUsers)
            {
                if (user.Nick == word)
                {
                    isValidName = true;
                    break;
                }
            }

            //Exit early if we don't need to edit the textbox.
            if (!isValidName)
            {
                return;
            }

            //Supress further actions on this Event
            argument.KeyEventArgs.SuppressKeyPress = true;

            //Remember old selectionstart, changing text resets it.
            var oldSelectionSTart = argument.Editbox.SelectionStart;
            //Insert @
            argument.Editbox.Text = text.Insert(i, "@");
            //Fix the cursor position.
            argument.Editbox.SelectionStart = oldSelectionSTart + 1;
        }
        
        private void OnChannelJoin(ChannelJoinArgs argument)
        {
            //Check if this event was fired on twitch, if not this plugin should 
            //never touch it so fires an early return.
            if (!IsTwitchServer(argument.Channel.Server))
            {
                return;
            }
                            
            var server = argument.Channel.Server;
            var channelName = argument.Channel.Name;
            var userName = argument.Channel.Name.TrimStart('#');
            string topicData;

            //Check if this event fired on the client joining the channel or 
            //someone else joining, we only need to set the topic of a channel
            //when we join a channel.
            if (argument.User.Nick != argument.Channel.Server.Nick)
            {
                return;
            }

            //TwitchApiTools connects to the web, disk or web IO is unreliable 
            //so handle it in a try / catch block
            try
            {                
                topicData = TwitchApiTools.GetSimpleChannelInformationByName(userName);
                
            }
            catch (Exception)
            {
                topicData = $"Twitch@AdiIRC: Could not find channel topic data for {userName}.";
            }

            //Finally set the topic title through a raw IRC message.
            var topicMessage = $":Twitch!Twitch@Twitch.tv TOPIC {channelName} :{topicData}";
            server.SendFakeRaw(topicMessage);
        }

        private Tuple<int, int> FindStringWordStartIndex(string text, string wordSuffix, int startIndex)
        {
            var i = startIndex;

            //Adjust backwards one due to how cursor position works. Then correct for out of bounds.
            i--;
            if (i < 0)
            {
                i = 0;
            }

            //If the text behind the cusor matches autocomplete inserted text, set i back by that much
            var iOffset = i - wordSuffix.Length;
            if (iOffset > 0)
            {
                var subString = text.Substring(iOffset + 1, wordSuffix.Length);

                //Adjust backwards the length of the suffix
                if (subString == wordSuffix)
                {
                    i = iOffset;
                    startIndex -= wordSuffix.Length;
                }
            }

            //Search backwards to find the end of the current word.
            while (text[i] != ' ' && i > 0)
            {
                i--;
            }

            //Offset one if its not the start of the text, don't want to include the spaces we searched for
            if (i != 0)
            {
                i++;
            }

            return Tuple.Create(i, startIndex);
        }

        private void TopicUpdate()
        {
            //Find any twitch server connections in the serverlist. there might be
            //more than one and there might be none so storing it statically is impractical
            foreach (IServer server in _host.GetServers)
            {
                if (IsTwitchServer(server))
                {
                    var channels = server.GetChannels;

                    //Iterate over all channels, updating the topic.
                    foreach (IChannel channel in channels)
                    {
                        string topicData;
                        var userName = channel.Name.TrimStart('#');

                        //TwitchApiTools connects to the web, disk or web IO is unreliable 
                        //so handle it in a try / catch block
                        try
                        {                           
                            topicData = TwitchApiTools.GetSimpleChannelInformationByName(userName);
                        }
                        catch (Exception)
                        {
                            topicData = $"Twitch@AdiIRC: Could not find channel topic data for {userName}.";                            
                        }

                        //AdiIRC will let you set a topic to the same thing, this avoids repetitive topic updates. 
                        if (channel.Topic != topicData)
                        {
                            var topicMessage = $":Twitch!Twitch@Twitch.tv TOPIC #{userName} :{topicData}";
                            server.SendFakeRaw(topicMessage);
                        }
                    }
                }
            }
        }

        public void RegisterEmote(TwitchEmote emote)
        {
            var window = _host.ActiveIWindow;            
            
            //Check if we've already added this emote
            if (_handledEmotes.Contains(emote.Name))
                return;
            
            var emoteDirectory = _host.ConfigFolder + @"\TwitchEmotes";            
            var emoteFile = $"{emoteDirectory}\\{emote.Id}.png";
            
            //Check if we've already downloaded this emote earlier, if so just 
            //add the existing file.
            if (File.Exists(emoteFile))
            {                
                //ExecuteCommand executes a scripting command like you entered 
                //into the window ExecteCommand is being invoked on. 
                //https://dev.adiirc.com/projects/adiirc/wiki/Scripting_Commands

                //AdiIRC will supress the output of a slashcommand if its 
                //instead invoked with a starting .

                //Setoption is a slash command to add or change options in the .ini file
                //https://dev.adiirc.com/projects/adiirc/wiki/Setoption
                var command = $".setoption Emoticons Emoticon_{emote.Name} {emoteFile}";
                window.ExecuteCommand(command);                
                _handledEmotes.Add(emote.Name);
                return;
            }
            
            //Try to download the emote, then add it
            if (emote.DownloadEmote(emoteFile))
            {
                //See above              
                var command = $".setoption Emoticons Emoticon_{emote.Name} {emoteFile}";
                window.ExecuteCommand(command);                
                _handledEmotes.Add(emote.Name);
            }
        }

        public bool RegisterBits(string bitCount)
        {
            var window = _host.ActiveIWindow;

            if (string.IsNullOrEmpty(bitCount))
                return false;

            //Create a new Bit, its basically the same idea as an emote with slightly different specifics
            var bit = new TwitchBit{Amount = bitCount};


            var emoteDirectory = _host.ConfigFolder + @"\TwitchEmotes";
            var emoteFile = $"{emoteDirectory}\\{bit.Name}.png";

            //Already registered this bit earlyier
            if (_handledEmotes.Contains(bit.Name))
                return true;

            //Check if we've already downloaded this bit earlier, if so just 
            //add the existing file.
            if (File.Exists(emoteFile))
            {
                //ExecuteCommand executes a scripting command like you entered 
                //into the window ExecteCommand is being invoked on. 
                //https://dev.adiirc.com/projects/adiirc/wiki/Scripting_Commands

                //AdiIRC will supress the output of a slashcommand if its 
                //instead invoked with a starting .

                //Setoption is a slash command to add or change options in the .ini file
                //https://dev.adiirc.com/projects/adiirc/wiki/Setoption
                var command = $".setoption Emoticons Emoticon_{bit.Name} {emoteFile}";
                window.ExecuteCommand(command);
                _handledEmotes.Add(bit.Name);
                return true;
            }

            //Try to download the emote, then add it
            if (bit.DownloadBit(emoteFile))
            {
                //See above              
                var command = $".setoption Emoticons Emoticon_{bit.Name} {emoteFile}";
                window.ExecuteCommand(command);
                _handledEmotes.Add(bit.Name);
                return true;
            }

            return false;
        }

        private bool IsTwitchServer(IServer server)
        {
            return server != null &&
                   server.Network.ToLower().Contains("twitch") 
                   && server.NetworkLabel.ToLower().Contains("twitch");
        }

        public void Dispose()
        {
            _settings.Save();        
        }        
    }
}
