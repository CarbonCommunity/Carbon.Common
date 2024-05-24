/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Extensions;

public static class UnityEx
{
	public static ComponentCacheBank ComponentCache = new();

	public static T AddComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		if (go == null)
		{
			return default;
		}

		return ComponentCache.Add<T>(go);
	}

	public static T GetComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		if (go == null)
		{
			return default;
		}

		return ComponentCache.Get<T>(go);
	}

	public static bool RemoveComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		if (go == null)
		{
			return default;
		}

		return ComponentCache.Remove<T>(go);
	}

	public static bool DestroyCache(this GameObject go)
	{
		if (go == null)
		{
			return false;
		}

		if (!ComponentCache.TryGetValue(go, out var monos))
		{
			return false;
		}

		monos.Clear();
		ComponentCache.Remove(go);

		GameObject.Destroy(go);
		return true;
	}

	internal static void DestroyEntityCache(BaseEntity entity)
	{
		if (entity == null)
		{
			return;
		}

		var go = entity.gameObject;

		if (!ComponentCache.TryGetValue(go, out var monos))
		{
			return;
		}

		monos.Clear();
		ComponentCache.Remove(go);
	}

	public class ComponentCacheBank : Dictionary<GameObject, List<MonoBehaviour>>
	{
		public T Add<T>(GameObject go) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var mono = go.AddComponent<T>();

			monos.Add(mono);

			return mono;
		}

		public T Get<T>(GameObject go) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var existent = monos.FirstOrDefault(x => x is T);

			if (existent == null)
			{
				monos.Add(existent = go.GetComponent<T>());
			}

			return existent as T;
		}

		public bool Remove<T>(GameObject go) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			return monos.RemoveAll(x =>
			{
				if (x is T)
				{
					GameObject.DestroyImmediate(x);
					return true;
				}

				return false;
			}) > 0;
		}
	}
}
