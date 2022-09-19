using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace Xytio
{
    [XmlRoot("TTSQueueData")]
    public class TTSQueueData
    {
        [XmlElement("Running")]
        public bool Running;

        [XmlArray("TTSQueue")]
        public List<string> ttsQueue;

        [XmlArray("TTSVolume")]
        public List<int> ttsVolume;
    }

    [XmlRoot("SideHoleData")]
    public class SideHoleData
    {
        [XmlArray("Users")]
        public List<UserData> users;

        [XmlElement("CanBet")]
        public bool CanBet;

        [XmlElement("TTSDownloadTEMP")]
        public string ttsDownloadTemp;

        [XmlElement("MaxVolume")]
        public float maxVolume = 1f;
    }

    [XmlRoot("UserData")]
    public class UserData
    {
        [XmlElement("TwitchID")]
        public string twitchID;

        [XmlElement("Points")]
        public int points;

        [XmlElement("TTSData")]
        public TTSData ttsData;

        [XmlElement(ElementName = "CurrentBet", IsNullable = true)]
        public BetData CurrentBet;

        [XmlElement("BetHistory")]
        public BetHistory betHistory;
    }

    [XmlRoot("BetData")]
    public class BetData
    {
        [XmlElement("BetIndex")]
        public int betIndex;

        [XmlElement(ElementName = "betValue", IsNullable = true)]
        public int? betValue;
    }

    [XmlRoot("TTSData")]
    public class TTSData
    {
        [XmlElement("Voice")]
        public string voice;

        [XmlElement("Volume")]
        public int volume = 100;

        [XmlElement("Pitch")]
        public int pitch = 1;
    }

    [XmlRoot("BetHistory")]
    public class BetHistory
    {
        [XmlElement("CorrectPreviousBet")]
        public bool correctPreviousBet;

        [XmlElement("CurrentStreak")]
        public int currentStreak;

        [XmlElement("MaxStreak")]
        public int maxStreak;

        [XmlElement("MinStreak")]
        public int minStreak;

        [XmlElement("TotalPointsWon")]
        public int totalPointsWon;

        [XmlElement("TotalPointsLost")]
        public int totalPointsLost;

        [XmlArray("PreviousBetOutcomes")]
        public List<bool> previousBetOutcomes;
    }
}
