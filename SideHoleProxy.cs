using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using NAudio;
using NAudio.Wave;
using NAudio.Wasapi;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using System.Xml;
using System.Text.RegularExpressions;

namespace Xytio
{
    public partial class SideHole
    {
        public IWavePlayer ActiveDevice {
            get; protected set;
        }

        public static class CPH
        {
            public static Action<string> LogInfo;
            public static Action<int> Wait;
            public static Action<string,bool> SendMessage;
            public static Func<string,bool,bool> RunAction;
        }

        public void RegisterCPH_LogInfo(Action<string> action) { CPH.LogInfo -= action; CPH.LogInfo += action; }
        //public void RegisterCPH_PlaySound(Action<string> action) { CPH.PlaySound -= action; CPH.PlaySound += action; }
        public void RegisterCPH_Wait(Action<int> action) { CPH.Wait -= action; CPH.Wait += action; }
        public void RegisterCPH_SendMessage(Action<string,bool> action) { CPH.SendMessage -= action; CPH.SendMessage += action; }
        public void RegisterCPH_RunAction(Func<string,bool,bool> action) { CPH.RunAction -= action; CPH.RunAction += action; }

        internal class ChatMsg
        {
            public bool isCommand => eventSource == "command" || (!string.IsNullOrEmpty(rawInput) && rawInput.StartsWith("!"));
			
            //command variables
            public string command           ;
            public string commandId         ;
            public string commandSource     ;
            public string rawInput          ;
            public string rawInputEscaped   ;
            public string rawInputUrlEncoded  ;
            public string input0            ;
            public string inputEscaped0     ;
            public string inputUrlEncoded0  ;
            public int counter           ;
            public int userCounter       ;
            public string user              ;
            public string userName          ;
            public string userId            ;
            public string userType          ;
            public bool isSubscribed      ;
            public bool isModerator       ;
            public bool isVip             ;
            public string eventSource       ;
            public string broadcastUser     ;
            public string broadcastUserName ;
            public string broadcastUserId   ;
            public bool broadcasterIsAffiliate  ;
            public bool broadcasterIsPartner  ;

            //message only variables (isCommand == false)
            public int role;
            public string message;
            public string messageStripped;
            public bool isHighlight;
            public int bits;
            public bool isAction;
            public bool isReply;
            public bool firstMessage;
            public string apiKey;
            public string secretKey;

            public List<string> emotes;
        }

        static string GetValue(Dictionary<string, object> args, string key)
        {
            return args.ContainsKey(key) ? args[key].ToString() : "";
        }

        static T GetValue<T>(Dictionary<string, object> args, string key)
        {
            return args.ContainsKey(key) ? (T)Convert.ChangeType(args[key].ToString(),typeof(T)) : default(T);
        }

        static ChatMsg GetMsgData(Dictionary<string, object> args)
        {
            return new ChatMsg() {

                eventSource = GetValue(args, "eventSource"),

                apiKey                        = GetValue(args, "apiKey"),
                secretKey                     = GetValue(args, "secretKey"),

                command                       = GetValue(args, "command"),
                commandId                     = GetValue(args, "commandId"),
                commandSource                 = GetValue(args, "commandSource"),
                rawInput                      = GetValue(args, "rawInput"),
                rawInputEscaped               = GetValue(args, "rawInputEscaped"),
                rawInputUrlEncoded            = GetValue(args, "rawInputUrlEncoded"),
                input0                        = GetValue(args, "input0"),
                inputEscaped0                 = GetValue(args, "inputEscaped0"),
                inputUrlEncoded0              = GetValue(args, "inputUrlEncoded0"),
                counter                       = GetValue<int>(args, "counter"),
                userCounter                   = GetValue<int>(args, "userCounter"),
                user                          = GetValue(args, "user"),
                userName                      = GetValue(args, "userName"),
                userId                        = GetValue(args, "userId"),
                userType                      = GetValue(args, "userType"),
                isSubscribed                  = GetValue<bool>(args, "isSubscribed"),
                isModerator                   = GetValue<bool>(args, "isModerator"),
                isVip                         = GetValue<bool>(args, "isVip"),
                broadcastUser                 = GetValue(args, "broadcastUser"),
                broadcastUserName             = GetValue(args, "broadcastUserName"),
                broadcastUserId               = GetValue(args, "broadcastUserId"),
                broadcasterIsAffiliate        = GetValue<bool>(args, "broadcasterIsAffiliate"),
                broadcasterIsPartner          = GetValue<bool>(args, "broadcasterIsPartner"),


                role = GetValue<int>(args, "role"),
                message = GetValue(args, "message"),
                messageStripped = GetValue(args, "messageStripped"),
                isHighlight = GetValue<bool>(args, "isHighlight"),
                bits = GetValue<int>(args, "bits"),
                isAction = GetValue<bool>(args, "isAction"),
                isReply = GetValue<bool>(args, "isReply"),
                firstMessage = GetValue<bool>(args, "firstMessage")
            };
        }

        private static readonly Regex regex = new Regex(@"[^a-zA-Z0-9,.\!\? _\-]");

        public string SanitizeMessage(string msg)
        {
            string cleanMsg = regex.Replace(msg, string.Empty);
            return cleanMsg;
        }

        public string RemoveEmotes(List<string> emotes, string msg)
        {
            foreach (var e in emotes)
                msg = msg.Replace(e, string.Empty);
            return msg;
        }

        bool ttslock;

		public bool DoTTSMessage(Dictionary<string, object> args, string runActionName, int retryInterval = 2000, int numRetries = 10)
		{
            var msgData = GetMsgData(args);
            if(msgData.rawInput.Contains("!stoptts"))
            {
                try
                {
                    if (ActiveDevice != null)
                        ActiveDevice.Stop();
                    if (ActiveDevice != null)
                        ActiveDevice.Dispose();
                }
                catch(Exception)
                {

                }

                ProcessTTSQueue(null, null);
                return false;
            }
            if(msgData.rawInput.Contains("!cleartts"))
            {
                try
                {

                    if (ActiveDevice != null)
                        ActiveDevice.Stop();
                    if (ActiveDevice != null)
                        ActiveDevice.Dispose();

                }
                catch (Exception)
                {

                }

                ttsData.Running = false;
                ttsData.ttsQueue.Clear();
                ttsData.ttsVolume.Clear();

                return false;
            }
            if (msgData.rawInput.Contains("!restarttts"))
            {
                try
                {
                    if (ActiveDevice != null)
                        ActiveDevice.Stop();
                    if (ActiveDevice != null)
                        ActiveDevice.Dispose();

                }
                catch (Exception)
            {

            }

            ttsData.Running = false;
                ProcessTTSAction();

                return false;
            }

            if (args.ContainsKey("emoteCount") && (int)args["emoteCount"] > 0)
            {
                var emoteVal = (List<Twitch.Common.Models.Emote>)args["emotes"];

                msgData.emotes = emoteVal.Select(x => x.Name).ToList();
            }

            if (!string.IsNullOrEmpty(msgData.rawInput) && (msgData.rawInput.StartsWith("!")))
                return false;

            if (!string.IsNullOrEmpty(msgData.rawInput) && (msgData.rawInput.StartsWith("#")))
                return false;

            string text = SanitizeMessage(msgData.rawInput);

            if (string.IsNullOrEmpty(text))
                return false;

            if (args.ContainsKey("emoteCount") && (int)args["emoteCount"] > 0)
            {
                text = RemoveEmotes(msgData.emotes, text);

                if (string.IsNullOrEmpty(text))
                    return false;
            }

            int wordcount = text.Split(' ').Length;
            if (wordcount > 45)
                return false;

            var user = GetUser(msgData.userId);
            string voice = user.ttsData.voice;

            ttslock = true;

            // Generate UUID as per Uberduck API instructions https://app.uberduck.ai/docs/api
            string uuid = GetUUID(msgData, text, voice);

            if (string.IsNullOrEmpty(uuid))
                return false;

            string path = GetTTSDownloadPath(msgData, uuid, retryInterval, numRetries);

            TTSDownloadQueue(path, user.ttsData.volume);

            //CPH.RunAction(runActionName, true);
            
            ProcessTTSAction();

            return true;
		}

        internal string GetTTSDownloadPath(ChatMsg msgData, string uuid, int retryInterval = 2000, int numRetries = 10)
        {
            // Generate path using the above UUID
            string path = string.Empty;

            // Keep trying until API returns a path (give up after numRetries)
            for (int i = 0; i < numRetries; i++)
            {
                //CPH.LogInfo($"Attempt #{i + 1}");
                CPH.Wait(500);
                path = GenerateTTS(msgData, uuid);
                if (!string.IsNullOrWhiteSpace(path))
                    break;
            }

            return path;
        }

        internal void TTSDownloadQueue(string path, int volume)
        {
            // If we found a url type path, queue it for download!
            if (!string.IsNullOrWhiteSpace(path))
            {
                ttsData.ttsQueue.Add(path);
                ttsData.ttsVolume.Add(volume);

                //using (var client = new WebClient())
                //{
                //    // Create the TTS directory if it doesn't already exist
                //    if (!Directory.Exists(this.data.ttsDownloadTemp))
                //    {
                //        Directory.CreateDirectory(this.data.ttsDownloadTemp);
                //    }
                //    client.DownloadFile(path, $"{this.data.ttsDownloadTemp}\\tts.mp3");
                //    CPH.PlaySound($"{this.data.ttsDownloadTemp}\\tts.mp3");
                //}
            }
            else
            {
                //CPH.LogInfo("Could not generate TTS");
                //CPH.SendMessage("/me Could not generate TTS");
            }
        }

        //mark action as keep alive???
        public bool ProcessTTSAction()
        {
            if (ttsData.Running)
                return false;

            if (ttsData.ttsQueue.Count <= 0)
                return false;

            ttsData.Running = true;
            ProcessTTSQueue(null,null);
            return true;
        }

        //public void StopTTS()
        //{
        //    LoadTTS();

        //    if (!string.IsNullOrEmpty(ttsData.currentSound))
        //    {
        //        var mf = new MediaFoundationReader(ttsData.currentSound);
        //        var wo = new WasapiOut();
        //        wo.Init(mf);
        //        wo.Stop();
        //        ttsData.currentSound = string.Empty;
        //    }

        //    if (ttsData.ttsQueue.Count <= 0)
        //    {
        //        ttsData.Running = false;

        //        if (ActiveDevice != null)
        //            ActiveDevice.Stop();

        //        SaveTTS();
        //        return;
        //    }
        //    else
        //    {
        //        //TODO: see if this breaks it or it continues to work
        //        if (ActiveDevice != null)
        //            ActiveDevice.Stop();
        //    }
        //}

        public void ProcessTTSQueue(MediaFoundationReader mf_in, IWavePlayer wo_in)
        {
            try
            {
                wo_in?.Stop();
                wo_in?.Dispose();
                mf_in?.Close();
            }
            catch (Exception)
            {

            }

            //ttsData.currentSound = string.Empty;

            if (ttsData.ttsQueue.Count <= 0)
            {
                ttsData.Running = false;
                return;
            }

            string path = ttsData.ttsQueue.First();
            //ttsData.currentSound = path;
            int volume = ttsData.ttsVolume.First();
            ttsData.ttsQueue.RemoveAt(0);
            ttsData.ttsVolume.RemoveAt(0);
            int length = 0;

            var mf = new MediaFoundationReader(path);

            ttslock = false;

            try
            {
                if (ActiveDevice != null)
                {
                    if (ActiveDevice.PlaybackState == PlaybackState.Playing || ActiveDevice.PlaybackState == PlaybackState.Paused)
                    {
                        ActiveDevice.Stop();
                        ActiveDevice.Dispose();
                    }
                }

            }
            catch (Exception)
            {

            }

            try
            {
                ActiveDevice.Init(mf);
                length = mf.TotalTime.Milliseconds;
                ActiveDevice.Volume = ((float)volume / 100.0f) * data.maxVolume;
                ActiveDevice.Play();
                ActiveDevice.PlaybackStopped += (x, y) => ProcessTTSQueue(mf, ActiveDevice);


            }
            catch (Exception)
            {

            }

            //while(wo.PlaybackState == PlaybackState.Playing)
            //    CPH.Wait(1000);

        }


        internal string GetUUID(ChatMsg chatData, string message, string voice)
        {
            var url = "https://api.uberduck.ai/speak";
            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/json";
            
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(chatData.apiKey.Trim() + ":" + chatData.secretKey.Trim()));

            httpRequest.Headers["Authorization"] = $"Basic {encoded}";

            JObject data = new JObject();
            data["speech"] = message;
            data["voice"] = voice;

            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data.ToString());
            }

            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                JObject response = JObject.Parse(result);
                //CPH.LogInfo(response.Value<string>("uuid"));
                return (response.Value<string>("uuid"));
            }
        }

        internal string GenerateTTS(ChatMsg data, string uuid)
        {
            var url = $"https://api.uberduck.ai/speak-status?uuid={uuid}";

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);

            httpRequest.ContentType = "application/json";

            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(data.apiKey.Trim() + ":" + data.secretKey.Trim()));

            httpRequest.Headers["Authorization"] = $"Basic {encoded}";

            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                JObject response = JObject.Parse(result);
                return (response.Value<string>("path"));
            }
        }

    }
}
