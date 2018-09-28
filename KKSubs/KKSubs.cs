using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using Harmony;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace KKSubs
{
    [BepInPlugin(GUID: "org.bepinex.kk.KKSubs", Name: "KKSubs", Version: "0.8.5")]
    public class KKSubsPlugin : BaseUnityPlugin
    {
        public const string GUID = "org.bepinex.kk.KKSubs";
        public const string BEPNAME = "[KKSubs] ";

        public static KKSubsPlugin Plugin { get; private set; }
        public static HarmonyInstance harmony { get; private set; }


        #region ConfigMgr
        public static SavedKeyboardShortcut ReloadTrans { get; private set; }
        public static SavedKeyboardShortcut CBCopy { get; private set; }
        public static SavedKeyboardShortcut SceneLogging { get; private set; }

        [DisplayName("Font")]
        [Category("Caption Text")]
        [Browsable(false)]
        public static ConfigWrapper<string> fontName { get; private set; }
        [DisplayName("Size")]
        [Category("Caption Text")]
        [Description("Positive values in px, negative values in % of screen size")]
        [AcceptableValueRange(-100, 300, false)]
        public static ConfigWrapper<int> fontSize { get; private set; }
        [DisplayName("Style")]
        [Category("Caption Text")]
        [Description("Most available fonts are dynamic, but non-dynamic fonts only support Normal style.")]
        public static ConfigWrapper<FontStyle> fontStyle { get; private set; }
        [DisplayName("Alignment")]
        [Category("Caption Text")]
        public static ConfigWrapper<TextAnchor> textAlign { get; private set; }
        [DisplayName("Text Offset")]
        [Category("Caption Text")]
        [Description("Padding from bottom of screen")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<int> textOffset { get; private set; }
        [DisplayName("Outline Thickness")]
        [Category("Caption Text")]
        [AcceptableValueRange(0, 100, false)]
        public static ConfigWrapper<int> outlineThickness { get; private set; }

        [DisplayName("Paste to Clipboard")]
        [Description("Automatically paste lines to clipboard.")]
        [Category("Logging Options")]
        [Advanced(true)]
        public static ConfigWrapper<bool> copyToClipboard { get; private set; }
        [DisplayName("Include JP line")]
        [Description("Include JP line in automatic and manual paste to clipboard")]
        [Category("Logging Options")]
        [Advanced(true)]
        public static ConfigWrapper<bool> copyJPLine { get; private set; }
        [DisplayName("Log Scene")]
        [Description("Save the current scene's lines to file.")]
        [Category("Logging Options")]
        [Advanced(true)]
        public static ConfigWrapper<bool> sceneLogging { get; private set; }

        [DisplayName("Text Color (Partner)")]
        [Category("Text Colors")]
        [Browsable(false)]
        public static ConfigWrapper<Color> textColor { get; private set; }
        [DisplayName("Outline Color")]
        [Category("Text Colors")]
        [Browsable(false)]
        public static ConfigWrapper<Color> outlineColor { get; private set; }
        [DisplayName("Text Color (2nd Partner)")]
        [Category("Text Colors")]
        [Browsable(false)]
        public static ConfigWrapper<Color> textColor2 { get; private set; }
        [DisplayName("Outline Color (2nd)")]
        [Category("Text Colors")]
        [Browsable(false)]
        public static ConfigWrapper<Color> outlineColor2 { get; private set; }

        [DisplayName("Language Options")]
        [Description("Show Captions None/ENG(default)/JP/Other.\n Selecting Other will permit another plugin to provide translation.")]
        [AcceptableValueList(new object[] { Lang.None, Lang.ENG, Lang.JP, Lang.Other })]
        [Advanced(true)]
        public static ConfigWrapper<Lang> LangOptions { get; private set; }
        [DisplayName("Update Mode")]
        [Description("Fully update when Game starts, partial update when Scene starts, or download only if no cache is available.")]
        [AcceptableValueList(new object[] { UpdateMode.None, UpdateMode.Game, UpdateMode.Scene })]
        [Advanced(true)]
        public static ConfigWrapper<UpdateMode> Updatemode { get; private set; }
        [DisplayName("Log File Prefix")]
        [Description("Label files by personality number, personality name, or character's first name.")]
        [AcceptableValueList(new object[] { PrefixType.PersonalityNo, PrefixType.Personality, PrefixType.Character, PrefixType.Nickname })]
        [Category("Logging Options")]
        [Advanced(true)]
        [Browsable(true)]
        public static ConfigWrapper<PrefixType> prefixType { get; private set; }
        [DisplayName("Log Output Folder")]
        [Description("Directory for log output. If the folder doesn't exist in BepInEx directories, this will reset to default.")]
        [Category("Logging Options")]
        [Advanced(true)]
        public static ConfigWrapper<string> logDir { get; private set; }

        #endregion

        public KKSubsPlugin()
        {
            Plugin = this;

            fontName = new ConfigWrapper<string>("fontName", this, "Arial");
            fontSize = new ConfigWrapper<int>("fontSize", this, -5);
            fontStyle = new ConfigWrapper<FontStyle>("fontStyle", this, FontStyle.Bold);
            textAlign = new ConfigWrapper<TextAnchor>("textAlignment", this, TextAnchor.LowerCenter);
            textOffset = new ConfigWrapper<int>("textOffset", this, 10);
            outlineThickness = new ConfigWrapper<int>("outlineThickness", this, 2);

            copyToClipboard = new ConfigWrapper<bool>("copyToClipboard", this, false);
            copyJPLine = new ConfigWrapper<bool>("copyJPLine", this, false);
            sceneLogging = new ConfigWrapper<bool>("sceneLogging", this, false);

            fontSize.SettingChanged += OnSettingChanged;
            fontName.SettingChanged += OnSettingChanged;
            textAlign.SettingChanged += OnSettingChanged;
            fontStyle.SettingChanged += OnSettingChanged;
            textOffset.SettingChanged += OnSettingChanged;
            outlineThickness.SettingChanged += OnSettingChanged;

            sceneLogging.SettingChanged += SceneLog.LogToggled;
        }

        public void Awake() { }

        public void Start()
        {
            ReloadTrans = new SavedKeyboardShortcut("Reload Translations", this,
                new KeyboardShortcut(KeyCode.R, KeyCode.LeftControl));
            CBCopy = new SavedKeyboardShortcut("Copy2Clipboard", this,
                new KeyboardShortcut(KeyCode.None));
            SceneLogging = new SavedKeyboardShortcut("SceneLogging", this, new KeyboardShortcut(KeyCode.None));

            StartCoroutine(InitAsync());

            harmony = Hooks.InstallHooks(GUID);
        }

        internal static void RenameDirPath(object sender, EventArgs args) { SceneLog.RenameLogPath(); }
        void OnEnable() { }
        void OnDisable() { }

        public void Update()
        {
            if (ReloadTrans.IsPressed())
                StartCoroutine(SubsCache.DownloadSubs());
            else if (CBCopy.IsPressed())
                GUIUtility.systemCopyBuffer = ((copyJPLine.Value ? " : " + VoiceCtrl.currentLine.Key : "") + VoiceCtrl.currentLine.Value);
            else if (SceneLogging.IsPressed()) sceneLogging.Value = !sceneLogging.Value;
        }

        private IEnumerator<WaitWhile> InitAsync()
        {
            yield return new WaitWhile(() => Singleton<Manager.Config>.Instance == null);

            string Col2str(Color c) => ColorUtility.ToHtmlStringRGBA(c);
            Color str2Col(string s) => ColorUtility.TryParseHtmlString("#" + s, out Color c) ? c : Color.clear;

            textColor = new ConfigWrapper<Color>("textColor", this, str2Col, Col2str, Manager.Config.TextData.Font1Color);
            textColor2 = new ConfigWrapper<Color>("textColor2", this, str2Col, Col2str, textColor.Value);
            outlineColor = new ConfigWrapper<Color>("outlineColor", this, str2Col, Col2str, Color.black);
            outlineColor2 = new ConfigWrapper<Color>("outlineColor2", this, str2Col, Col2str, outlineColor.Value);

            LangOptions = new ConfigWrapper<Lang>("LangOptions", this, Lang.ENG);
            Updatemode = new ConfigWrapper<UpdateMode>("Updatemode", this, UpdateMode.None);
            prefixType = new ConfigWrapper<PrefixType>("prefixType", this, PrefixType.PersonalityNo);
            logDir = new ConfigWrapper<string>("logDir", this, "translation");

            textColor.SettingChanged += OnSettingChanged;
            textColor2.SettingChanged += OnSettingChanged;
            outlineColor.SettingChanged += OnSettingChanged;
            outlineColor2.SettingChanged += OnSettingChanged;

            LangOptions.SettingChanged += OnLangChange;

            RenameDirPath(null, null);

            SubsCache.UpdateSubs();

            yield return null;
        }

        void OnSettingChanged(object sender, EventArgs args) { Caption.InitGUI(); }
        void OnLangChange(object sender, EventArgs args) { Caption.UnfixFix(); }
        // req for Patchwork
        void OnSettingChanged() { Caption.InitGUI(); } 
        void OnLangChange() { Caption.UnfixFix();  }

        public static void CleanupScene(HVoiceCtrl ctrl)
        {
            VoiceCtrl.subtitlesDict = new Dictionary<string, KeyValuePair<string, string>>();
            VoiceCtrl.currentLine = new KeyValuePair<string, string>();

            SceneLog.LogFile = null;
            SceneLog.LogFilename = "";

        }

        public void OnDestroy() { Hooks.DetachPatch(); }

        internal static class Hooks
        {
            public static HarmonyInstance InstallHooks(string id)
            {
                var h = harmony ?? HarmonyInstance.Create("org.bepinex.kk.hsubs");
                h.PatchAll(typeof(Hooks));
                return h;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HVoiceCtrl), "Init", new Type[] { typeof(string), typeof(string), typeof(string) })]
            public static void InitScene(string _chaFolder, string _chaFolder1, string _pathAssetFolder, HVoiceCtrl __instance)
            {
                VoiceCtrl.GetVoiceFromInfo(__instance);
                SceneLog.InitSceneFile(__instance.flags.lstHeroine);
                Caption.InitGUI();

                __instance.OnDestroyAsObservable().Subscribe(delegate (Unit _) { CleanupScene(__instance); });
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(LoadVoice), "Play")]
            public static void CatchVoice(LoadVoice __instance)
            {
#if DEBUG
                if (!VoiceCtrl.hproc) return;

                if (__instance.assetName.Length == 14 && !__instance.assetName.StartsWith("h_ko_") &&
                    __instance.assetName != VoiceCtrl.hproc.voice.nowVoices[0].voiceInfo.nameFile &&
                        VoiceCtrl.hproc.flags.lstHeroine.Count > 0 && __instance.assetName != VoiceCtrl.hproc.voice.nowVoices[1].voiceInfo.nameFile)
                   SPAM($"Voice clip playing [{__instance.assetName}] doesn't match current controlled voices.");

#endif
                if (!VoiceCtrl.hproc || __instance.assetName.Length != 14
                    ||__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;

                VoiceCtrl.GenerateSubtitle(__instance);
            }

            public static void DetachPatch(HarmonyInstance hi = null)
            {
                hi = hi ?? harmony;

                foreach (var method in hi.GetPatchedMethods())
                    hi.RemovePatch(method, HarmonyPatchType.All, hi.Id);
            }
        }

        public enum Lang { None, JP, ENG, Other }

        public enum UpdateMode { None, Game, Scene }

        public enum PrefixType { PersonalityNo, Personality, Character, Nickname }

#if DEBUG
        public static void INFO(string s) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, KKSubsPlugin.BEPNAME + s);
        public static void WARN(string s) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Warning, KKSubsPlugin.BEPNAME + s);
        public static void ERROR(string s) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error, KKSubsPlugin.BEPNAME + s);
        public static void SPAM(string s) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Debug, KKSubsPlugin.BEPNAME + s);

        public static Dictionary<string, KeyValuePair<string, string>>subs => VoiceCtrl.subtitlesDict;
        public static List<string> filenames => VoiceCtrl.subtitlesDict.Keys.ToList();
        public static GameObject Captions => Caption.Pane;

        public static string TestCaptions()
        {
            if (Caption.Pane)
                return Caption.TestSub();
            return "";
        }
        public static bool TestRemoveCaption()
        {
            if (Caption.Pane)
                return Caption.TestRemove();
            return false;
        }
#endif
    }
}

