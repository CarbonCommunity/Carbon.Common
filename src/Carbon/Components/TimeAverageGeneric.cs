/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public abstract class TimeAverageGeneric<T> : Dictionary<double, T>
{
	public TimeAverageGeneric (double time)
	{
		Time = time;
	}

	public double Time { get; set; }

	public virtual void Increment(T value)
	{
		Calibrate();

		var time = Mathf.RoundToInt(UnityEngine.Time.realtimeSinceStartup);

		if (!ContainsKey(time))
		{
			Add(time, value);
		}
	}
	public virtual void Calibrate()
	{
		if (Count == 0)
		{
			return;
		}

		var instance = this.ElementAt(0);
		var timePassed = UnityEngine.Time.realtimeSinceStartup - instance.Key;

		if (timePassed >= Time)
		{
			Remove(instance.Key);
		}
	}

	public abstract T CalculateTotal();
	public abstract T CalculateAverage();
	public abstract T CalculateMin();
	public abstract T CalculateMax();
}

public class HookTimeAverage : TimeAverageGeneric<double>
{
	public HookTimeAverage(double time) : base(time) { }

	public override double CalculateTotal()
	{
		return Count > 0 ? this.Sum(x => x.Value) : 0;
	}
	public override double CalculateAverage()
	{
		return Count > 0 ? this.Average(x => x.Value) : 0;
	}
	public override double CalculateMin()
	{
		return Count > 0 ? this.Min(x => x.Value) : 0;
	}
	public override double CalculateMax()
	{
		return Count > 0 ? this.Max(x => x.Value) : 0;
	}
}

public class MemoryAverage : TimeAverageGeneric<long>
{
	public MemoryAverage(double time) : base(time) { }

	public override long CalculateTotal()
	{
		return Count > 0 ? this.Sum(x => x.Value) : 0;
	}
	public override long CalculateAverage()
	{
		return Count > 0 ? (long)this.Average(x => x.Value) : 0;
	}
	public override long CalculateMin()
	{
		return Count > 0 ? this.Min(x => x.Value) : 0;
	}
	public override long CalculateMax()
	{
		return Count > 0 ? this.Max(x => x.Value) : 0;
	}
}
