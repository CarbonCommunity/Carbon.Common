using System.Diagnostics;
using Facepunch;
using VLB;
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

		var temp = Pool.GetList<Timer>();
		temp.AddRange(_timers);

		foreach (var timer in temp)
		{
			timer.Destroy();
		}

		Pool.FreeList(ref temp);

		_timers.Clear();
		_timers = null;
	}

	internal static TimeProcessing _processor;
	public static TimeProcessing Processor
	{
		get
		{
			if (_processor == null)
			{
				_processor = new GameObject("Timer Processor").AddComponent<TimeProcessing>();
				_processor.Init();
			}

			return _processor;
		}
	}

	public Timer In(float time, Action action)
	{
		if (!IsValid()) return null;

		var timer = Pool.Get<Timer>();
		timer.Event = action;
		timer.Plugin = Plugin;

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
			}
		});

		timer.Delay = time;
		timer.Callback = activity;

		Processor.AddTimer(timer.Process = TimeProcessing.Process.New(timer));

		_timers.Add(timer);
		return timer;
	}
	public Timer Once(float time, Action action)
	{
		return In(time, action);
	}
	public Timer Every(float time, Action action)
	{
		if (!IsValid()) return null;

		var timer = Pool.Get<Timer>();
		timer.Event = action;
		timer.Plugin = Plugin;

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
			}
		});

		timer.Delay = time;
		timer.Repetitions = int.MaxValue;
		timer.Callback = activity;

		Processor.AddTimer(timer.Process = TimeProcessing.Process.New(timer));

		_timers.Add(timer);
		return timer;
	}
	public Timer Repeat(float time, int times, Action action)
	{
		if (!IsValid()) return null;

		var timer = Pool.Get<Timer>();
		timer.Event = action;
		timer.Plugin = Plugin;

		var activity = new Action(() =>
		{
			try
			{
				action?.Invoke();
				timer.TimesTriggered++;

				if (times == 0 || timer.TimesTriggered < times) return;

				// timer.Destroy();
			}
			catch (Exception ex)
			{
				Plugin.LogError($"Timer {time}s has failed:", ex);

				timer.Destroy();
			}
		});

		timer.Delay = time;
		timer.Repetitions = times;
		timer.Callback = activity;

		Processor.AddTimer(timer.Process = TimeProcessing.Process.New(timer));

		_timers.Add(timer);
		return timer;
	}
	public void Destroy(ref Timer timer)
	{
		timer?.Destroy();
		timer = null;
	}

	public class TimeProcessing : Persistence
	{
		public List<Process> PendingQueue = new(1024);
		public Queue<Process> ExpiredQueue = new(1024);

		public static Stopwatch Watch = new();
		public static double Now => Watch.Elapsed.TotalSeconds;

		public void Init()
		{
			Watch.Restart();
		}
		public void Update()
		{
			 ProcessExpired();

			 for (int i = 0; i < ExpiredQueue.Count; i++)
			 {
				 var element = ExpiredQueue.Dequeue();

				 if (element.Timer == null)
				 {
					 continue;
				 }

				 element.Timer.Callback?.Invoke();

				 if (element.Finalized || element.Timer.Repetitions == 1)
				 {
					 continue;
				 }

				 element.UpdateNewTime();
				 PendingQueue.Add(element);
			 }
		}

		public void AddTimer(Process process)
		{
			PendingQueue.Add(process);
		}
		public void ProcessExpired()
		{
			for (int i = 0; i < PendingQueue.Count; i++)
			{
				var process = PendingQueue[i];

				if (process.Timer == null)
				{
					Dispose();
					continue;
				}

				if (!process.IsExpired()) continue;

				ExpiredQueue.Enqueue(process);
				Dispose();

				void Dispose()
				{
					PendingQueue.RemoveAt(i);
					i--;
				}
			}
		}
		public void CancelProcess(ulong id)
		{
			for (int i = 0; i < PendingQueue.Count; i++)
			{
				var process = PendingQueue[i];

				if (process.Id != id) continue;

				process.Cancel();
				PendingQueue.RemoveAt(i);
				break;
			}
		}

		public struct Process
		{
			internal static ulong CurrentId = 0;
			public static ulong NextId => CurrentId++;

			public static Process New(Timer timer)
			{
				Process process = default;
				process.Id = NextId;
				process.Timer = timer;
				process.DueTime = Now + timer.Delay;

				return process;
			}

			public ulong Id;
			public Timer Timer;
			public double DueTime;
			public bool Finalized;

			public bool IsValid() => Timer != null && !Timer.Destroyed;
			public bool IsExpired() => Now > DueTime;
			public void UpdateNewTime()
			{
				DueTime = Now + Timer.Delay;
			}
			public void Cancel()
			{
				Finalized = true;
				Timer = null;
			}
		}
	}
}

public class Timer : Library, IDisposable, Pool.IPooled
{
	public RustPlugin Plugin { get; set; }

	public Action Event { get; set; }
	public Action Callback { get; set; }
	public int Repetitions { get; set; } = 1;
	public float Delay { get; set; }
	public int TimesTriggered { get; set; }
	public bool Destroyed { get; set; }
	public Timers.TimeProcessing.Process Process { get; set; }

	public Timer() { }
	public Timer(Action @event, RustPlugin plugin = null)
	{
		Event = @event;
		Plugin = plugin;
	}

	public void Reset(float delay = -1f, int repetitions = 1)
	{
		Destroyed = false;
		Repetitions = repetitions;
		Delay = delay;

		if (Timers.Processor != null)
		{
			Process.Cancel();
			Timers.Processor.CancelProcess(Process.Id);
		}

		TimesTriggered = 0;

		Callback = () =>
		{
			try
			{
				Event?.Invoke();
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
		};

		Timers.Processor.AddTimer(Process = Timers.TimeProcessing.Process.New(this));
	}
	public bool Destroy()
	{
		if (Destroyed)
		{
			return false;
		}

		Destroyed = true;

		if (Timers.Processor != null)
		{
			Process.Cancel();
			Timers.Processor.CancelProcess(Process.Id);
		}

		if (Callback != null)
		{
			Callback = null;
		}

		Plugin?.timer?._timers.Remove(this);

		var self = this;
		Pool.Free(ref self);

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
	public void EnterPool()
	{
		Event = null;
		Callback = null;
		Repetitions = 1;
		Delay = 0;
		TimesTriggered = 0;
		Destroyed = false;
		Process = default;
	}
	public void LeavePool()
	{
	}
}
