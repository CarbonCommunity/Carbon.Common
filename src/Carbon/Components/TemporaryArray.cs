/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public class TemporaryArray<T> : IDisposable
{
	public T[] Array;

	public static TemporaryArray<T> New(T[] array)
	{
		return new TemporaryArray<T>
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
