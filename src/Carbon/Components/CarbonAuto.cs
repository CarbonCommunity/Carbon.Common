namespace Carbon.Components;

public class CarbonAuto : API.Abstracts.CarbonAuto
{
	internal Dictionary<string, AutoVar> _autoCache = new();

	public struct AutoVar
	{
		public CarbonAutoVar Variable;
		public object ReflectionInfo;

		public readonly object GetValue()
		{
			var core = Community.Runtime.CorePlugin;

			return ReflectionInfo switch
			{
				FieldInfo field => field.IsStatic ? field.GetValue(null) : field.GetValue(core),
				PropertyInfo property => property.GetValue(core),
				_ => null
			};
		}
	}

	public static void Init()
	{
		Singleton = new CarbonAuto();
	}
	public override void Refresh()
	{
		var type = typeof(CorePlugin);
		var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
		var fields = type.GetFields(flags);
		var properties = type.GetProperties(flags);

		_autoCache.Clear();

		foreach (var field in fields)
		{
			var attribute = field.GetCustomAttribute<CarbonAutoVar>();
			if (attribute == null) continue;

			AutoVar var = default;
			var.Variable = attribute;
			var.ReflectionInfo = field;

			_autoCache.Add($"c.{attribute.Name}", var);
		}

		foreach (var property in properties)
		{
			var attribute = property.GetCustomAttribute<CarbonAutoVar>();
			if (attribute == null) continue;

			AutoVar var = default;
			var.Variable = attribute;
			var.ReflectionInfo = property;

			_autoCache.Add($"c.{attribute.Name}", var);
		}
	}
	public override bool IsForceModded()
	{
		using (TimeMeasure.New("CarbonAuto.IsChanged"))
		{
			var core = Community.Runtime.CorePlugin;

			foreach (var cache in _autoCache)
			{
				if (!cache.Value.Variable.ForceModded)
				{
					continue;
				}

				if (cache.Value.GetValue() is float and not -1) return true;
			}
		}

		return false;
	}
	public override void Save()
	{
		using (TimeMeasure.New("CarbonAuto.Save"))
		{
			try
			{
				Refresh();

				var core = Community.Runtime.CorePlugin;

				using var sb = new StringBody();

				foreach (var cache in _autoCache)
				{
					sb.Add($"{cache.Key} \"{cache.Value.GetValue()}\"");
				}

				OsEx.File.Create(Defines.GetCarbonAutoFile(), sb.ToNewLine());
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed saving Carbon auto file", ex);
			}
		}
	}
	public override void Load()
	{
		using (TimeMeasure.New("CarbonAuto.Load"))
		{
			try
			{
				Refresh();

				var file = Defines.GetCarbonAutoFile();

				if (!OsEx.File.Exists(file))
				{
					Save();
					return;
				}

				var lines = OsEx.File.ReadTextLines(Defines.GetCarbonAutoFile());
				var option = ConsoleSystem.Option.Server.Quiet();

				foreach (var line in lines)
				{
					ConsoleSystem.Run(option, line, Array.Empty<string>());
				}

				if (IsForceModded())
				{
					Logger.Warn($" The server Carbon auto options have been changed which are gameplay significant.\n" +
								$" Any values that aren't \"-1\" will force the server to modded!");
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed saving Carbon auto file", ex);
			}
		}
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CarbonAutoVar : CommandVarAttribute
{
	public bool ForceModded;

	public CarbonAutoVar(string name, string help = null, bool @protected = false, bool forceModded = true) : base(name, @protected, help)
	{
		ForceModded = forceModded;
	}
}
