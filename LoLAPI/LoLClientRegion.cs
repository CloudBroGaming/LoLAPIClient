using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace CloudBroGaming.LoLAPI
{
    public enum LoLClientRegion
    {
        [RiotLoginQueueAddress("https://lq.eu.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.eu.lol.riotgames.com:2099")]
        EUW,

        [RiotLoginQueueAddress("https://lq.eun1.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.eun1.lol.riotgames.com:2099")]
        EUNE,

        [RiotLoginQueueAddress("https://lq.na1.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.na1.lol.riotgames.com:2099")]
        NA,

        [RiotLoginQueueAddress("https://lq.tr.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.tr.lol.riotgames.com:2099")]
        TR,

        [RiotLoginQueueAddress("https://lq.ru.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.ru.lol.riotgames.com:2099")]
        RU,

        [RiotLoginQueueAddress("https://lq.oc1.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.oc1.lol.riotgames.com:2099")]
        OCE,

        [RiotLoginQueueAddress("https://lq.la1.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.la1.lol.riotgames.com:2099")]
        LAN,

        [RiotLoginQueueAddress("https://lq.la2.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.la2.lol.riotgames.com:2099")]
        LAS,

        [RiotLoginQueueAddress("https://lq.br.lol.riotgames.com")]
        [RiotServerAddress("rtmps://prod.br.lol.riotgames.com:2099")]
        BR
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class RiotServerAddressAttribute : Attribute
    {
        public readonly String Address;
        public RiotServerAddressAttribute(String Address)
        {
            this.Address = Address;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class RiotLoginQueueAddressAttribute : Attribute
    {
        public readonly String Address;
        public RiotLoginQueueAddressAttribute(String Address)
        {
            this.Address = Address;
        }
    }

    public static class LoLClientRegionExtensions
    {
        private static T GetAttribute<T>(Enum enumValue) where T : Attribute
        {
            T attribute;

            MemberInfo memberInfo = enumValue.GetType().GetMember(enumValue.ToString())
                                            .FirstOrDefault();

            if (memberInfo != null)
            {
                attribute = (T)memberInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault();
                return attribute;
            }
            return null;
        }

        public static String getAddress(this LoLClientRegion region)
        {
            return GetAttribute<RiotServerAddressAttribute>(region).Address;
        }

        public static String getLoginQueueAddress(this LoLClientRegion region)
        {
            return GetAttribute<RiotLoginQueueAddressAttribute>(region).Address;
        }
    }
}