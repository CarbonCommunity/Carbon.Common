/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon;

public class Entities : IDisposable
{
	public static void Init()
	{
		try
		{
			Community.Runtime.Entities?.Dispose();

			foreach (var type in _findAssignablesFrom<BaseEntity>())
			{
				Mapping.Add(type, new List<object>(100000));
			}

			if (Community.IsServerInitialized)
			{
				Carbon.Logger.Warn($"Mapping {BaseNetworkable.serverEntities.Count:n0} entities... This will take a while.");
			}

			using (TimeMeasure.New("Entity mapping"))
			{
				foreach (var type in Mapping)
				{
					type.Value.AddRange(BaseNetworkable.serverEntities.Where(x => x.GetType() == type.Key).Select(x => x as BaseEntity));
				}
			}

			if (Community.IsServerInitialized)
			{
				Carbon.Logger.Warn($"Done mapping.");
			}
		}
		catch (Exception ex) { Carbon.Logger.Error($"Failed Entities.Init()", ex); }
	}

	public void Dispose()
	{
		foreach (var map in Mapping)
		{
			map.Value.Clear();
		}

		Mapping.Clear();
	}

	public static Dictionary<Type, List<object>> Mapping { get; internal set; } = new();

	internal static IEnumerable<Type> _findAssignablesFrom<TBaseType>()
	{
		var baseType = typeof(TBaseType);
		var assembly = baseType.Assembly;

		return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
	}

	public static Map<T> Get<T>(bool inherited = false)
	{
		var map = new Map<T>
		{
			Pool = Facepunch.Pool.GetList<T>()
		};

		if (inherited)
		{
			foreach (var entry in Mapping)
			{
				if (typeof(T).IsAssignableFrom(entry.Key))
				{
					foreach (T entity in entry.Value)
					{
						map.Pool.Add(entity);
					}
				}
			}
		}
		else
		{
			if (Mapping.TryGetValue(typeof(T), out var mapping))
			{
				foreach (var entity in mapping)
				{
					if (entity is T result) map.Pool.Add(result);
				}
			}
		}

		return map;
	}
	public static Map<BaseEntity> GetAll(bool inherited = false)
	{
		var map = new Map<BaseEntity>
		{
			Pool = Facepunch.Pool.GetList<BaseEntity>()
		};

		if (inherited)
		{
			foreach (var entry in Mapping)
			{
				if (typeof(BaseEntity).IsAssignableFrom(entry.Key))
				{
					foreach (var entity in entry.Value)
					{
						map.Pool.Add(entity as BaseEntity);
					}
				}
			}
		}
		else
		{
			if (Mapping.TryGetValue(typeof(BaseEntity), out var mapping))
			{
				foreach (var entity in mapping)
				{
					if (entity is BaseEntity result) map.Pool.Add(result);
				}
			}
		}

		return map;
	}
	public static T GetOne<T>(bool inherited = false)
	{
		using (var map = Get<T>(inherited))
		{
			return map.Pool.FirstOrDefault();
		}
	}
	public static void AddMap(BaseEntity entity)
	{
		if (!Mapping.TryGetValue(entity.GetType(), out var map))
		{
			return;
			// EntityMapping.Add(entity.GetType(), map = new List<BaseEntity> { entity });
		}

		map.Add(entity);
	}
	public static void RemoveMap(BaseEntity entity)
	{
		if (!Mapping.TryGetValue(entity.GetType(), out var map))
		{
			return;
		}

		map.Remove(entity);
		UnityEx.InternalEntityDestroy(entity);
	}

	public struct Map<T> : IDisposable
	{
		public List<T> Pool;

		public Map<T> Each(Action<T> callback, Func<T, bool> condition = null)
		{
			foreach (var drop in Pool)
			{
				if (condition != null && !condition(drop)) continue;

				callback.Invoke(drop);
			}

			return this;
		}
		public T Pick(int index)
		{
			if (Pool.Count == 0)
			{
				return default;
			}

			if (Pool.Count - 1 > index)
			{
				return default;
			}

			return Pool[index];
		}

		public void Dispose()
		{
#if DEBUG
			Logger.Debug($"Cleaned {typeof(T).Name}", 2);
#endif
			Facepunch.Pool.FreeList(ref Pool);
		}
	}
}
