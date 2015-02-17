﻿using System.Collections.Generic;

namespace Arbor.Castanea
{
    public class NuGetConfig
    {
        public string RepositoriesConfig { get; set; }

        public readonly List<string> PackageConfigFiles = new List<string>();

        public string NuGetExePath { get; set; }

        public string OutputDirectory { get; set; }
    }
}