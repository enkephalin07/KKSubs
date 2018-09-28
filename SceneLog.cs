using System;
using System.IO;
using BepInEx;

namespace HSubs
{
    public partial class HSubsPlugin
    {
        public static FileInfo LogFile { get; internal set; } = null;
        public static string LogFilename { get; internal set; } = "";

        public static string fileCache => Path.Combine(Paths.PluginPath, "translation/hsubs.msgpack");

        void LogToggled(object sender, EventArgs args) { LogToggled(); }
        void LogToggled()
        {
            if (LogFilename.IsNullOrEmpty())
                return;

            LogFilename = Path.Combine(Paths.PluginPath, Path.Combine(logDir.Value, LogFilename + DateTime.UtcNow.ToString("s"))) + ".txt";
            SPAM($"LogToggled() {DateTime.UtcNow.ToString("s")}: fname: {LogFilename}");

            if (sceneLogging.Value && (LogFile == null || !LogFile.Exists))
            {
                try
                {

                    LogFile = new FileInfo(LogFilename);
                    using (StreamWriter writer = LogFile.AppendText())
                        writer.WriteLine(LogFilename);
                    SPAM($"LogToggled() {LogFile.Name} Created. " + (LogFile.Exists ? "" : "But still doesn't exist"));
                }
                catch (Exception e)
                {
                    SPAM("LogToggled() " + e.Message + "\n   " + e.InnerException.Message);
                    if (LogFile.Exists) LogFile.Delete();
                }
            }
        }

        void WriteToFile(string outstring)
        {
            SPAM($"WriteToFile({outstring})");
            try
            {
                if (!LogFile.Exists) LogToggled();
                if (!LogFile.Exists)
                {
                    SPAM($"I didn't expect this. WriteToFile() SFN: {LogFilename}  file: {LogFile.Name}");
                    return;
                }

                using (StreamWriter writer = LogFile.AppendText())
                    writer.WriteLine(outstring);
                SPAM($"WriteToFile - written");
            }
            catch (Exception e)
            { SPAM(e.Message + "\n   " + e.InnerException.Message); }
        }
    }
}
