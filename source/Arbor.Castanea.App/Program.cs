﻿using System;
using System.Linq;

namespace Arbor.Castanea.App
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var exitCode = TryRunApp(args);

            return exitCode;
        }

        static int TryRunApp(string[] args)
        {
            try
            {
                RunApp(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
            }

            return 0;
        }

        static void RunApp(string[] args)
        {
            CastaneaLogger.SetLoggerAction(Console.WriteLine);
            CastaneaLogger.SetErrorLoggerAction(Console.Error.WriteLine);

            var app = new CastaneaApplication();

            var repositoriesFile = args.FirstOrDefault();

            var config = new NuGetConfig {RepositoriesConfig = repositoriesFile};

            if (args.Length > 1)
            {
                config.OutputDirectory = args[1];
            }

            if (args.Length > 2)
            {
                config.NuGetExePath = args[2];
            }

            app.RestoreAllSolutionPackages(config);
        }
    }
}