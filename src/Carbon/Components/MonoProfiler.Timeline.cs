/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using Timer = Oxide.Plugins.Timer;

namespace Carbon.Components;

public partial class MonoProfiler
{
	public class TimelineRecording
	{
		public float Rate;
		public float Duration;
		public Timeline Timeline = new();
		public ProfilerArgs Args = AllFlags;

		public Action<Sample> OnSnapshot;
		public Action<bool> OnStopped;

		private bool _started;
		private DateTime _timeSinceStart;

		public double CurrentDuration => (DateTime.Now - _timeSinceStart).TotalSeconds;

		private Sample Record(AssemblyOutput assemblies, CallOutput calls)
		{
			Sample snapshot = default;
			snapshot.Assemblies = new();
			snapshot.Assemblies.AddRange(assemblies);
			snapshot.Calls = new();
			snapshot.Calls.AddRange(calls);

			Record(snapshot);
			return snapshot;
		}
		private void Record(Sample snapshot)
		{
			Timeline.Add(DateTime.Now, snapshot);
		}
		private void Clear()
		{
			foreach (var timeline in Timeline)
			{
				timeline.Value.Clear();
			}
		}

		public TimelineRecording Start(float rate, float duration, ProfilerArgs args, Action<bool> onStopped)
		{
			if (_started)
			{
				Logger.Warn("Timeline is already recording.");
				return this;
			}

			Rate = rate;
			Duration = duration;
			Args = args | ProfilerArgs.FastResume;
			OnStopped = onStopped;

			if (Recording)
			{
				ToggleProfiling(ProfilerArgs.Abort);
			}

			_timeSinceStart = DateTime.Now;
			_started = true;

			Logger.Warn("Started timeline recording..");

			Recurse(this);

			static void Recurse(TimelineRecording recording)
			{
				ToggleProfilingTimed(recording.Rate, recording.Args, _ =>
				{
					var snapshot = recording.Record(AssemblyRecords, CallRecords);
					recording.OnSnapshot?.Invoke(snapshot);

					if (recording.CurrentDuration >= recording.Duration)
					{
						recording.Stop();
						return;
					}

					Recurse(recording);
				}, false);
			}

			return this;
		}
		public void Stop(bool discard = false)
		{
			if (Recording)
			{
				ToggleProfiling(ProfilerArgs.Abort);
			}

			Logger.Warn($"Ended timeline recording.{(discard ? " Discarded." : string.Empty)}");

			if (discard)
			{
				Discard();
			}

			OnStopped?.Invoke(discard);
		}
		public void Discard()
		{
			if (Recording)
			{
				ToggleProfiling(ProfilerArgs.Abort);
			}

			Clear();
		}

		public static TimelineRecording Create(float rate, float duration, ProfilerArgs args, Action<bool> onStopped)
		{
			return new TimelineRecording().Start(rate, duration, args, onStopped);
		}
	}

	public class Timeline : Dictionary<DateTime, Sample>;

	public struct Sample
	{
		public AssemblyOutput Assemblies;
		public CallOutput Calls;

		public void Clear()
		{
			Assemblies.Clear();
			Calls.Clear();
		}
	}
}
