﻿using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class FolderSettings : IDumper
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Files = nameof(Files);
        public override string ToString() => "Folder / Dumping Settings";

        [Category(FeatureToggle), Description("When enabled, dumps any received PKM files (trade results) to the DumpFolder.")]
        public bool Dump { get; set; }

        [Category(Files), Description("Source folder: where PKM files to distribute are selected from.")]
        public string DistributeFolder { get; set; } = string.Empty;

        [Category(Files), Description("Source folder: where WC files for SpecialRequests are pulled from.")]
        public string SpecialRequestWCFolder { get; set; } = string.Empty;

        [Category(Files), Description("Source folder: where PKM files to distribute are selected from.")]
        public string GiveAwayFolder { get; set; } = string.Empty;        

        [Category(Files), Description("Destination folder: where all received PKM files are dumped to.")]
        public string DumpFolder { get; set; } = string.Empty;

        public void CreateDefaults(string path)
        {
            var dump = Path.Combine(path, "dump");
            Directory.CreateDirectory(dump);
            DumpFolder = dump;
            Dump = true;

            var distribute = Path.Combine(path, "distribute");
            Directory.CreateDirectory(distribute);
            DistributeFolder = distribute;

            var wc = Path.Combine(path, "wc");
            Directory.CreateDirectory(wc);
            SpecialRequestWCFolder = wc;

            var giveaway = Path.Combine(path, "giveaway");
            Directory.CreateDirectory(giveaway);
            GiveAwayFolder = giveaway;
        }
    }
}