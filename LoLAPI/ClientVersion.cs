using CloudBroGaming.LoLAPI.SWF;
using CloudBroGaming.LoLAPI.SWF.SWFTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace CloudBroGaming.LoLAPI
{
    public static class ClientVersion
    {
        private const String ReleaseListing = "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_{0}";
        private const String LibCommon = "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/{0}/files/lib/ClientLibCommon.dat";
        private static Dictionary<LoLClientRegion, String> LastKnownVersions = new Dictionary<LoLClientRegion, String>();
        private static Dictionary<LoLClientRegion, DateTime> LastCheckedTimes = new Dictionary<LoLClientRegion, DateTime>();


        public static String Get(LoLClientRegion region, bool force = false)
        {
            if (!LastCheckedTimes.ContainsKey(region))
            {
                LastKnownVersions.Add(region, "3.15.14_01_08_11_31");
                LastCheckedTimes.Add(region, DateTime.UtcNow.AddDays(-1));
            }
            if (DateTime.UtcNow > LastCheckedTimes[region].AddHours(5) || force)
            {
                using (var wc = new WebClient())
                {
                    String[] versions = wc.DownloadString(String.Format(ReleaseListing, region.ToString())).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    String tmpFile = Path.GetTempFileName();
                    for (int i = 0; i < (int)versions.Length; i++)
                    {
                        try
                        {
                            wc.DownloadFile(String.Format(LibCommon, versions[i]), tmpFile);
                            break;
                        }
                        catch (WebException webException)
                        {
                            WebException e = webException;
                            if ((e.Status != WebExceptionStatus.ProtocolError ? true : ((HttpWebResponse)e.Response).StatusCode != HttpStatusCode.NotFound))
                            {
                                throw e;
                            }
                        }
                    }
                    String version = GetVersion(tmpFile);
                    LastCheckedTimes[region] = DateTime.UtcNow;
                    LastKnownVersions[region] = version;
                    File.Delete(tmpFile);
                    return version;
                }
            }
            else
            {
                return LastKnownVersions[region];
            }
        }

        private static String GetVersion(String filename)
        {
            SWFReader reader = new SWFReader(filename);
            foreach (Tag tag in reader.Tags)
            {
                if (tag is DoABC)
                {
                    DoABC abcTag = (DoABC)tag;
                    if (abcTag.Name.Contains("riotgames/platform/gameclient/application/Version"))
                    {
                        var str = System.Text.Encoding.Default.GetString(abcTag.ABCData);
                        string[] firstSplit = str.Split((char)6);
                        //string[] secondSplit = firstSplit[0].Split((char)19);
                        return firstSplit[0].Substring(firstSplit[0].IndexOf("CURRENT_VERSION") + 17); //17 for CURRENT_VERSION + 2 hidden characters
                    }
                }
            }
            return "3.15.14_01_08_11_31";
        }
    }
}