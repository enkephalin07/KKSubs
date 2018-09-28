//#define INTERNALHOOKS
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace KKSubs
{
    public class VoiceCtrl
    {
        // string assetname, <string JP text, string ENG text>
        public static Dictionary<string, KeyValuePair<string, string>> subtitlesDict { get; internal set; }
            = new Dictionary<string, KeyValuePair<string, string>>();
        public static KeyValuePair<string, string> currentLine { get; internal set; } = new KeyValuePair<string, string>();

        internal static void GenerateSubtitle(LoadVoice voice)
        {
            string clipname = voice.assetName.ToLower();
            if (!subtitlesDict.TryGetValue(clipname, out KeyValuePair<string, string> subs) || subs.Key.IsNullOrEmpty())
                return;

            currentLine = new KeyValuePair<string, string>(subs.Key, subs.Value);

            var speaker = voice.voiceTrans.gameObject.GetComponentInParent<ChaControl>().chaFile.parameter.firstname;
            var outstring = $"[{clipname}] {speaker}:  {currentLine.Key}  {currentLine.Value}";
            if (KKSubs.copyToClipboard.Value)
                UnityEngine.GUIUtility.systemCopyBuffer =
                    ("[" + clipname + "]" + speaker + ":" + (KKSubs.copyJPLine.Value ? " : " + currentLine.Key : "") + currentLine.Value);

            SceneLog.WriteToFile(outstring);
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug, KKSubs.BEPNAME + outstring);

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

        public static void InitScene(string chaFolder, string chaFolder1, string pathAssetFolder)
        {
            chaFolder = $"personality_voice_{chaFolder}_";
            chaFolder1 = chaFolder1.IsNullOrEmpty() ? "" : $"personality_voice_{chaFolder1}_";
//            KKSubs.SPAM(chaFolder + " " + chaFolder1);

            List<string> paths = CommonLib.GetAssetBundleNameListFromPath("h/list/");
            paths.Sort();
            foreach (var path in paths)
            {
//                KKSubs.SPAM(path);
                foreach (var contents in AssetBundleCheck.GetAllAssetName(path)
                    .Where(x => x.StartsWith(chaFolder) || (!chaFolder1.IsNullOrEmpty() && x.StartsWith(chaFolder1))))
                {
                    KKSubs.SPAM(contents);
                    List<string> texts = new List<string>();
                    for (int i = 0; i < 9; i++)
                     texts.Add(GlobalMethod.LoadAllListText(path, "contents"+ i.ToString("00")+"_00"));


//                        KKSubs.SPAM(contents);
                }
            }
 /*           var filenames = from string path in paths
                            from string[] files in AssetBundleCheck.GetAllAssetName(path, false, null, false)
                            from string file in files
                            where !file.IsNullOrEmpty() && (file.StartsWith("personality_voice_" + chaFolder)
                                || (!chaFolder1.IsNullOrEmpty() && file.StartsWith("personality_voice_" + chaFolder1)))
                            select file; // CommonLib.LoadAsset<UnityEngine.TextAsset>(path, file, false, string.Empty);
            KKSubs.SPAM("contenst");

            foreach (var file in filenames)
                    KKSubs.SPAM(file);
            KKSubs.SPAM("contenst");

/*            var rows = from text in filenames
                        where text != null
                        from row in text.text.Trim().Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                        where !row.IsNullOrEmpty() 
                            && (row.Split(new char[] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries)).Length > 1
                        select row;

            foreach (var row in rows)
                KKSubs.SPAM($"{row}, {row.Length}");

            //             List<string> voices = new List<string>();
            foreach (string row in rows)
            {
                string[] cells = row.Split(new char[] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                KKSubs.SPAM(row);
            }
            //                lines.Add(cells[2], new KeyValuePair<string, string>(cells[3], ""));
*/            foreach (var path in paths)
                AssetBundleManager.UnloadAssetBundle(path, true, null, false);

        }

        public static void InitScene(HVoiceCtrl ctrl)
        {
            IEnumerable<string> assets = from int heroine in ctrl.dicVoiceIntos
                                         from mode in ctrl.dicVoiceIntos[heroine].Cast<int>()
                                         from act in ctrl.dicVoiceIntos[heroine][mode].Cast<int>()
                                         from vi in ctrl.dicVoiceIntos[heroine][mode][act].Cast<HVoiceCtrl.VoiceInfo>()
                                         select vi.pathAsset;
            List<string> files = assets.ToList<string>();
            foreach (KeyValuePair<string, KeyValuePair<string, string>> line2 in SubsCache.LoadFromMessagepack("").Where(delegate (KeyValuePair<string, KeyValuePair<string, string>> line)
            {
                List<string> files = files;
                KeyValuePair<string, KeyValuePair<string, string>> keyValuePair = line;
                return files.Contains(keyValuePair.Key);
            }))
            {
                VoiceCtrl.subtitlesDict.Add(line2.Key, line2.Value);
            }
            SceneLog.InitSceneFile(ctrl.flags.lstHeroine);
            Caption.InitGUI();
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
/*
 * 
*/