﻿using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.statistics.RawStatDTO")]
    public class RawStatDTO
    {
        [SerializedName("value")]
        public Double Value { get; set; }

        [SerializedName("statTypeName")]
        public String StatTypeName { get; set; }
    }
}
