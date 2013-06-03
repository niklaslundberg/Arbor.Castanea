using System;
using System.Reflection;

namespace Arbor.Castanea
{
    public class CastaneaApplication
    {
        public int RestoreAllSolutionPackages(NuGetConfig nuGetConfig)
        {
            Assembly entryAssembly = Assembly.GetExecutingAssembly();
            Version version = entryAssembly.GetName().Version;
            
            CastaneaLogger.Write(GetType().Namespace + ", " + version);

            var helper = new NuGetHelper();

            var repositoriesConfig = helper.EnsureConfig(nuGetConfig);

            var repositories = helper.GetNuGetRepositories(repositoriesConfig);

            return helper.RestorePackages(repositories, repositoriesConfig);
        }
    }
}