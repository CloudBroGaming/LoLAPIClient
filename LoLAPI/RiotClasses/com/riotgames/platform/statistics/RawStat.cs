﻿using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.statistics.RawStat")]
    public class RawStat
    {
        [SerializedName("statType")]
        public String StatType { get; set; }

        [SerializedName("value")]
        public Double Value { get; set; }
    }
}
