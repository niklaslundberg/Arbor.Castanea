using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Arbor.Castanea
{
    public class CastaneaApplication
    {
        /// <summary>
        ///     Returns the number of packages.config files that have been restored
        /// </summary>
        /// <param name="nuGetConfig"></param>
        /// <param name="logInfo"></param>
        /// <param name="logError"></param>
        /// <param name="logDebug"></param>
        /// <param name="removeNuGetDirectoryAfterRestore"></param>
        /// <param name="findVcsRootPath"></param>
        /// <returns></returns>
        public int RestoreAllSolutionPackages(
            NuGetConfig nuGetConfig,
            Action<string> logInfo = null,
            Action<string> logError = null,
            Action<string> logDebug = null,
            bool removeNuGetDirectoryAfterRestore = false,
            Func<string, string> findVcsRootPath = null)
        {
            CastaneaLogger.SetErrorLoggerAction(logError);
            CastaneaLogger.SetLoggerAction(logInfo);
            CastaneaLogger.SetLoggerDebugAction(logDebug);

            try
            {
                Assembly entryAssembly = Assembly.GetExecutingAssembly();
                Version version = entryAssembly.GetName().Version;

                CastaneaLogger.WriteDebug(GetType().Namespace + ", " + version);

                var helper = new NuGetHelper();

                NuGetConfig repositoriesConfig = helper.EnsureConfig(nuGetConfig, findVcsRootPath);

                IReadOnlyCollection<NuGetRepository> repositories = helper.GetNuGetRepositories(repositoriesConfig);

                return helper.RestorePackages(repositories, repositoriesConfig);
            }
            finally
            {
                if (removeNuGetDirectoryAfterRestore)
                {
                    if (!string.IsNullOrWhiteSpace(nuGetConfig.NuGetExePath))
                    {
                        bool isInTempDirectory = nuGetConfig.NuGetExePath.IndexOf(Path.GetTempPath(),
                            StringComparison.InvariantCultureIgnoreCase) >= 0;

                        if (isInTempDirectory)
                        {
                            var fileInfo = new FileInfo(nuGetConfig.NuGetExePath);

                            if (fileInfo.DirectoryName != null)
                            {
                                if (Directory.Exists(fileInfo.DirectoryName))
                                {
                                    Directory.Delete(fileInfo.DirectoryName, recursive: true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}