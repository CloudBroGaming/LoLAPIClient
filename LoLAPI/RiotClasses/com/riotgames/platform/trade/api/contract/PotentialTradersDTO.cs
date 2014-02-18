﻿using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.trade.api.contract.PotentialTradersDTO")]
    public class PotentialTradersDTO
    {
        [SerializedName("potentialTraders")]
        public List<String> PotentialTraders { get; set; }
    }
}
