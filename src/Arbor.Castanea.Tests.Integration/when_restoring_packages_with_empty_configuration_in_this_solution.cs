using System;
using Machine.Specifications;

namespace Arbor.Castanea.Tests.Integration
{
    [Subject(typeof (CastaneaApplication))]
    public class when_restoring_packages_with_empty_configuration_in_this_solution
    {
        static CastaneaApplication app;
        static NuGetConfig nuGetConfig;
        static int restored;

        Establish context = () =>
        {
            CastaneaLogger.SetLoggerAction(Console.WriteLine);
            app = new CastaneaApplication();
            nuGetConfig = new NuGetConfig();
        };

        Because of = () =>
        {
            restored = app.RestoreAllSolutionPackages(nuGetConfig,
                findVcsRootPath: path => VcsTestPathHelper.FindVcsRootPath());
        };

        It should_restore_two_packages = () => restored.ShouldEqual(2);
    }
}