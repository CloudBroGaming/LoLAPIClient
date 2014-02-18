﻿using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.game.StartChampSelectDTO")]
    public class StartChampSelectDTO
    {
        [SerializedName("invalidPlayers")]
        public List<object> InvalidPlayers { get; set; }
    }
}
