namespace Arbor.Castanea
{
	public class NuGetRepository
	{
		readonly string _path;

		public NuGetRepository(string path)
		{
			_path = path;
		}

		public string Path
		{
			get { return _path; }
		}
	}
}