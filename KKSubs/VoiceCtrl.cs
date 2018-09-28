using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KKSubs
{
    public class VoiceCtrl
    {
        // string assetname, <string JP text, string ENG text>
        public static Dictionary<string, KeyValuePair<string, string>> subtitlesDict { get; internal set; }
            = new Dictionary<string, KeyValuePair<string, string>>();

        public static KeyValuePair<string, string> currentLine { get; internal set; } = new KeyValuePair<string, string>();

        public static HSceneProc hproc;

        internal static void GenerateSubtitle(LoadVoice voice)
        {
            string clipname = voice.assetName.ToLower();
            if (!subtitlesDict.TryGetValue(clipname, out KeyValuePair<string, string> subs) || subs.Key.IsNullOrEmpty())
                return;

            currentLine = new KeyValuePair<string, string>(subs.Key, subs.Value);

            var speaker = voice.voiceTrans.gameObject.GetComponentInParent<ChaControl>().chaFile.parameter.firstname;
            var outstring = $"[{clipname}] {speaker}:  {subs.Key}  {subs.Value}";
            if (KKSubsPlugin.copyToClipboard.Value)
                UnityEngine.GUIUtility.systemCopyBuffer =
                    ("[" + clipname + "]" + speaker + ":" + (KKSubsPlugin.copyJPLine.Value ? " : " + subs.Key : "") + subs.Value);

            SceneLog.WriteToFile(outstring);
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug, KKSubsPlugin.BEPNAME + outstring);

            Caption.DisplaySubtitle(voice, speaker);
        }

        internal static int BuildDictionary(string dictdata)
        {
            int cnt = 0;
            foreach (IEnumerable<string> row in ParseCSV(dictdata))
            {
                string[] cells = row.ToArray();

                if (cells[0].Length == 14 && !cells[1].IsNullOrEmpty())
                {
                    subtitlesDict[cells[0]] = new KeyValuePair<string, string>(cells[1], cells[2]);
                    cnt++;
                }
            }
            return cnt;
        }

        internal static IEnumerable<IEnumerable<string>> ParseCSV(string source)
        {
            var bodyBuilder = new StringBuilder();

            var row = new List<string>();
            bool inQuote = false;

            for (int i = 0; i < source.Length; i++)
                switch (source[i])
                {
                    case '\r': break;
                    case ',' when !inQuote:
                        row.Add(bodyBuilder.ToString());
                        bodyBuilder.Length = 0;
                        break;
                    case '\n' when !inQuote:
                        if (bodyBuilder.Length != 0 || row.Count != 0)
                        {
                            row.Add(bodyBuilder.ToString());
                            bodyBuilder.Length = 0;
                        }

                        yield return row;
                        row.Clear();
                        break;
                    case '"':
                        if (!inQuote)
                        {
                            inQuote = true;
                        }
                        else
                        {
                            if (i + 1 < source.Length && source[i + 1] == '"')
                            {
                                bodyBuilder.Append('"');
                                i++;
                            }
                            else
                            {
                                inQuote = false;
                            }
                        }

                        break;
                    default:
                        bodyBuilder.Append(source[i]);
                        break;

                }

            if (bodyBuilder.Length > 0)
                row.Add(bodyBuilder.ToString());

            if (row.Count > 0)
                yield return row;
        }

        public static void GetVoiceFromInfo()
        {
            HVoiceCtrl ctrl = Object.FindObjectOfType<HVoiceCtrl>();
            if (ctrl) GetVoiceFromInfo(ctrl);
        }

        public static void GetVoiceFromInfo(HVoiceCtrl ctrl)
        {
           hproc = ctrl.flags.gameObject.GetComponent<HSceneProc>();
            // I wish I could make this less ugly, but this is necessary
            for (int i = 0; i < ctrl.flags.lstHeroine.Count; i++)
            {
                SaveData.Heroine.HExperienceKind experience = ctrl.flags.lstHeroine[i].HExperience;
                for (int j = 0; j < 9; j++)
                {
                    /* Their HMode order is screwed up; sonyu and onani are swapped
                    if ((HFlag.EMode)(j+1) > HFlag.EMode.peeping && ctrl.flags.lstHeroine.Count < 2)
                        break;

                    if (((HFlag.EMode)(j+1) == HFlag.EMode.peeping || (HFlag.EMode)j == HFlag.EMode.masturbation)
                        && (hproc.dataH.peepCategory[0] == 0 || ctrl.flags.lstHeroine.Count > 1))
                        continue;
                        */
                    if (ctrl.dicVoiceIntos[i][j] != null && ctrl.dicVoiceIntos[i][j].Count > 0)
                        for (int k = 0; k < ctrl.dicVoiceIntos[i][j].Count; k++)
                        {
                            if (ctrl.dicVoiceIntos[i][j][k] != null && ctrl.dicVoiceIntos[i][j][k].Count > 0)
                                for (int l = 0; l < ctrl.dicVoiceIntos[i][j][k].Count; l++)
                                {
                                    var kv = ctrl.dicVoiceIntos[i][j][k].ToList<KeyValuePair<int, HVoiceCtrl.VoiceInfo>>();
                                    foreach (var kvp in kv.Where(x => x.Value != null && !subtitlesDict.ContainsKey(x.Value.nameFile) &&
                                    (SaveData.Heroine.HExperienceKind)k == experience))
                                    {
                                        subtitlesDict.Add(kvp.Value.nameFile.ToLower(), new KeyValuePair<string, string>(kvp.Value.word, ""));
                                        //  KKSubsPlugin.SPAM($"Foreach experience: {(SaveData.Heroine.HExperienceKind)k}  Heroine[{i}] experience {experience}");
                                    }
                                }
                        }
                }

            }
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, KKSubsPlugin.BEPNAME + $"subtiles {subtitlesDict.Count}");

#region update from remote
/*
            if (KKSubsPlugin.Updatemode.Value == KKSubsPlugin.UpdateMode.Scene)
            {
                if (false)
                {
                    List<string> englines = subtitlesDict.Keys.ToList();
                    // List<string> englines = SubsCache.GetSomethingRemoteAndUnsupported(subtitlesDict.Keys);
                }
            }
*/
#endregion
            // move this to SubsCache
            var dict = SubsCache.LoadFromMessagepack();
            if (dict == null) return;

 #region update cache from voiceinfo
            /*
            foreach (var nameFile in subtitlesDict.Keys)
            {
                try
                {
                    if (!dict.ContainsKey(nameFile))
                    {
                        KKSubsPlugin.SPAM($" [{nameFile}] not found in cache");
                        dict.Add(nameFile, subtitlesDict[nameFile]);
                    }
                    else if (dict.ContainsValue(subtitlesDict[nameFile]) && !(dict[nameFile]).Value.IsNullOrEmpty())
                    {
                        KKSubsPlugin.SPAM(nameFile + ": cache up to date");
                    }
                    else if ((dict[nameFile]).Key.IsNullOrEmpty() && !(subtitlesDict[nameFile]).Key.IsNullOrEmpty() ||
                            (dict[nameFile]).Value.IsNullOrEmpty() && !(subtitlesDict[nameFile]).Value.IsNullOrEmpty())
                    {
                        dict.Remove(nameFile);
                        dict.Add(nameFile, subtitlesDict[nameFile]);
                    }
                }
                catch { }
            }
            SubsCache.SaveToMessagepack(dict);
            */
#endregion

            var changes = from nameFile in subtitlesDict.Keys
                          where dict.ContainsKey(nameFile) && !dict[nameFile].Value.IsNullOrEmpty()
                          where subtitlesDict[nameFile].Value.IsNullOrEmpty()
                          select new KeyValuePair<string, string>(nameFile, dict[nameFile].Value);

            var tmp = new Dictionary<string, KeyValuePair<string, string>>();

            foreach (var change in changes)
                tmp.Add(change.Key, new KeyValuePair<string, string>(subtitlesDict[change.Key].Key, change.Value));

            subtitlesDict = tmp;
        }
    }
}
