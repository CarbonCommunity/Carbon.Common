using System.Diagnostics;
using System.Text;

namespace Carbon.Pooling;

public class PoolEx
{
	public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
	{
		return Facepunch.Pool.Get<Dictionary<TKey, TValue>>();
	}

	public static void FreeDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> value)
	{
		Facepunch.Pool.FreeUnmanaged(ref value);
	}

	public static StringBuilder GetStringBuilder()
	{
		return Facepunch.Pool.Get<StringBuilder>();
	}

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
}
