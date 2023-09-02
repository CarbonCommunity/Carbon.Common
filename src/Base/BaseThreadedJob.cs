﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Base;

public class BaseThreadedJob : IDisposable
{
	internal bool _isDone = false;
	internal object _handle = new();
	internal Task _task = null;

	private CancellationTokenSource cancellationToken;

	public bool IsDone
	{
		get
		{
			bool temp;
			lock (_handle)
			{
				temp = _isDone;
			}
			return temp;
		}
		set
		{
			lock (_handle)
			{
				_isDone = value;
			}
		}
	}

	public virtual void Start()
	{
		if (Community.IsServerInitialized || ConVar.Global.skipAssetWarmup_crashes)
		{
			cancellationToken = new CancellationTokenSource();
			_task = Task.Factory.StartNew(Run, cancellationToken.Token);
		}
		else Run();
	}
	public virtual void Abort()
	{
		cancellationToken?.Cancel();
	}

	public virtual void ThreadFunction() { }
	public virtual void OnFinished() { }

	public virtual bool Update()
	{
		if (IsDone)
		{
			OnFinished();
			return true;
		}
		return false;
	}
	public IEnumerator WaitFor()
	{
		while (!Update())
		{
			yield return null;
		}
	}
	private void Run()
	{
		ThreadFunction();
		IsDone = true;
	}

	public virtual void Dispose() { }
}
