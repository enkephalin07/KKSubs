//#define INTERNALHOOKS
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

//            currentLine = new KeyValuePair<string, string>(subs.Key, subs.Value);

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

            for (int i = 0; i < ctrl.flags.lstHeroine.Count; i++)
            {
                SaveData.Heroine.HExperienceKind experience = ctrl.flags.lstHeroine[i].HExperience;
                for (int j = 0; j < 9; j++)
                {
                    if ((HFlag.EMode)j > HFlag.EMode.peeping && ctrl.flags.lstHeroine.Count < 2)
                        break;

                    if (((HFlag.EMode)j == HFlag.EMode.peeping || (HFlag.EMode)j == HFlag.EMode.masturbation)
                        && (hproc.dataH.peepCategory[0] == 0 || ctrl.flags.lstHeroine.Count > 1))
                        continue;

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

            if (KKSubsPlugin.Updatemode.Value != KKSubsPlugin.UpdateMode.Scene)
            {
                var dict = SubsCache.LoadFromMessagepack();
                if (dict == null) return;

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
            }
            else
            {
                if (false)
                {
                    List<string> englines = subtitlesDict.Keys.ToList();
                    // List<string> englines = SubsCache.GetSomethingRemoteAndUnsupported(subtitlesDict.Keys);
                }
            }
        }
    }

#if INTERNALHOOKS
        public static class Hooks
        {
            public static HarmonyInstance harmony { get; internal set; }

            public static void InstallHooks(string id)
            {
                harmony = HarmonyInstance.Create("org.bepinex.kk.hsubs.voicectrl");
                harmony.PatchAll(typeof(Hooks));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HVoiceCtrl), "Init", new Type[] { typeof(string), typeof(string), typeof(string) })]
            public static void LoadHVoices(string _chaFolder, string _chaFolder1, string _pathAssetFolder, HVoiceCtrl __instance)
            {
                InitScene(__instance);

                __instance.OnDestroyAsObservable().Subscribe(delegate (Unit _)
                    { CleanupScene(__instance); });
            }

            [HarmonyPatch(typeof(ActionGame.MotionVoice), "Init", new Type[] { typeof(SaveData.Heroine), })]
            public static void LoadMotionVoices(SaveData.Heroine _heroine, HVoiceCtrl __instance)
            {

            }

            public static void DetachPatch()
            {
                foreach (var method in harmony.GetPatchedMethods())
                    harmony.RemovePatch(method, HarmonyPatchType.All, GUID);
            }

        }
#endif
}
//                                            KKSubsPlugin.SPAM(kvp.Value.pathAsset + " : " + kvp.Value.word);                                        }
/*                                        if (ctrl.dicVoiceIntos[i][j][k][l] != null && !ctrl.dicVoiceIntos[i][j][k][l].pathAsset.IsNullOrEmpty())
                                        {
                                            var vi = ctrl.dicVoiceIntos[i][j][k][l];
                                            vinfos.Add(ctrl.dicVoiceIntos[i][j][k][l]);
                                            KKSubsPlugin.SPAM(ctrl.dicVoiceIntos[i][j][k][l].pathAsset + " : " + ctrl.dicVoiceIntos[i][j][k][l].word);
                                        }
  */
/*
public static void AddToDictionary(string chaFolder, int mode, string pathAssetFolder, int main)
{
    string chafile = $"personality_voice_{chaFolder}_{mode.ToString("00")}_00";
    KKSubsPlugin.SPAM("chafile:"+chafile);

    string text = GlobalMethod.LoadAllListText(pathAssetFolder, chafile);
    if (text.IsNullOrEmpty()) return;

//            string[] rows = text.Trim().Split(new string[] { "\n", " " }, System.StringSplitOptions.RemoveEmptyEntries);

    string[,] arr;
    GlobalMethod.GetListString(text, out arr);
    arr.Copy(arr, 1, arr.Length)
;//            arr.GetLength(0)
    for (int i = 0; i < arr.GetLength(0); i++)
    {
        KKSubsPlugin.SPAM(arr[i,0]);
        for (int j = 0; j < arr.GetLength(1); j++)
        {
//                    KKSubsPlugin.SPAM(arr[i,j]);
        }
    }
}
*/
