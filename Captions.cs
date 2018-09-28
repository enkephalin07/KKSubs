//#define DISPLAYMODULE
//#define INTERNALHOOKS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace HSubs
{
    public partial class HSubsPlugin
    {
        //        private TextMesh subtitleText;
        private GameObject Caption;
        private Text subtitleText;

        internal void GenerateSubtitle(LoadVoice voice)
        {
            string clipname = voice.assetName.ToLower();
            if (!subtitlesDict.TryGetValue(clipname, out KeyValuePair<string, string> subs) || subs.Key.IsNullOrEmpty())
                return;

            currentLine = new KeyValuePair<string, string>(subs.Key, subs.Value);

            var speaker = voice.voiceTrans.gameObject.GetComponentInParent<ChaControl>().chaFile.parameter.firstname;
            var outstring = $"[{clipname}] {speaker}:  {currentLine.Key}  {currentLine.Value}";
            if (copyToClipboard.Value)
                GUIUtility.systemCopyBuffer =
                    ("[" + clipname + "]" + speaker + ":" + (copyJPLine.Value ? " : " + currentLine.Key : "") + currentLine.Value);

            if (sceneLogging.Value)
                WriteToFile(outstring);

            if (LangOptions.Value == Lang.None || (LangOptions.Value == Lang.ENG && currentLine.Value.IsNullOrEmpty()))
                return;
#if DEBUG
            SPAM(outstring);
#endif

            voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
            {
                currentLine = new KeyValuePair<string, string>();
                Instance.subtitleText.text = "";
            });
        }

        internal bool InitGUI()
        {
            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            //            Font fontFace = Resources.Load<Font>("SetoFontCustom.ttf");
            int fsize = (int)(fontSize.Value < 0 ? ((fontSize.Value * Screen.height / -100.0)) : fontSize.Value);

            var cscl = gameObject.GetComponent<CanvasScaler>() ?? Instance.gameObject.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;
            (Instance.gameObject.GetComponent<Canvas>() ?? Instance.gameObject.AddComponent<Canvas>()).renderMode = RenderMode.ScreenSpaceOverlay;

            if (!(Instance.Caption = Instance.Caption ?? GameObject.Find("HSubs_Dummy")))
                Instance.Caption = new GameObject("HSubs_Dummy");
            Caption.transform.SetParent(gameObject.transform, false);

            RectTransform rect = Caption.GetComponent<RectTransform>() ?? Instance.Caption.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, textOffset.Value);
            rect.sizeDelta = new Vector2(Screen.width * 0.995f, Screen.height * 0.1f);

            var vlg = Caption.GetComponent<VerticalLayoutGroup>() ?? Caption.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = true;
            vlg.childAlignment = TextAnchor.LowerCenter;

            subtitleText = Caption.GetComponent<Text>() ?? Caption.AddComponent<Text>();
            subtitleText.font = fontFace;
            subtitleText.fontSize = fsize;
            subtitleText.fontStyle = (fontFace.dynamic) ? fontStyle.Value : FontStyle.Normal;
            subtitleText.material = fontFace.material;
            subtitleText.supportRichText = true;
            subtitleText.alignment = textAlign.Value;
            subtitleText.lineSpacing = 1;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.text = subtitleText.text ?? string.Empty;

            var subOutline = Caption.GetComponent<Outline>() ?? Caption.AddComponent<Outline>();
            subOutline.enabled = true;
            subOutline.effectColor = outlineColor.Value;
            subOutline.effectDistance = new Vector2(outlineThickness.Value, outlineThickness.Value);

            subtitleText.material.color = textColor.Value;
            subtitleText.color = textColor.Value;
            
            return true;
        }
    }
#if DISPLAYMODULE
    internal class Caption : MonoBehaviour, IDisposable
    {


        public static List<Caption> captions { get; private set; }
        public static GameObject canvasRoot {  get; private set;  }
        public static Font fontFace { get; internal set; } = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        public static int fontSize { get; internal set; }

        public static void SceneStart() { }

        public struct CaptionInfo
        {
            public const string FIX = "_Dummy";
            public Transform voiceTrans;
            public List<string> translations;
            public string capID; // = voice line ID
            public float lifetime;
        }

        public Caption(CaptionInfo cap)
        {
            var go = new GameObject(cap.capID + (HSubsPlugin.LangOptions.Value == HSubsPlugin.Lang.Other ? CaptionInfo.FIX : ""));

            Text subtitleText = go.GetComponent<Text>() ?? go.AddComponent<Text>();
            subtitleText.font = fontFace;
            subtitleText.fontSize = fontSize;
            subtitleText.fontStyle = (fontFace.dynamic) ? HSubsPlugin.fontStyle.Value : FontStyle.Normal;
            subtitleText.material = fontFace.material;
            subtitleText.supportRichText = true;
            subtitleText.alignment = HSubsPlugin.textAlign.Value;
            subtitleText.lineSpacing = 1;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.text = subtitleText.text ?? string.Empty;

            Outline subOutline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            subOutline.enabled = true;
            subOutline.effectColor = HSubsPlugin.outlineColor.Value;
            subOutline.effectDistance = new Vector2(HSubsPlugin.outlineThickness.Value, HSubsPlugin.outlineThickness.Value);

            subtitleText.material.color = HSubsPlugin.textColor.Value;
            subtitleText.color = HSubsPlugin.textColor.Value;

            go.transform.SetParent(canvasRoot.transform);
            captions.Add(this);
        }

        public void Dispose()
        {
            captions.Remove(this);
        }

        private bool RootCanvas(GameObject gameObject)
        {
            if (canvasRoot) return true;

            var cscl = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;
            var cvs = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            cvs.renderMode = RenderMode.ScreenSpaceOverlay;

            var vlg = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = true;
            vlg.childAlignment = TextAnchor.LowerCenter;

            canvasRoot = gameObject;

            return true;
        }

    public void Dispose() { }
#endif
#if INTERNALHOOKS
        internal static class Hooks
        {
            public static HarmonyInstance harmony { get; internal set; }

            public static void InstallHooks(string id)
            {
                harmony = HarmonyInstance.Create("org.bepinex.kk.hsubs.caption");
                harmony.PatchAll(typeof(Hooks));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(LoadVoice), "Play")]
            public static void CatchVoice(LoadVoice __instance)
            {
                if (__instance.assetName.Length != 14
                    || __instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;
                Instance.GenerateSubtitle(__instance);
            }
        }
    }
#endif
}
/*
//        public static TextGenerationSettings subGenSet;   // For mesh conversion
            subGenSet = new TextGenerationSettings()
            {
                font = fontFace,
                fontSize = fsize,
                fontStyle = (fontFace.dynamic) ? fontStyle.Value : FontStyle.Normal,
                color = textColor.Value,
                richText = true,
                textAnchor = textAlign.Value,
                generationExtents = new Vector2(Screen.width, (textOffset.Value + fsize) * 2.5f),
                lineSpacing = 1,
                scaleFactor = 1,
                pivot = new Vector2(0.5f, 0.5f),
                horizontalOverflow = HorizontalWrapMode.Wrap,
                verticalOverflow = VerticalWrapMode.Overflow,
                generateOutOfBounds = true,
            }; 
*/

