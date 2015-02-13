using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
                throw new ArgumentNullException("nuGetConfig");
            }

            List<NuGetRepository> repositories;

            if (!nuGetConfig.PackageConfigFiles.Any())
            {
                var fileInfo = new FileInfo(nuGetConfig.RepositoriesConfig);

                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(string.Format("Could not find the repositories.config file '{0}'",
                        nuGetConfig.RepositoriesConfig));
                }

                var xdoc = XDocument.Load(nuGetConfig.RepositoriesConfig);

                var repositoryPaths =
                    xdoc.Descendants("repository")
                        .Select(repository => repository.Attribute("path").Value)
                        .ToList();

                var repositoryFileDirectory = fileInfo.Directory;

                if (repositoryFileDirectory == null)
                {
                    var message = string.Format("Could not find directory repository directory '{0}'",
                        fileInfo.DirectoryName);
                    throw new DirectoryNotFoundException(message);
                }

                repositories =
                    repositoryPaths.Select(
                        path =>
                            new NuGetRepository(Path.GetFullPath(Path.Combine(repositoryFileDirectory.FullName, path))))
                        .ToList();

                CastaneaLogger.Write(string.Format("Found {0} NuGet repositories", repositories.Count));
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
            var exePath = config.NuGetExePath;
            var outputDir = config.OutputDirectory;

            if (!Directory.Exists(outputDir))
            {
                var message = string.Format("The restore output directory '{0}' does not exist", outputDir);

                Console.WriteLine(message);
                Directory.CreateDirectory(outputDir);
            }
            
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException(string.Format("NuGet.exe could not be found at path '{0}'", exePath));
            }

            foreach (var repository in repositories)
            {
                RestorePackage(outputDir, repository, exePath);
            }

            return repositories.Count;
        }

        static void RestorePackage(string outputDir, NuGetRepository repository, string exePath)
        {
            CastaneaLogger.Write(string.Format("Installing packages into directory '{0}', defined in '{1}'",
                                               outputDir, repository.Path));

            var args = string.Format("install \"{0}\" -OutputDirectory \"{1}\" -Verbosity Detailed", repository.Path,
                                     outputDir);
            var process = new Process
                              {
                                  StartInfo =
                                      new ProcessStartInfo(exePath)
                                          {
                                              Arguments = args,
                                              RedirectStandardError = true,
                                              RedirectStandardOutput = true,
                                              UseShellExecute = false
                                          }
                              };

            process.OutputDataReceived += (sender, eventArgs) => CastaneaLogger.Write(eventArgs.Data);
            process.ErrorDataReceived += (sender, eventArgs) => CastaneaLogger.WriteError(eventArgs.Data);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            var exitCode = process.ExitCode;

            if (exitCode == 0)
            {
                CastaneaLogger.Write(string.Format("Successfully installed packages in '{0}'", repository.Path));
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("Failed to install packages in '{0}'. The process '{1}' exited with code {2}",
                                  repository.Path, process.StartInfo.FileName, exitCode));
            }
        }

        public NuGetConfig EnsureConfig(NuGetConfig nuGetConfig)
        {
            var config = nuGetConfig ?? new NuGetConfig();

            if (!config.PackageConfigFiles.Any())
            {
                config.RepositoriesConfig = config.RepositoriesConfig ?? FindRepositoriesConfig();

                var configDir = new FileInfo(config.RepositoriesConfig).Directory;

                if (string.IsNullOrWhiteSpace(config.OutputDirectory))
                {
                    config.OutputDirectory = configDir.FullName;
                }

                if (string.IsNullOrWhiteSpace(config.NuGetExePath))
                {
                    Console.WriteLine("No nuget.exe path specified, looking for ..\\.nuget\\nuget.exe");
                    config.NuGetExePath = Path.Combine(configDir.Parent.FullName, ".nuget", "nuget.exe");
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

            var nuGetExeExists = File.Exists(config.NuGetExePath);

            if (!nuGetExeExists)
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                Directory.CreateDirectory(tempFolder);
                Console.WriteLine("'{0}' does not exist, downloading NuGet.exe from NuGet.org", config.NuGetExePath);

                try
                {
                    var tempPath = Path.Combine(tempFolder, "nuget.exe");
                    var webClient = new WebClient();
                    webClient.DownloadFile("https://nuget.org/nuget.exe", tempPath);
                    config.NuGetExePath = tempPath;
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex);
                    Directory.Delete(tempFolder, true);
                }
            }
            
            return config;
        }

        string FindRepositoriesConfig()
        {
            var vcsRoot = VcsPathHelper.FindVcsRootPath();

            var directory = FindRepositoriesDirectory(new DirectoryInfo(vcsRoot));

            if (directory == null)
            {
                throw new FileNotFoundException("Could not find repositories.config anywhere");
            }

            var path = Path.Combine(directory.FullName, "repositories.config");

            CastaneaLogger.Write(string.Format("Repositories.config found at '{0}' after {1} iterations", path, _counter));

            return path;
        }

        DirectoryInfo FindRepositoriesDirectory(DirectoryInfo folder)
        {
            _counter++;

            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            var forbiddenNames = new List<string> {".git", "bin", "obj"};

            if (forbiddenNames.Any(name => name.Equals(folder.Name)))
            {
                return null;
            }

            var configFile = folder.GetFiles("repositories.config").FirstOrDefault();

            if (configFile != null)
            {
                return folder;
            }

            var subDirs = folder.GetDirectories();

            foreach (var directoryInfo in subDirs)
            {
                var configDir = FindRepositoriesDirectory(directoryInfo);

                if (configDir != null)
                {
                    return configDir;
                }
            }

            return null;
        }
    }
}