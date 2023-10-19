﻿namespace Carbon.Contracts;

public interface IScriptProcessor : IBaseProcessor, IDisposable
{
	void InvokeRepeating(Action action, float delay, float repeat);

	GameObject gameObject { get; }

	bool AllPendingScriptsComplete();
	bool AllNonRequiresScriptsComplete();
	bool AllExtensionsComplete();

	void StartCoroutine(IEnumerator coroutine);
	void StopCoroutine(IEnumerator coroutine);

	public interface IScript : IProcess
	{
		IScriptLoader Loader { get; set; }
	}
}
