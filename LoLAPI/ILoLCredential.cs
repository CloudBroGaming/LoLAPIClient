using System;
using System.Runtime.CompilerServices;

namespace CloudBroGaming.LoLAPI
{
    public interface ILoLCredential
    {
        LoLClientRegion Region { get; set; }
        String Username { get; set; }
        String Password { get; set; }
    }
}