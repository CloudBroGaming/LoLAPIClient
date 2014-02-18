﻿using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.matchmaking.MatchingThrottleConfig")]
    public class MatchingThrottleConfig
    {
        [SerializedName("limit")]
        public Double Limit { get; set; }

        [SerializedName("matchingThrottleProperties")]
        public List<object> MatchingThrottleProperties { get; set; }

        [SerializedName("cacheName")]
        public String CacheName { get; set; }
    }
}
