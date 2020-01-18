using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Twitch___AdiIRC.TwitchApi;

namespace APITester
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var how = TwitchApiTools.GetFollowLong("lilac_unicorn_", "leekcake");
            Assert.AreEqual(127, how);
        }
    }
}
