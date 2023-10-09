namespace Carbon.Contracts;

public interface IZipScriptProcessor : IBaseProcessor, IDisposable
{
	void InvokeRepeating(Action action, float delay, float repeat);

	GameObject gameObject { get; }

	bool AllPendingScriptsComplete();
	bool AllNonRequiresScriptsComplete();
	bool AllExtensionsComplete();

	void StartCoroutine(IEnumerator coroutine);
	void StopCoroutine(IEnumerator coroutine);

	void Remove(string name);

	public interface IZipScript : IInstance
	{
		IScriptLoader Loader { get; set; }
	}
}
