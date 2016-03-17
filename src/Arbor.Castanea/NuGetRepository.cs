namespace Arbor.Castanea
{
    public class NuGetRepository
    {
        public NuGetRepository(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}