//#define DISPLAYMODULE
//#define INTERNALHOOKS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace HSubs
{
#if !DISPLAYMODULE
    public partial class HSubsPlugin
    {
        //        private TextMesh subtitleText;
        public const string FIX = "_Dummy";
        public GameObject Captions { get; private set; }


        internal bool InitGUI()
        {
            if (!(Instance.Captions = Instance.Captions ?? GameObject.Find("Captions")))
                Instance.Captions = new GameObject("Captions");

            var cscl = Captions.GetComponent<CanvasScaler>() ?? Captions.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;

            (Captions.GetComponent<Canvas>() ?? Captions.AddComponent<Canvas>()).renderMode = RenderMode.ScreenSpaceOverlay;
            (Captions.GetComponent<CanvasGroup>() ?? Captions.AddComponent<CanvasGroup>()).blocksRaycasts = false;
            
            var vlg = Captions.GetComponent<VerticalLayoutGroup>() ?? Captions.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = vlg.childForceExpandHeight = vlg.childControlWidth = vlg.childForceExpandWidth = false;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.padding = new RectOffset(0, 0, 0, textOffset.Value);

            return true;
        }

        internal void DisplaySubtitle(LoadVoice voice, string speaker)
        {
            if (LangOptions.Value == Lang.None || (LangOptions.Value == Lang.ENG && currentLine.Value.IsNullOrEmpty()))
                return;

            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            //            Font fontFace = Resources.Load<Font>("SetoFontCustom.ttf");
            int fsize = (int)(fontSize.Value < 0 ? ((fontSize.Value * Screen.height / -100.0)) : fontSize.Value);

            GameObject subtitle = new GameObject(voice.assetName + (LangOptions.Value == Lang.ENG ? FIX : ""));
            subtitle.transform.SetParent(Captions.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.995f, fsize * 0.05f);

            var text = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            text.font = fontFace;
            text.fontSize = fsize;
            text.fontStyle = (fontFace.dynamic) ? fontStyle.Value : FontStyle.Normal;
            text.material = fontFace.material;
            text.alignment = textAlign.Value;
            text.lineSpacing = 0;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.color = textColor.Value;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = outlineColor.Value;
            subOutline.effectDistance = new Vector2(outlineThickness.Value, outlineThickness.Value);

            text.text = speaker + ": " + ((LangOptions.Value == Lang.ENG) ? currentLine.Value : currentLine.Key);

            voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
            {
                currentLine = new KeyValuePair<string, string>();
                subtitle.transform.SetParent(null);
                Destroy(subtitle);
            });
        }

#if DEBUG
        public string TestSub()
        {
            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            int fsize = (int)(fontSize.Value < 0 ? ((fontSize.Value * Screen.height / -100.0)) : fontSize.Value);

            GameObject subtitle = new GameObject($"Test# {Captions.transform.childCount + 1} Lang {LangOptions.Value}"+ (LangOptions.Value == Lang.ENG ? FIX : ""));
            subtitle.transform.SetParent(Captions.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.995f, fsize * 0.05f);

            subtitle.AddComponent<LayoutElement>().flexibleHeight = fsize * 2;

            var text = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            text.font = fontFace;
            text.fontSize = fsize;
            text.fontStyle = (fontFace.dynamic) ? fontStyle.Value : FontStyle.Normal;
            text.material = fontFace.material;
            text.alignment = textAlign.Value;
            text.lineSpacing = 0;
//            text.resizeTextForBestFit = true;
//            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.color = textColor.Value;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = outlineColor.Value;
            subOutline.effectDistance = new Vector2(outlineThickness.Value, outlineThickness.Value);

            text.text = subtitle.name;
            return subtitle.name;
        }

        public bool TestRemove()
        {
            if (Captions.transform.childCount > 0)
            {
                var child = Captions.transform.GetChild(0);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            return true;
        }
#endif
    }
#else
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

