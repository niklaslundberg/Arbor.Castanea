namespace Arbor.Castanea
{
    public class CastaneaApplication
    {
        public int RestoreAllSolutionPackages(NuGetConfig nuGetConfig)
        {
            var helper = new NuGetHelper();

            var ensuredConfig = helper.EnsureConfig(nuGetConfig);

            var repositories = helper.GetNuGetRepositories(ensuredConfig);

            return helper.RestorePackages(repositories, ensuredConfig);
        }
    }
}