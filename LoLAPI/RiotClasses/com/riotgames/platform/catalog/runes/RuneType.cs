using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Platform
{
    //[Serializable]
    [SerializedName("com.riotgames.platform.catalog.runes.RuneType")]
    public class RuneType
    {
        [SerializedName("runeTypeId")]
        public Int32 RuneTypeId { get; set; }

        [SerializedName("name")]
        public String Name { get; set; }
    }
}
