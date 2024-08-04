namespace Carbon.Components;

public class TempArray<T> : IDisposable
{
	public T[] Array;

	public bool IsEmpty => Array == null || Array.Length == 0;

	public int Length => IsEmpty ? 0 : Array.Length;

	public T Get(int index, T @default = default)
	{
		return index > Array.Length - 1 ? @default : Array[index];
	}

	public static TempArray<T> New(T[] array)
	{
		return new TempArray<T>
		{
			Array = array
		};
	}

	public void Dispose()
	{
		if (Array == null)
		{
			return;
		}

		System.Array.Clear(Array, 0, Array.Length);
		Array = null;
	}
}
