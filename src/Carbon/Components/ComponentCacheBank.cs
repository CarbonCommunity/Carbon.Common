/*
 *
 * Copyright (c) 2022-2024 Carbon Community  
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public static class ComponentCacheBankNonGeneric
{
	public static List<IComponentBank> All = new();

	public static T AddComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank<T>.Instance.Add(go);
	}
	public static T GetComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank<T>.Instance.Get(go);
	}
	public static bool RemoveComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank<T>.Instance.Remove(go);
	}
	public static bool TryGetOrAddComponentCache<T>(this GameObject go, out T component) where T : MonoBehaviour
	{
		return (component = go.GetComponentCache<T>() ?? go.AddComponentCache<T>()) != null;
	}
	public static bool DestroyCache(this GameObject go)
	{
		if (go == null)
		{
			return false;
		}

		return All.Count(cache => cache.Destroy(go)) > 0;
	}

	internal static void InternalEntityDestroy(BaseEntity entity)
	{
		if (entity == null || entity.gameObject == null)
		{
			return;
		}

		All.ForEach(cache => cache.Destroy(entity.gameObject));
	}
}

public class ComponentCacheBank<T> : Dictionary<GameObject, List<T>>, IComponentBank where T : MonoBehaviour
{
	public static ComponentCacheBank<T> Instance { get; }

	static ComponentCacheBank()
	{
		Instance = new();
		ComponentCacheBankNonGeneric.All.Add(Instance);
	}

	public T Add(GameObject go)
	{
		if (!this.TryGetValue(go, out var monos))
		{
			this[go] = monos = new();
		}

		var mono = go.AddComponent<T>();

		monos.Add(mono);

		return mono;
	}

	public T Get(GameObject go)
	{
		if (!this.TryGetValue(go, out var monos))
		{
			this[go] = monos = new();
		}

		var existent = monos.FirstOrDefault(x => x is T);

		if (existent != null)
		{
			return existent as T;
		}

		if (!go.TryGetComponent(out existent))
		{
			return default;
		}

		monos.Add(existent);

		return existent as T;
	}

	public bool Remove(GameObject go, bool destroy = true)
	{
		if (!this.TryGetValue(go, out var monos))
		{
			return false;
		}

		var removed = monos.RemoveAll(x =>
		{
			if (destroy)
			{
				GameObject.DestroyImmediate(x);
			}

			return true;
		});

		return removed > 0 && base.Remove(go);
	}

	public bool Destroy(GameObject go)
	{
		if (!this.TryGetValue(go, out var monos))
		{
			GameObject.Destroy(go);
			return false;
		}

		monos.Clear();
		Remove(go, false);
		GameObject.Destroy(go);
		return true;
	}
}

public interface IComponentBank
{
	public bool Remove(GameObject go, bool destroy = true);
	public bool Destroy(GameObject go);
}
