﻿using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.harassment.LcdsResponseString")]
    public class LcdsResponseString
    {
        [SerializedName("value")]
        public String Value { get; set; }
    }
}
