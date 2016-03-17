using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;

using Arbor.Aesculus.Core;

namespace Arbor.Castanea
{
    public class NuGetHelper
    {
        int _counter;

        public IReadOnlyCollection<NuGetRepository> GetNuGetRepositories(NuGetConfig nuGetConfig)
        {
            if (nuGetConfig == null)
            {
                throw new ArgumentNullException(nameof(nuGetConfig));
            }

            List<NuGetRepository> repositories;

            if (!nuGetConfig.PackageConfigFiles.Any())
            {
                FileInfo fileInfo = new FileInfo(nuGetConfig.RepositoriesConfig);

                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(
                        $"Could not find the repositories.config file '{nuGetConfig.RepositoriesConfig}'");
                }

                XDocument xdoc = XDocument.Load(nuGetConfig.RepositoriesConfig);

                List<string> repositoryPaths =
                    xdoc.Descendants("repository")
                        .Select(repository => repository.Attribute("path").Value)
                        .ToList();

                DirectoryInfo repositoryFileDirectory = fileInfo.Directory;

                if (repositoryFileDirectory == null)
                {
                    string message = $"Could not find directory repository directory '{fileInfo.DirectoryName}'";
                    throw new DirectoryNotFoundException(message);
                }

                repositories =
                    repositoryPaths.Select(
                        path =>
                        new NuGetRepository(Path.GetFullPath(Path.Combine(repositoryFileDirectory.FullName, path))))
                        .ToList();

                CastaneaLogger.WriteDebug($"Found {repositories.Count} NuGet repositories");
            }
            else
            {
                repositories = nuGetConfig.PackageConfigFiles
                    .Select(file => new NuGetRepository(file))
                    .ToList();
            }

            return repositories;
        }

        public int RestorePackages(IReadOnlyCollection<NuGetRepository> repositories, NuGetConfig config)
        {
            string exePath = config.NuGetExePath;
            string outputDir = config.OutputDirectory;

            if (!Directory.Exists(outputDir))
            {
                string message = $"The restore output directory '{outputDir}' does not exist";

                CastaneaLogger.WriteDebug(message);
                Directory.CreateDirectory(outputDir);
            }

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException($"NuGet.exe could not be found at path '{exePath}'");
            }

            foreach (NuGetRepository repository in repositories)
            {
                TryRestorePackage(repository, config);
            }

            return repositories.Count;
        }

        static void TryRestorePackage(NuGetRepository repository, NuGetConfig config)
        {
            int maxAttempts = 3;

            bool succeeded = false;

            int attempt = 1;

            while (!succeeded && attempt <= maxAttempts)
            {
                try
                {
                    RestorePackage(repository, config);
                    succeeded = true;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal() || attempt == maxAttempts)
                    {
                        throw;
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(200));
                }
            }
        }

        static void RestorePackage(NuGetRepository repository, NuGetConfig config)
        {
            CastaneaLogger.WriteDebug(
                $"Installing packages into directory '{config.OutputDirectory}', defined in '{repository.Path}'");

            string args =
                $"restore \"{repository.Path}\" -PackagesDirectory \"{config.OutputDirectory}\" -NonInteractive";

            if (CastaneaLogger.DebugLog != null)
            {
                args += " -Verbosity Detailed";
            }

            if (config.DisableParallelProcessing)
            {
                args += " -DisableParallelProcessing";
            }

            if (config.NoCache)
            {
                args += " -NoCache";
            }

            CastaneaLogger.WriteDebug($"Running exe '{config.NuGetExePath}' with arguments: {args}");

            Process process = new Process
                                  {
                                      StartInfo =
                                          new ProcessStartInfo(config.NuGetExePath)
                                              {
                                                  Arguments = args,
                                                  RedirectStandardError = true,
                                                  RedirectStandardOutput = true,
                                                  UseShellExecute = false
                                              }
                                  };

            process.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        CastaneaLogger.WriteDebug(eventArgs.Data);
                    }
                };
            process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        CastaneaLogger.WriteError(eventArgs.Data);
                    }
                };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            int exitCode = process.ExitCode;

            if (exitCode == 0)
            {
                CastaneaLogger.WriteDebug($"Successfully installed packages in '{repository.Path}'");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Failed to install packages in '{repository.Path}'. The process '{process.StartInfo.FileName}' exited with code {exitCode}");
            }
        }

        public NuGetConfig EnsureConfig(NuGetConfig nuGetConfig, Func<string, string> findVcsRoot = null)
        {
            Func<string, string> usedFindVcsRoot = findVcsRoot ?? VcsPathHelper.FindVcsRootPath;

            NuGetConfig config = nuGetConfig ?? new NuGetConfig();

            if (!config.PackageConfigFiles.Any())
            {
                if (config.RepositoriesConfig != null &&
                    config.RepositoriesConfig.EndsWith(
                        "repositories.config",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    DirectoryInfo configDir = new FileInfo(config.RepositoriesConfig).Directory;

                    if (string.IsNullOrWhiteSpace(config.OutputDirectory))
                    {
                        config.OutputDirectory = configDir.FullName;
                    }

                    if (string.IsNullOrWhiteSpace(config.NuGetExePath))
                    {
                        CastaneaLogger.WriteDebug("No nuget.exe path specified, looking for ..\\.nuget\\nuget.exe");
                        config.NuGetExePath = Path.Combine(configDir.Parent.FullName, ".nuget", "nuget.exe");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(config.RepositoriesConfig))
                {
                    string root = usedFindVcsRoot(config.RepositoriesConfig);

                    DirectoryInfo rootDirectory = new DirectoryInfo(root);

                    if (!rootDirectory.Exists)
                    {
                        throw new Exception(
                            $"Cannot scan directory '{rootDirectory.FullName}' for package config files since it does not exist");
                    }

                    FileInfo[] packageConfigFiles = rootDirectory.GetFiles(
                        "packages.config",
                        SearchOption.AllDirectories);

                    config.PackageConfigFiles.AddRange(packageConfigFiles.Select(file => file.FullName));
                }
                else
                {
                    config.RepositoriesConfig = FindRepositoriesConfig(usedFindVcsRoot);

                    DirectoryInfo configDir = new FileInfo(config.RepositoriesConfig).Directory;

                    if (string.IsNullOrWhiteSpace(config.OutputDirectory))
                    {
                        config.OutputDirectory = configDir.FullName;
                    }

                    if (string.IsNullOrWhiteSpace(config.NuGetExePath))
                    {
                        CastaneaLogger.WriteDebug("No nuget.exe path specified, looking for ..\\.nuget\\nuget.exe");
                        config.NuGetExePath = Path.Combine(configDir.Parent.FullName, ".nuget", "nuget.exe");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(config.OutputDirectory))
            {
                throw new InvalidOperationException("The output directory not been specified");
            }

            if (string.IsNullOrWhiteSpace(config.NuGetExePath))
            {
                throw new InvalidOperationException("The NuGet exe path has not been specified");
            }

            bool nuGetExeExists = File.Exists(config.NuGetExePath);

            if (!nuGetExeExists)
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                Directory.CreateDirectory(tempFolder);
                CastaneaLogger.WriteDebug(
                    $"'{config.NuGetExePath}' does not exist, downloading NuGet.exe from NuGet.org");

                try
                {
                    string tempPath = Path.Combine(tempFolder, "nuget.exe");
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", tempPath);
                    config.NuGetExePath = tempPath;
                }
                catch (WebException ex)
                {
                    CastaneaLogger.WriteDebug(ex.ToString());
                    Directory.Delete(tempFolder, true);
                }
            }

            return config;
        }

        string FindRepositoriesConfig(Func<string, string> usedFindVcsRoot)
        {
            string vcsRoot = usedFindVcsRoot(null);

            DirectoryInfo directory = FindRepositoriesDirectory(new DirectoryInfo(vcsRoot));

            if (directory == null)
            {
                throw new FileNotFoundException("Could not find repositories.config anywhere");
            }

            string path = Path.Combine(directory.FullName, "repositories.config");

            CastaneaLogger.WriteDebug($"Repositories.config found at '{path}' after {_counter} iterations");

            return path;
        }

        DirectoryInfo FindRepositoriesDirectory(DirectoryInfo folder)
        {
            _counter++;

            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }

            List<string> forbiddenNames = new List<string>
                                              {
                                                  ".git",
                                                  "bin",
                                                  "obj"
                                              };

            if (forbiddenNames.Any(name => name.Equals(folder.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return null;
            }

            FileInfo configFile = folder.GetFiles("repositories.config").FirstOrDefault();

            if (configFile != null)
            {
                return folder;
            }

            DirectoryInfo[] subDirs = folder.GetDirectories();

            foreach (DirectoryInfo directoryInfo in subDirs)
            {
                DirectoryInfo configDir = FindRepositoriesDirectory(directoryInfo);

                if (configDir != null)
                {
                    return configDir;
                }
            }

            return null;
        }
    }
}