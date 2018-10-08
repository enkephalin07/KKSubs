using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace KKSubs
{
    public class Caption
    {
        //        private TextMesh subtitleText;
        public static GameObject Pane { get; internal set; }
        public const string PANE = "Captions";
        public const string FIX = "_Dummy";

        internal static bool InitGUI()
        {
            if (!(Pane = Pane ?? (GameObject.Find(PANE)) ?? GameObject.Find(PANE + FIX)))
                Pane = new GameObject(PANE + (KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.Other ? "" : FIX));

            var cscl = Pane.GetComponent<CanvasScaler>() ?? Pane.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;

            (Pane.GetComponent<Canvas>() ?? Pane.AddComponent<Canvas>()).renderMode = RenderMode.ScreenSpaceOverlay;
            (Pane.GetComponent<CanvasGroup>() ?? Pane.AddComponent<CanvasGroup>()).blocksRaycasts = false;

            var vlg = Pane.GetComponent<VerticalLayoutGroup>() ?? Pane.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = vlg.childForceExpandHeight = vlg.childControlWidth = vlg.childForceExpandWidth = false;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.padding = new RectOffset(0, 0, 0, KKSubsPlugin.textOffset.Value);

            return true;
        }

        internal static void DisplaySubtitle(LoadVoice voice, string speaker)
        {
            if (KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.None
                || (KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.Translated && VoiceCtrl.currentLine.Value.IsNullOrEmpty()))
                return;

            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            //            Font fontFace = Resources.Load<Font>("SetoFontCustom.ttf");
            var fsize = KKSubsPlugin.fontSize.Value;
            fsize = (int)(fsize < 0 ? ((fsize * Screen.height / -100.0)) : fsize);

            GameObject subtitle = new GameObject(voice.assetName);
            subtitle.transform.SetParent(Pane.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

            var text = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            text.font = fontFace;
            text.fontSize = fsize;
            text.fontStyle = (fontFace.dynamic) ? KKSubsPlugin.fontStyle.Value : FontStyle.Normal;
            text.alignment = KKSubsPlugin.textAlign.Value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = KKSubsPlugin.textColor.Value;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = KKSubsPlugin.outlineColor.Value;
            subOutline.effectDistance = new Vector2(KKSubsPlugin.outlineThickness.Value, KKSubsPlugin.outlineThickness.Value);

            text.text = speaker + ": " + ((KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.Translated) ? VoiceCtrl.currentLine.Value : VoiceCtrl.currentLine.Key);

            voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
            {
                VoiceCtrl.currentLine = new KeyValuePair<string, string>();
                subtitle.transform.SetParent(null);
                Object.Destroy(subtitle);
            });
        }

        public static void UnfixFix()
        {
            if (Pane)
                Pane.name = (KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.Other) ? PANE : PANE + FIX;
        }

#if DEBUG
        public static string TestSub()
        {
            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

            var fsize = KKSubsPlugin.fontSize.Value;
            fsize = (int)(fsize < 0 ? ((fsize * Screen.height / -100.0)) : fsize);

            GameObject subtitle = new GameObject($"Test# {Pane.transform.childCount + 1} Lang {KKSubsPlugin.LangOptions.Value}" + (KKSubsPlugin.LangOptions.Value == KKSubsPlugin.Lang.Other ? "" : FIX));
            subtitle.transform.SetParent(Pane.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.995f, fsize + (fsize * 0.05f));

            subtitle.AddComponent<LayoutElement>().flexibleHeight = fsize * 2;

            var text = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            text.font = fontFace;
            text.fontSize = fsize;
            text.fontStyle = (fontFace.dynamic) ? KKSubsPlugin.fontStyle.Value : FontStyle.Normal;
            text.alignment = KKSubsPlugin.textAlign.Value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = KKSubsPlugin.textColor.Value;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = KKSubsPlugin.outlineColor.Value;
            subOutline.effectDistance = new Vector2(KKSubsPlugin.outlineThickness.Value, KKSubsPlugin.outlineThickness.Value);

            text.text = subtitle.name;
            return subtitle.name;
        }

        public static bool TestRemove()
        {
            if (Pane.transform.childCount > 0)
            {
                var child = Pane.transform.GetChild(0);
                child.SetParent(null);
                Object.Destroy(child.gameObject);
            }
            return true;
        }
#endif
    }
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