using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
                throw new DirectoryNotFoundException(string.Format(
                    "Could not find directory repository directory '{0}'",
                    fileInfo.DirectoryName));
            }

            var repositories =
                repositoryPaths.Select(
                    path => new NuGetRepository(Path.GetFullPath(Path.Combine(repositoryFileDirectory.FullName, path))))
                               .ToList();

            CastaneaLogger.Write(string.Format("Found {0} NuGet repositories", repositories.Count));

            return repositories;
        }

        public int RestorePackages(IReadOnlyCollection<NuGetRepository> nuGetRepositories, NuGetConfig nuGetConfig)
        {
            var exePath = nuGetConfig.NuGetExePath;
            var outputDir = nuGetConfig.OutputDirectory;

            if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
            {
                throw new DirectoryNotFoundException(string.Format("The restore output directory '{0}' does not exist",
                                                                   outputDir));
            }

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException(string.Format("NuGet.exe could not be found at path '{0}'", exePath));
            }


            foreach (var repository in nuGetRepositories)
            {
                CastaneaLogger.Write(string.Format("Installing packages into directory '{0}', defined in '{1}'", outputDir, repository.Path));

                var args = string.Format("install \"{0}\" -OutputDirectory \"{1}\" -Verbosity Detailed", repository.Path, outputDir);
                var process = new Process {StartInfo = new ProcessStartInfo(exePath) {Arguments = args, RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false}};

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

            return nuGetRepositories.Count;
        }

        public NuGetConfig EnsureConfig(NuGetConfig nuGetConfig)
        {
            var config = nuGetConfig ?? new NuGetConfig();

            config.RepositoriesConfig = nuGetConfig.RepositoriesConfig ?? FindRepositoriesConfig();

            var configDir = new FileInfo(config.RepositoriesConfig).Directory;

            if (string.IsNullOrWhiteSpace(config.OutputDirectory))
            {
                nuGetConfig.OutputDirectory = configDir.FullName;
            }

            if (string.IsNullOrWhiteSpace(config.NuGetExePath))
            {
                config.NuGetExePath = Path.Combine(configDir.Parent.FullName, ".nuget", "nuget.exe");
            }

            return config;
        }

        DirectoryInfo WalkToRoot(DirectoryInfo directoryInfo)
        {
            var allowParent = directoryInfo.EnumerateDirectories().All(dir => !dir.Name.Equals(".git"));

            var root = allowParent ? WalkToRoot(directoryInfo.Parent) : directoryInfo;

            return root;
        }

        string FindRepositoriesConfig()
        {
            var appDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            var directory = FindRepositoriesDirectory(WalkToRoot(appDir));

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