namespace Carbon.Contracts;

public interface IZipScriptProcessor : IScriptProcessor, IDisposable
{
	public interface IZipScript : IInstance
	{
		IScriptLoader Loader { get; set; }
	}
}
