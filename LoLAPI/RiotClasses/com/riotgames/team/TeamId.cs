using System;
using RtmpSharp.IO;

namespace CloudBroGaming.LoLAPI.Riot.Team
{
    //[Serializable]
    [SerializedName("com.riotgames.team.TeamId")]
    public class TeamId
    {
        [SerializedName("fullId")]
        public String FullId { get; set; }

        public static implicit operator TeamId(String s)
        {
            return new TeamId { FullId = s };
        }
    }
}
