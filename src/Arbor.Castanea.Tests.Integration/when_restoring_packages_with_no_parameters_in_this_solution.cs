using System;
using Machine.Specifications;
using Machine.Specifications.Model;

namespace Arbor.Castanea.Tests.Integration
{
    [Subject(typeof (Subject))]
    public class when_restoring_packages_with_no_parameters_in_this_solution
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

        Because of =
            () => restored = app.RestoreAllSolutionPackages(nuGetConfig);

        It should_restore_two_packages = () => restored.ShouldEqual(2);
    }
}