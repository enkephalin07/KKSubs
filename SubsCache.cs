using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using MessagePack;
using UnityEngine;

namespace HSubs
{
    public partial class HSubsPlugin
    {
        public bool UpdateSubs()
        {
            if (Updatemode.Value == UpdateMode.None && File.Exists(fileCache))
            {
                subtitlesDict = LZ4MessagePackSerializer.Deserialize<Dictionary<string, KeyValuePair<string, string>>>(File.ReadAllBytes(fileCache));
                BepInEx.Logger.Log(LogLevel.Info, BEPNAME + subtitlesDict.Count + " lines parsed in " + fileCache);
                subtitlesDict = new Dictionary<string, KeyValuePair<string, string>>();
            }
            else
            {
                BepInEx.Logger.Log(LogLevel.Info, BEPNAME + ((File.Exists(fileCache) ? "" : fileCache + " not found. ") + "Updating subs..."));
                Instance.StartCoroutine(DownloadSubs());
            }
            return true;
        }

        private IEnumerator DownloadSubs()
        {
            BepInEx.Logger.Log(LogLevel.Info, BEPNAME + "Downloading subs from " + SSURL + SHEET_KEY);
            // + $"export?exportFormat=csv&gid={GID}&range={VOICEID}");
            var dl = new WWW(SSURL + SHEET_KEY + "/export?exportFormat=csv&gid=" + GID + "&range=" + RANGE);
            while (!dl.isDone)
            {
                SPAM($"DownloadSubs(): {dl.url} : {dl.progress}");
                yield return new WaitForSeconds(30); // dl;
            }
            SPAM("DownloadSubs(): Complete");

            if (dl.error != null)
            {
                BepInEx.Logger.Log(LogLevel.Warning, BEPNAME + "Failed to fetch latest subtitles. Going to use cached ones.");
                yield break;
            }

            BepInEx.Logger.Log(LogLevel.Info, BEPNAME + "Downloaded " + dl.bytesDownloaded + " bytes. Parsing...");

            int cnt = 0;
            foreach (IEnumerable<string> row in ParseCSV(dl.text))
            {
                string[] cells = row.ToArray();

                if (cells[0].Length == 14 && !cells[1].IsNullOrEmpty())
                {
                    subtitlesDict[cells[0]] = new KeyValuePair<string, string>(cells[1], cells[2]);
                    cnt++;
                }
            }

            BepInEx.Logger.Log(LogLevel.Info, BEPNAME + "Done parsing subtitles: " + cnt + " lines found.");
            if (cnt > 60000)
                File.WriteAllBytes(fileCache, LZ4MessagePackSerializer.Serialize(subtitlesDict));
            else
                BepInEx.Logger.Log(LogLevel.Warning, BEPNAME + "The amount of lines is suspiciously low (defaced sheet?); not caching.");
        }

        public void OnDestroy() { Hooks.DetachPatch(); }

    }
}
