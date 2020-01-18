using AdiIRCAPIv2.Arguments.WindowInteraction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Twitch___AdiIRC.Forms;

namespace Twitch___AdiIRC.Menu
{
    public class BaseManagement : ToolStripMenuItem
    {
        private MenuEventArgs argument;
        private string reason;
        private int time;
        private string friendlyMessage;
        private string banMessage;
        public BaseManagement(MenuEventArgs argument, string reason, int time, string friendlyMessage, string banMessage = "님은 통제를 따라주시지 않으셔서 더이상 이 채널에서 채팅을 치실수 없게 되었습니다. HSWP")
        {
            this.argument = argument;
            this.reason = reason;
            this.time = time;
            this.friendlyMessage = friendlyMessage;
            this.banMessage = banMessage;

            Text = $"{time}초";
            if (time == -1)
            {
                Text = "영구차단";
            }
            if (time == -2)
            {
                Text = "단순경고";
            }
            Click += BaseManagement_Click;
        }

        private void BaseManagement_Click(object sender, EventArgs e)
        {
            if (time == -1)
            {
                IRCUtils.PerformBanForUser(argument.Window.Server, argument.Window.Name, argument.Text, reason, banMessage);
                return;
            }
            IRCUtils.PerformTimeoutForUser(argument.Window.Server, argument.Window.Name, argument.Text, time, reason, friendlyMessage);
        }
    }
    public class AmityManagement : BaseManagement
    {
        public AmityManagement(MenuEventArgs argument, int time)
            : base(argument, "친목", time, "님 안녕하세요 VoHiYo ! 죄송하지만 이 채팅방에서는 친목질(닉네임 언급 등)이 금지되어 있습니다. 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class LineManagement : BaseManagement
    {
        public LineManagement(MenuEventArgs argument, int time)
            : base(argument, "선넘네?", time, "님 안녕하세요 VoHiYo ! 죄송하지만 이 채팅방에서는 선을 넘는 행동이 금지되어 있습니다! 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class LowWordManagement : BaseManagement
    {
        public LowWordManagement(MenuEventArgs argument, int time)
            : base(argument, "반말", time, "님 안녕하세요 VoHiYo ! 죄송하지만 이 채팅방에서는 반말이 금지되어 있습니다! 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class BadWordManagement : BaseManagement
    {
        public BadWordManagement(MenuEventArgs argument, int time)
            : base(argument, "욕설", time, "님 안녕하세요 VoHiYo! 죄송하지만 이 채팅방에서는 욕설이 금지되어 있습니다! 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class SexualManagement : BaseManagement
    {
        public SexualManagement(MenuEventArgs argument, int time)
            : base(argument, "성적멘트", time, "님 안녕하세요 VoHiYo ! 죄송하지만 이 채팅방에서는 수위가 높은 채팅이 금지되어 있습니다! 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class FastChatManagement : BaseManagement
    {
        public FastChatManagement(MenuEventArgs argument, int time)
            : base(argument, "도배", time, "님 안녕하세요 VoHiYo ! 죄송하지만 이 채팅방에서는 도배가 금지되어 있습니다! 규칙을 한번만 확인해주세요! 감사합니다!")
        {

        }
    }

    public class TrashManagement : BaseManagement
    {
        public TrashManagement(MenuEventArgs argument, int time)
            : base(argument, "쓰레기", time, "님 안녕하세요 VoHiYo ! 죄송하지만 쓰레기보다도 못한 사람은 이 채팅방에서 말을 할 수 없습니다! 감사합니다!",
                  "님 안녕하세요 VoHiYo ! 죄송하지만 쓰레기보다도 못한 사람은 이 채팅방에서 말을 할 수 없습니다! 감사합니다!")
        {
            Text = "쓰레기";
        }
    }

    public static class ChannelLinkMenuHandler
    {
        private static ToolStripItem[] BuildDefaultManagement(MenuEventArgs args, Type type)
        {
            return new ToolStripItem[]
            {
                (BaseManagement) Activator.CreateInstance(type, args, -2),
                (BaseManagement) Activator.CreateInstance(type, args, 5),
                (BaseManagement) Activator.CreateInstance(type, args, 30),
                (BaseManagement) Activator.CreateInstance(type, args, 60),
                (BaseManagement) Activator.CreateInstance(type, args, 600),
                (BaseManagement) Activator.CreateInstance(type, args, -1)
            };
        }

        public static void InsertMenuTo(MenuEventArgs argument)
        {
            var menuItems = argument.MenuItems;

            if (menuItems == null)
            {
                return;
            }
            menuItems.Add(new ToolStripSeparator());

            var toolStripMenuItem = new ToolStripMenuItem("Twitch@AdiIRC");
            toolStripMenuItem.Enabled = false;
            menuItems.Add(toolStripMenuItem);

            var detailItem = new ToolStripMenuItem("유저 상세정보");
            detailItem.Click += delegate {
                var form = new TwitchUserDetailForm(argument.Window.Name, argument.Text, argument.Window.Server);
                form.Show();
            };
            menuItems.Add(detailItem);

            var amityItem = new ToolStripMenuItem("친목질");
            amityItem.DropDownItems.AddRange(BuildDefaultManagement(argument, typeof(AmityManagement)));
            menuItems.Add(amityItem);

            var lineItem = new ToolStripMenuItem("선넘네");
            lineItem.DropDownItems.AddRange(BuildDefaultManagement(argument, typeof(LineManagement)));
            menuItems.Add(lineItem);

            var lowWordItem = new ToolStripMenuItem("반말");
            lowWordItem.DropDownItems.AddRange(BuildDefaultManagement(argument, typeof(LowWordManagement)));
            menuItems.Add(lowWordItem);

            var badWordItem = new ToolStripMenuItem("욕설");
            badWordItem.DropDownItems.AddRange(BuildDefaultManagement(argument, typeof(BadWordManagement)));
            menuItems.Add(badWordItem);

            var sexualWordItem = new ToolStripMenuItem("음란");
            sexualWordItem.DropDownItems.AddRange(BuildDefaultManagement(argument, typeof(SexualManagement)));
            menuItems.Add(sexualWordItem);

            menuItems.Add(new TrashManagement(argument, -1));
        }
    }
}
