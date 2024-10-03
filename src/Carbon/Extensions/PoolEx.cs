using System.Diagnostics;
using System.Text;

namespace Carbon.Pooling;

public class PoolEx
{
	[Obsolete("Use Rust's Facepunch.Pool system instead. Will be removed on November, 7th 2024.")]
	public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
	{
		return Facepunch.Pool.Get<Dictionary<TKey, TValue>>();
	}

	[Obsolete("Use Rust's Facepunch.Pool system instead. Will be removed on November, 7th 2024.")]
	public static void FreeDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> value)
	{
		Facepunch.Pool.FreeUnmanaged(ref value);
	}

	[Obsolete("Use Rust's Facepunch.Pool system instead. Will be removed on November, 7th 2024.")]
	public static StringBuilder GetStringBuilder()
	{
		return Facepunch.Pool.Get<StringBuilder>();
	}

	[Obsolete("Use Rust's Facepunch.Pool system instead. Will be removed on November, 7th 2024.")]
	public static void FreeStringBuilder(ref StringBuilder value)
	{
		Facepunch.Pool.FreeUnmanaged(ref value);
	}

	public static Stopwatch GetStopwatch()
	{
		return Facepunch.Pool.Get<Stopwatch>();
	}

	public static void FreeStopwatch(ref Stopwatch value)
	{
		value.Reset();
		Facepunch.Pool.FreeUnsafe(ref value);
	}

	public static void FreeRaycastHitList(ref List<RaycastHit> hitList)
	{
		Facepunch.Pool.FreeUnmanaged(ref hitList);
	}
}
