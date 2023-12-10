﻿using Facepunch;
using static Oxide.Plugins.RustPlugin;
using Logger = Carbon.Logger;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Plugins;

public class Timers : Library
{
	public RustPlugin Plugin { get; }
	internal List<Timer> _timers { get; set; } = new List<Timer>();

	public Timers() { }
	public Timers(RustPlugin plugin)
	{
		Plugin = plugin;
	}

	public bool IsValid()
	{
		return Plugin != null && Plugin.persistence != null;
	}
	public void Clear()
	{
		if (_timers == null)
		{
			return;
		}

		foreach (var timer in _timers)
		{
			timer.Destroy();
		}

		_timers.Clear();
		_timers = null;
	}

	public Persistence Persistence => Plugin.persistence;

	public Timer In(float time, Action action)
	{
		if (!IsValid()) return null;

		var timer = new Timer(Persistence, action, Plugin);
		var activity = new Action(() =>
		{
			try
			{
				action?.Invoke();
				timer.TimesTriggered++;
			}
			catch (Exception ex) { Plugin.LogError($"Timer {time}s has failed:", ex); }
		});

		timer.Delay = time;
		timer.Callback = activity;

		if (Community.IsServerInitialized)
		{
			Persistence.Invoke(activity, time);
		}

		return timer;
	}
	public Timer Once(float time, Action action)
	{
		return In(time, action);
	}
	public Timer Every(float time, Action action)
	{
		if (!IsValid()) return null;

		var timer = new Timer(Persistence, action, Plugin);
		var activity = new Action(() =>
		{
			try
			{
				action?.Invoke();
				timer.TimesTriggered++;
			}
			catch (Exception ex)
			{
				Plugin.LogError($"Timer {time}s has failed:", ex);

				timer.Destroy();
				Pool.Free(ref timer);
			}
		});

		timer.Callback = activity;
		Persistence.InvokeRepeating(activity, time, time);
		return timer;
	}
	public Timer Repeat(float time, int times, Action action)
	{
		if (!IsValid()) return null;

		var timer = new Timer(Persistence, action, Plugin);
		var activity = new Action(() =>
		{
			try
			{
				action?.Invoke();
				timer.TimesTriggered++;

				if (times != 0 && timer.TimesTriggered >= times)
				{
					if (Persistence != null)
					{
						Persistence.CancelInvoke(timer.Callback);
						Persistence.CancelInvokeFixedTime(timer.Callback);
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.LogError($"Timer {time}s has failed:", ex);

				timer.Destroy();
				Pool.Free(ref timer);
			}
		});

		timer.Delay = time;
		timer.Callback = activity;
		Persistence.InvokeRepeating(activity, time, time);
		return timer;
	}
	public void Destroy(ref Timer timer)
	{
		if (timer != null)
		{
			timer.Destroy();
		}

		timer = null;
	}
	public void DestroyAll()
	{
		foreach (var timer in _timers)
		{
			timer.Destroy();
		}

		_timers.Clear();
	}
}

public class Timer : Library, IDisposable
{
	public RustPlugin Plugin { get; set; }

	public Action Activity { get; set; }
	public Action Callback { get; set; }
	public Persistence Persistence { get; set; }
	public int Repetitions { get; set; }
	public float Delay { get; set; }
	public int TimesTriggered { get; set; }
	public bool Destroyed { get; set; }

	public Timer() { }
	public Timer(Persistence persistence, Action activity, RustPlugin plugin = null)
	{
		Persistence = persistence;
		Activity = activity;
		Plugin = plugin;
	}

	public void Reset(float delay = -1f, int repetitions = 1)
	{
		TimesTriggered = 0;
		Repetitions = repetitions;
		Delay = delay;

		if (Destroyed)
		{
			Logger.Warn($"You cannot restart a timer that has been destroyed.");
			return;
		}

		if (Persistence != null)
		{
			Persistence.CancelInvoke(Callback);
			Persistence.CancelInvokeFixedTime(Callback);
		}

		if (Repetitions == 1)
		{
			Callback = new Action(() =>
			{
				try
				{
					Activity?.Invoke();
					TimesTriggered++;
				}
				catch (Exception ex) { Plugin.LogError($"Timer {delay}s has failed:", ex); }

				Destroy();
			});

			Persistence.Invoke(Callback, delay);
		}
		else
		{
			Callback = new Action(() =>
			{
				try
				{
					Activity?.Invoke();
					TimesTriggered++;

					if (TimesTriggered >= Repetitions)
					{
						Dispose();
					}
				}
				catch (Exception ex)
				{
					Plugin.LogError($"Timer {delay}s has failed:", ex);

					Destroy();
				}
			});

			Persistence.InvokeRepeating(Callback, delay, delay);
		}
	}
	public bool Destroy()
	{
		if (Destroyed) return false;
		Destroyed = true;

		if (Persistence != null)
		{
			Persistence.CancelInvoke(Callback);
			Persistence.CancelInvokeFixedTime(Callback);
		}

		if (Callback != null)
		{
			Callback = null;
		}

		return true;
	}
	public void DestroyToPool()
	{
		Destroy();
	}
	public override void Dispose()
	{
		Destroy();

		base.Dispose();
	}
}
