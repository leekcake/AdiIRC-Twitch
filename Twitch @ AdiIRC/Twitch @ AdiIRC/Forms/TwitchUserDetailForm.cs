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

namespace Twitch___AdiIRC.Forms
{
    public partial class TwitchUserDetailForm : Form
    {
        private string channelName;
        private string targetName;

        private string cutSharp(string value)
        {
            if(value.StartsWith("#"))
            {
                return value.Substring(1);
            }
            return value;                
        }

        private IServer caller;
        private TwitchChannel channel, target;
        
        public TwitchUserDetailForm(string channelName, string targetName, IServer caller)
        {
            InitializeComponent();
            this.caller = caller;
            this.channelName = cutSharp(channelName);
            this.targetName = cutSharp(targetName);
        }

        private void TwitchUserDetailForm_Load(object sender, EventArgs e)
        {
            var datas = TwitchApiTools.GetTwitchChannel(new List<string>() { channelName, targetName});
            channel = datas[channelName];
            target = datas[targetName];

            Text = targetName + " #" + channelName;
            UserNameLabel.Text = $"유저 아이디: {target.display_name}(@{targetName})";
        }
    }
}
