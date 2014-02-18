using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Kudos
{
    //[Serializable]
    [SerializedName("com.riotgames.kudos.dto.PendingKudosDTO")]
    public class PendingKudosDTO
    {
        [SerializedName("pendingCounts")]
        public Int32[] PendingCounts { get; set; }
    }
}
