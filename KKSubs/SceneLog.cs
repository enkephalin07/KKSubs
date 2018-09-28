using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace KKSubs
{
    public class SceneLog
    {
        public static FileInfo LogFile { get; internal set; } = null;
        public static string LogFilename { get; internal set; } = "";

        internal static void RenameLogPath()
        {
            KKSubsPlugin.logDir.SettingChanged -= KKSubsPlugin.RenameDirPath;

            var path = string.Join(Path.PathSeparator.ToString(), KKSubsPlugin.logDir.Value.Split(Path.GetInvalidPathChars()));
            path.Replace(Paths.PluginPath, "");

            if (Directory.Exists(Path.Combine(Paths.PluginPath, path)))
                KKSubsPlugin.logDir.Value = path;
            else
                KKSubsPlugin.logDir.Value = "translation";

            KKSubsPlugin.logDir.SettingChanged += KKSubsPlugin.RenameDirPath;
        }

        internal static void LogToggled(object sender, EventArgs args) { LogToggled(); }
        internal static void LogToggled()
        {
            if (!KKSubsPlugin.sceneLogging.Value || LogFilename.IsNullOrEmpty())
                return;

            if (KKSubsPlugin.sceneLogging.Value && (LogFile == null || !LogFile.Exists))
            {
                var path = Path.Combine(Paths.PluginPath, Path.Combine(KKSubsPlugin.logDir.Value, LogFilename));
                path += DateTime.UtcNow.ToString("yyyy_MM_dd_hhmmss") + ".txt";

                try
                {
                    LogFile = new FileInfo(path);
                    using (StreamWriter writer = LogFile.CreateText())
                    {
                        writer.WriteLine(LogFilename);
                        writer.WriteLine("\t");
                    }
                    Logger.Log(BepInEx.Logging.LogLevel.Info, KKSubsPlugin.BEPNAME + $"{LogFile.Name} Created.");
                }
                catch (Exception e)
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Error, KKSubsPlugin.BEPNAME + "LogToggled() " + e.Message);
                    if (LogFile.Exists) LogFile.Delete();
                }
            }
        }

        internal static void WriteToFile(string outstring)
        {
            if (!KKSubsPlugin.sceneLogging.Value || LogFilename.IsNullOrEmpty())
                return;

            try
            {
                if (LogFile == null || !LogFile.Exists) LogToggled();
                if (!LogFile.Exists)
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Error, KKSubsPlugin.BEPNAME + $"WriteToFile({outstring})\tfile: {LogFile.Name}");
                    return;
                }

                using (StreamWriter writer = LogFile.AppendText())
                    writer.WriteLine(outstring);
            }
            catch (Exception e)
            { Logger.Log(BepInEx.Logging.LogLevel.Error, KKSubsPlugin.BEPNAME + e.InnerException.Message); }
        }

        internal static void InitSceneFile(List<SaveData.Heroine> heroines)
        {
            var pref = (LogFilename = "");

            for (int i = 0; i < heroines.Count; i++)
            {
                var fem = heroines[i];

                switch (KKSubsPlugin.prefixType.Value)
                {
                    case KKSubsPlugin.PrefixType.Personality:
                        pref = Singleton<Manager.Voice>.Instance.voiceInfoDic[fem.voiceNo].Personality;
                        break;
                    case KKSubsPlugin.PrefixType.Character:
                        pref = fem.Name;
                        break;
                    case KKSubsPlugin.PrefixType.Nickname:
                        pref = fem.nickname;
                        break;
                    case KKSubsPlugin.PrefixType.PersonalityNo:
                    default:
                        pref = fem.voiceNo.ToString();
                        break;
                }
                LogFilename += pref + "_";
            }
            LogToggled();
        }
    }
}
