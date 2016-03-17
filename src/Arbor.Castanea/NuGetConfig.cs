using System.Collections.Generic;

namespace Arbor.Castanea
{
    public class NuGetConfig
    {
        public readonly List<string> PackageConfigFiles = new List<string>();

        public string RepositoriesConfig { get; set; }

        public string NuGetExePath { get; set; }

        public string OutputDirectory { get; set; }

        public bool DisableParallelProcessing { get; set; }

        public bool NoCache { get; set; }
    }
}