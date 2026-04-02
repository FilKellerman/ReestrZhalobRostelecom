using System;

namespace ReestrObrashcheniy
{
    public class ConfigSettings
    {
        public InterfaceSettings Interface { get; set; } = new InterfaceSettings();
        public BackupSettings Backup { get; set; } = new BackupSettings();
        public FeaturesSettings Features { get; set; } = new FeaturesSettings();
    }

    public class InterfaceSettings
    {
        public string Theme { get; set; } = "dark";
        public int Scale { get; set; } = 100;
        public string FontFamily { get; set; } = "Arial";
    }

    public class BackupSettings
    {
        public bool AutoBackup { get; set; } = true;
        public int BackupIntervalDays { get; set; } = 7;
    }

    public class FeaturesSettings
    {
        public bool TipsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = false;
        public bool HotkeysEnabled { get; set; } = true;
    }
}