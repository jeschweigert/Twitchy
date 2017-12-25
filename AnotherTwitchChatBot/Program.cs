﻿using ATCB.Library.Models.Twitch;
using ATCB.Library.Models.WebApi;
using Colorful;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Models.Client;

namespace ATCB
{
    class Program
    {
        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string OAuthUrl = "https://api.twitch.tv/kraken/oauth2/authorize?response_type=code&client_id=r0rcrtf3wfququa8p1nkhsttam2io1&redirect_uri=http%3A%2F%2Fbot.sandhead.stream%2Fapi%2Fclient_auth.php&scope=channel_check_subscription+channel_commercial+channel_editor+channel_feed_edit+channel_feed_read+channel_read+channel_stream+channel_subscriptions+chat_login+collections_edit+communities_edit+communities_moderate+openid+user_blocks_edit+user_blocks_read+user_follows_edit+user_read+user_subscriptions+viewing_activity_read";
        
        private static WebAuthenticator Authenticator;
        private static TwitchChatBot ChatBot;
        
        private static Guid AppState;

        static void Main(string[] args)
        {
            Authenticator = new WebAuthenticator();
            if (!File.Exists($"{AppDirectory}setupcomplete.txt"))
            {
                FirstTimeSetup();
            }
            
            // TODO: create json/xml settings file instead of this
            var FileContents = File.ReadAllText($"{AppDirectory}setupcomplete.txt");
            AppState = Guid.Parse(FileContents);

            Colorful.Console.WriteLine("Grabbing credentials from database...");
            ChatBot = new TwitchChatBot(Authenticator, AppState);
            Colorful.Console.WriteLine("Connecting to Twitch...");
            ChatBot.Start();

            object locker = new object();
            List<char> charBuffer = new List<char>();

            while (ChatBot.IsConnected) {
                var key = Colorful.Console.ReadKey();
                if (key.Key == ConsoleKey.Enter && charBuffer.Count > 0)
                {
                    StyleSheet styleSheet = new StyleSheet(Color.White);
                    styleSheet.AddStyle("Console", Color.Gray);
                    var sentMessage = new string(charBuffer.ToArray());

                    Colorful.Console.WriteLineStyled($"[{DateTime.Now.ToString("T")}] Console: {sentMessage}", styleSheet);
                    ChatBot.PerformConsoleCommand(sentMessage);
                    charBuffer.Clear();
                }
                else if (key.Key == ConsoleKey.Backspace && charBuffer.Count > 0)
                {
                    charBuffer.RemoveAt(charBuffer.Count - 1);
                }
                else if (char.IsLetterOrDigit(key.KeyChar))
                {
                    charBuffer.Add(key.KeyChar);
                }
            }

            Colorful.Console.WriteLine("Press any key to exit...");
            Colorful.Console.ReadKey(true);
        }

        private static void FirstTimeSetup()
        {
            var HasNotAuthenticated = true;
            AppState = Guid.NewGuid();

            Colorful.Console.WriteLine("Hi there! It looks like you're starting ATCB for the first time.");
            Colorful.Console.WriteLine("Just so you know, I'm going to need some permissions from your Twitch account to run correctly.");
            while (HasNotAuthenticated)
            {
                Colorful.Console.WriteLine("I'll open up the authentication page in your default browser, press any key once you've successfully authenticated.");
                Thread.Sleep(5000);
                System.Diagnostics.Process.Start($"{OAuthUrl}&state={AppState.ToString()}");
                Colorful.Console.ReadKey(true);
                Colorful.Console.WriteLine("Checking for authentication...");
                try
                {
                    var AccessToken = Authenticator.GetAccessTokenByStateAsync(AppState).Result;
                    HasNotAuthenticated = false;
                }
                catch (Exception)
                {
                    Colorful.Console.WriteLine("Authentication failed, uh oh. Let's send you back to Twitch's authentication page and try again!");
                }
            }
            Colorful.Console.WriteLine("Neat-o! We've hooked ourselves an access token! Look's like you're all good to go!");
            File.WriteAllText($"{AppDirectory}setupcomplete.txt", AppState.ToString());
        }
    }
}
