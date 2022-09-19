using System;
using System.Linq;
using System.Collections.Generic;

namespace Xytio
{
    public partial class SideHole
    {
        //Don't leave this as TEMP....
        public static string DatabaseFile = @"c:\TEMP\yourFilePathHere.xml";
        public static string TTSFile = @"c:\TEMP\yourTTSPathHere.xml";
        public static string DefaultVoice = "zwf";

        public SideHoleData data;
        public TTSQueueData ttsData;

        static SideHole instance;
        public static SideHole Instance {
            get {
                if (instance == null)
                {
                    instance = new SideHole();
                    instance.Setup();
                }
                return instance;
            }
        }

        /// <summary>
        /// Establish a new sidehole user
        /// </summary>
        public UserData AddUser(string tID)
        {
            if (HasUser(tID))
                return GetUser(tID);

            data.users.Add(new UserData() { 
                twitchID = tID, 
                ttsData = new TTSData() {  voice = DefaultVoice },
                betHistory = new BetHistory() { previousBetOutcomes = new List<bool>() } }

            );
            Save();
            return GetUser(tID);
        }

        public void SaveUser(UserData user)
        {
            if (HasUser(user.twitchID))
            {
                int index = data.users.FindIndex(x => x.twitchID == user.twitchID);
                data.users[index] = user;
            }
        }

        /// <summary>
        /// Checks to see if this user exists in the database (will not add a user if id is missing)
        /// </summary>
        public bool HasUser(string tID)
        {
            return (data.users.Any(x => x.twitchID.Equals(tID)));
        }

        /// <summary>
        /// Will add/create a user if the twitch id is not in the database
        /// </summary>
        public UserData GetUser(string tID)
        {
            var foundUser = data.users.FirstOrDefault(x => x.twitchID.Equals(tID));
            if (foundUser == null)
                foundUser = AddUser(tID);
            return foundUser;
        }

        /// <summary>
        /// Warning: is not reversable, will clear all stats for the user
        /// </summary>
        public bool RemoveUser(string tID)
        {
            var foundUser = GetUser(tID);
            bool result = data.users.Remove(foundUser);
            Save();
            return result;
        }

        /// <summary>
        /// Add (or subtract) points from a user.
        /// </summary>
        public void AddPoints(string tID, int points)
        {
            var foundUser = GetUser(tID);
            foundUser.points += points;
            if (foundUser.points < 0)
                foundUser.points = 0;
            Save();
        }

        /// <summary>
        /// Set the user's points to this value.
        /// </summary>
        public void SetPoints(string tID, int points)
        {
            var foundUser = GetUser(tID);
            foundUser.points = points;
            if (foundUser.points < 0)
                foundUser.points = 0;
            Save();
        }

        /// <summary>
        /// Get the user's points
        /// </summary>
        public int GetPoints(string tID)
        {
            var foundUser = GetUser(tID);
            return foundUser.points;
        }

        /// <summary>
        /// Check if a user has enough points
        /// </summary>
        public bool HasPoints(string tID, int points)
        {
            var foundUser = GetUser(tID);
            return foundUser.points >= points;
        }

        /// <summary>
        /// Register a bet for this user. Points bet are optional. If already betting will add any points sent in to the current bet.
        /// </summary>
        public bool AddBet(string tID, int betIndex, int? pointsBet = null)
        {
            var foundUser = GetUser(tID);

            int betValue = pointsBet.HasValue ? pointsBet.Value : 0;
            if (betValue > 0)
            {
                if (!HasPoints(tID, betValue))
                    return false;

                AddPoints(tID, -betValue);
            }

            if (foundUser.CurrentBet == null)
            {
                foundUser.CurrentBet = new BetData() { betIndex = betIndex, betValue = betValue };
            }
            else
            {
                foundUser.CurrentBet.betIndex = betIndex;
                foundUser.CurrentBet.betValue += pointsBet;
            }

            return true;
        }

        public void SetVoice(Dictionary<string, object> args)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            var voice = GetValue(args, "voice");

            if (string.IsNullOrEmpty(voice))
                voice = GetValue(args, "input0");

            Load();
            user.ttsData.voice = voice;
            SaveUser(user);
            Save();
        }

        public void SetVolume(Dictionary<string, object> args)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            var strvol = GetValue(args, "volume");

            if (string.IsNullOrEmpty(strvol))
                strvol = GetValue(args, "input0");

            try
            {
                int vol = int.Parse(strvol);

                Load();
                user.ttsData.volume = vol;
                SaveUser(user);
                Save();
            }
            catch (Exception)
            {

            }
        }

        public void SetMaxVolume(Dictionary<string, object> args)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            var strvol = GetValue(args, "maxvolume");

            if (string.IsNullOrEmpty(strvol))
                strvol = GetValue(args, "input0");

            try
            {
                float vol = float.Parse(strvol);

                Load();
                data.maxVolume = vol;
                SaveUser(user);
                Save();
            }
            catch(Exception)
            {

            }
        }

        public void ShowVoiceConfigLink()
        {
           CPH.SendMessage(@"To view possible voice choices visit: https://xytio.com/tts/voiceconfig.html", false);
        }

        public void SetDefaultVoice(string voice)
        {
            SideHole.DefaultVoice = voice;
        }

        //public void ClearTTS()
        //{
        //    LoadTTS();
        //    ttsData.Running = false;
        //    if (ttsData.ttsQueue.Count <= 0)
        //    {
        //        SaveTTS();
        //        return;
        //    }
        //    else
        //    {
        //        ttsData.ttsQueue.Clear();
        //        ttsData.ttsVolume.Clear();

        //        //StopTTS();

        //        ttsData.Running = false;
        //        SaveTTS();
        //    }
        //}


        //public void RestartTTS()
        //{
        //    LoadTTS();
        //    ttsData.Running = false;
        //    SaveTTS();
        //    ProcessTTSAction();
        //}

        //TODO:
        public void SetPitch(Dictionary<string, object> args)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            var strvol = GetValue(args, "maxvolume");

            if (string.IsNullOrEmpty(strvol))
                strvol = GetValue(args, "input0");

            try
            {
                int vol = int.Parse(strvol);

                Load();
                user.ttsData.pitch = vol;
                SaveUser(user);
                Save();
            }
            catch (Exception)
            {

            }
        }

        //TODO:
        public void SetPace(Dictionary<string, object> args)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            var strvol = GetValue(args, "maxvolume");

            if (string.IsNullOrEmpty(strvol))
                strvol = GetValue(args, "input0");

            try
            {
                float vol = float.Parse(strvol);

                Load();
                SaveUser(user);
                Save();
            }
            catch (Exception)
            {

            }
        }

        public void SetVoice(Dictionary<string, object> args, string voice)
        {
            var msgData = GetMsgData(args);
            var user = GetUser(msgData.userId);
            Load();
            user.ttsData.voice = voice;
            SaveUser(user);
            Save();
        }

        /// <summary>
        /// Sets the user's tts voice
        /// </summary>
        public void SetVoice(string tID, string voice)
        {
            var foundUser = GetUser(tID);

            Load();
            foundUser.ttsData.voice = voice;
            SaveUser(foundUser);
            Save();
        }

        /// <summary>
        /// Get the user's bet, if it exists
        /// </summary>
        public BetData GetBet(string tID)
        {
            var foundUser = GetUser(tID);
            return foundUser.CurrentBet;
        }

        /// <summary>
        /// Clear a user's bet
        /// </summary>
        public void ClearBet(string tID, bool save = true)
        {
            var foundUser = GetUser(tID);
            foundUser.CurrentBet = null;
            if (save)
                Save();
        }

        /// <summary>
        /// Clear the bets for all users
        /// </summary>
        public void ClearAllBets(bool save = true)
        {
            data.users.ForEach(x => ClearBet(x.twitchID, save));
            Save();
        }

        /// <summary>
        /// Mark bets as allowed (does nothing. Simply a persistant bool that can be used by you on the twitch frontend)
        /// </summary>
        public void EnableBets()
        {
            data.CanBet = true;
            Save();
        }

        /// <summary>
        /// Mark bets as disallowed (does nothing. Simply a persistant bool that can be used by you on the twitch frontend)
        /// </summary>
        public void DisableBets()
        {
            data.CanBet = false;
            Save();
        }

        /// <summary>
        /// Resolve the bet for a user by providing the winning index. Will update bet history, streaks, etc.
        /// </summary>
        public void ResolveBet(string tID, int winningIndex, bool save = true)
        {
            var foundUser = GetUser(tID);
            if (foundUser.CurrentBet == null)
                return;

            bool result = foundUser.CurrentBet.betIndex == winningIndex;
            if (result)
            {
                if (foundUser.CurrentBet.betValue.HasValue && foundUser.CurrentBet.betValue > 0)
                {
                    foundUser.points += foundUser.CurrentBet.betValue.Value;
                    foundUser.betHistory.totalPointsWon += foundUser.CurrentBet.betValue.Value;
                }
            }
            else
            {
                if (foundUser.CurrentBet.betValue.HasValue && foundUser.CurrentBet.betValue > 0)
                {
                    foundUser.betHistory.totalPointsLost += foundUser.CurrentBet.betValue.Value;
                }
            }

            if (foundUser.betHistory.correctPreviousBet == result)
            {
                if (result)
                {
                    foundUser.betHistory.currentStreak++;

                    if (foundUser.betHistory.currentStreak > foundUser.betHistory.maxStreak)
                        foundUser.betHistory.maxStreak = foundUser.betHistory.currentStreak;
                }
                else
                {
                    foundUser.betHistory.currentStreak--;

                    if (foundUser.betHistory.currentStreak < foundUser.betHistory.minStreak)
                        foundUser.betHistory.minStreak = foundUser.betHistory.currentStreak;
                }
            }
            else
            {
                foundUser.betHistory.currentStreak = 0;
            }

            foundUser.betHistory.previousBetOutcomes.Add(result);
            foundUser.betHistory.correctPreviousBet = result;

            ClearBet(tID, false);

            if (save)
                Save();
        }

        /// <summary>
        /// Resolve the bets for all users by providing the winning index
        /// </summary>
        public void ResolveBets(int winningIndex)
        {
            DisableBets();
            data.users.ForEach(x => ResolveBet(x.twitchID, winningIndex, false));
            Save();
        }

        public BetHistory GetBetHistory(string tID)
        {
            return GetUser(tID).betHistory;
        }

        public List<bool> GetPreviousBets(string tID)
        {
            return GetBetHistory(tID).previousBetOutcomes;
        }

        public float GetPersonalBetRate(string tID)
        {
            var outcomes = GetPreviousBets(tID);
            float good = outcomes.Where(x => x == true).Count();
            return good / (float)(outcomes.Count);
        }

        public int GetTotalPointsWon(string tID)
        {
            return GetBetHistory(tID).totalPointsWon;
        }

        public int GetTotalPointsLost(string tID)
        {
            return GetBetHistory(tID).totalPointsLost;
        }

        public int GetCurrentStreak(string tID)
        {
            return GetBetHistory(tID).currentStreak;
        }

        public int GetMaxStreak(string tID)
        {
            return GetBetHistory(tID).maxStreak;
        }

        public int GetMinStreak(string tID)
        {
            return GetBetHistory(tID).minStreak;
        }

        public bool GetPreviousBetResult(string tID)
        {
            return GetBetHistory(tID).correctPreviousBet;
        }

        SideHole()
        {
            //force users to use Instance
        }

        void Setup()
        {
            Load();
            LoadTTS();
        }

        public void Save()
        {
            XMLUtils.WriteDataToFile(DatabaseFile, data);
        }

        public void Load()
        {
            if(ActiveDevice == null)
            {
                ActiveDevice = new NAudio.Wave.WaveOutEvent();
            }


            if(!System.IO.File.Exists(DatabaseFile))
            {
                PostLoad_Init();
                Save();
            }
            XMLUtils.ReadDataFromFile<SideHoleData>(DatabaseFile, out data);
            PostLoad_Init();
        }

        public void SaveTTS()
        {
            XMLUtils.WriteDataToFile(TTSFile, ttsData);
        }

        public void LoadTTS()
        {
            XMLUtils.ReadDataFromFile<TTSQueueData>(TTSFile, out ttsData);
            PostLoad_Init();
        }

        void PostLoad_Init()
        {
            if (data == null)
                data = new SideHoleData();

            if (data.users == null)
                data.users = new List<UserData>();

            if (ttsData == null)
                ttsData = new TTSQueueData();

            if (ttsData.ttsQueue == null)
                ttsData.ttsQueue = new List<string>();

            if (ttsData.ttsVolume == null)
                ttsData.ttsVolume = new List<int>();
        }
    }


}