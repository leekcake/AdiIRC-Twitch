using AdiIRCAPIv2.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Twitch___AdiIRC.TwitchApi;
using static Twitch___AdiIRC.IRCUtils;

namespace Twitch___AdiIRC.Forms
{
    public partial class TwitchUserDetailForm : Form
    {
        private string channelName;
        private string targetName;

        private IServer caller;
        private TwitchChannel channel, target;
        
        public TwitchUserDetailForm(string channelName, string targetName, IServer caller)
        {
            InitializeComponent();
            this.caller = caller;
            this.channelName = CutSharp(channelName);
            this.targetName = CutSharp(targetName);
        }

        private void TwitchUserDetailForm_Load(object sender, EventArgs e)
        {
            var ids = TwitchApiTools.GetUserIds(new List<string>() { channelName, targetName });
            var datas = TwitchApiTools.GetTwitchChannel(ids.Values);
            channel = datas[channelName];
            target = datas[targetName];

            Text = target.SimpleDescription + " @ " + channel.SimpleDescription;
            UserNameLabel.Text = $"대상 유저: {target.SimpleDescription}";
        }
    }
}
