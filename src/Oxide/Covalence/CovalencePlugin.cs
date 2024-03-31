﻿using static Oxide.Plugins.CovalencePlugin;
using Formatter = Oxide.Core.Libraries.Covalence.Formatter;
using Logger = Carbon.Logger;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Plugins
{
	public class CovalencePlugin : RustPlugin
	{
		protected string game = "Rust";

		public PlayerManager players = new();

		public new RustServer server = new();

		public struct PlayerManager
#if !NOCOVALENCE
			: IPlayerManager
#endif
		{
#if !NOCOVALENCE
			public static void RefreshDatabase(Dictionary<string, UserData> data)
			{
				_all = data.Select(x => x.AsIPlayer());

				foreach (var user in _all)
				{
					if (user.Object == null)
					{
						user.Object = BasePlayer.Find(user.Id);
					}
				}
			}

			internal static IEnumerable<IPlayer> _all;

			public IEnumerable<IPlayer> All => _all;

			public IEnumerable<IPlayer> Connected => BasePlayer.activePlayerList.Select(x => x.AsIPlayer());

			public IPlayer FindPlayer(string partialNameOrId)
			{
				return All.FirstOrDefault(x => x.Id.Contains(partialNameOrId) || x.Name.Contains(partialNameOrId, CompareOptions.OrdinalIgnoreCase));
			}

			public IPlayer FindPlayerById(string id)
			{
				return All.FirstOrDefault(x => x.Id.Contains(id));
			}

			public IPlayer FindPlayerByObj(object obj)
			{
				var value = obj?.ToString();
				if (string.IsNullOrEmpty(value)) return default;

				return FindPlayer(value);
			}

			public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
			{
				return All.Where(x => x.Id.Contains(partialNameOrId) || x.Name.Contains(partialNameOrId, CompareOptions.OrdinalIgnoreCase));
			}
#endif
		}
	}
}

namespace Oxide.Core.Libraries.Covalence
{
	public class Covalence : Library, ICovalence
	{
		public Covalence() { }

		public IPlayerManager Players { get; }
#if !NOCOVALENCE
			= new PlayerManager();
#endif
		public IServer Server { get; } = new RustServer();

		public string FormatText(string text)
		{
			return Formatter.ToUnity(text);
		}

		public void UnregisterCommand(string command, Plugin plugin)
		{
			Community.Runtime.CorePlugin.cmd.RemoveConsoleCommand(command, plugin);
		}

		public uint ClientAppId { get; } = 252490U;

		public string Game { get; } = "Rust";
	}
}

namespace Oxide.Game.Rust.Libraries.Covalence
{
	public struct RustServer : IServer
	{
		public RustServer() { }

		public string Name
		{
			get
			{
				return ConVar.Server.hostname;
			}
			set
			{
				ConVar.Server.hostname = value;
			}
		}

		public System.Net.IPAddress Address
		{
			get
			{
				System.Net.IPAddress any;
				try
				{
					if (address == null || !Core.Utility.ValidateIPv4(address.ToString()))
					{
						if (Core.Utility.ValidateIPv4(ConVar.Server.ip) && !Core.Utility.IsLocalIP(ConVar.Server.ip))
						{
							System.Net.IPAddress.TryParse(ConVar.Server.ip, out address);
							Core.Interface.Oxide.LogInfo(string.Format("IP address from command-line: {0}", address));
						}
						else
						{
							System.Net.IPAddress.TryParse(new System.Net.WebClient().DownloadString("http://api.ipify.org"), out address);
							Core.Interface.Oxide.LogInfo(string.Format("IP address from external API: {0}", address));
						}
					}
					any = address;
				}
				catch (System.Exception exception)
				{
					Logger.Error("Couldn't get server's public IP address", exception);
					any = System.Net.IPAddress.Any;
				}
				return any;
			}
		}

		public System.Net.IPAddress LocalAddress
		{
			get
			{
				System.Net.IPAddress result;
				try
				{
					System.Net.IPAddress ipaddress;
					if ((ipaddress = localAddress) == null)
					{
						ipaddress = (localAddress = Core.Utility.GetLocalIP());
					}
					result = ipaddress;
				}
				catch (System.Exception exception)
				{
					Logger.Error("Couldn't get server's local IP address", exception);
					result = System.Net.IPAddress.Any;
				}
				return result;
			}
		}

		public ushort Port
		{
			get
			{
				return (ushort)ConVar.Server.port;
			}
		}

		public string Version
		{
			get
			{
				return Facepunch.BuildInfo.Current.Build.Number;
			}
		}

		public string Protocol
		{
			get
			{
				return global::Rust.Protocol.printable;
			}
		}

		public System.Globalization.CultureInfo Language
		{
			get
			{
				return System.Globalization.CultureInfo.InstalledUICulture;
			}
		}

		public int Players
		{
			get
			{
				return BasePlayer.activePlayerList.Count;
			}
		}

		public int MaxPlayers
		{
			get
			{
				return ConVar.Server.maxplayers;
			}
			set
			{
				ConVar.Server.maxplayers = value;
			}
		}

		public System.DateTime Time
		{
			get
			{
				return TOD_Sky.Instance.Cycle.DateTime;
			}
			set
			{
				TOD_Sky.Instance.Cycle.DateTime = value;
			}
		}

		public void Ban(string id, string reason, System.TimeSpan duration = default(System.TimeSpan))
		{
			if (!IsBanned(id))
			{
				ServerUsers.Set(ulong.Parse(id), ServerUsers.UserGroup.Banned, Name, reason, -1L);
				ServerUsers.Save();
			}
		}

		public SaveInfo SaveInfo { get; } = SaveInfo.Create(World.SaveFileName);

		public System.TimeSpan BanTimeRemaining(string id)
		{
			if (!IsBanned(id))
			{
				return System.TimeSpan.Zero;
			}
			return System.TimeSpan.MaxValue;
		}

		public bool IsBanned(string id)
		{
			return ServerUsers.Is(ulong.Parse(id), ServerUsers.UserGroup.Banned);
		}

		public void Save()
		{
			ConVar.Server.save(null);
			System.IO.File.WriteAllText(ConVar.Server.GetServerFolder("cfg") + "/serverauto.cfg", ConsoleSystem.SaveToConfigString(true));
			ServerUsers.Save();
		}

		public void Unban(string id)
		{
			if (IsBanned(id))
			{
				ServerUsers.Remove(ulong.Parse(id));
				ServerUsers.Save();
			}
		}

		public void Broadcast(string message, string prefix, params object[] args)
		{
			Server.Broadcast(message, prefix, 0UL, args);
		}

		public void Broadcast(string message)
		{
			Broadcast(message, null, System.Array.Empty<object>());
		}

		public void Command(string command, params object[] args)
		{
			Server.Command(command, args);
		}

		internal readonly Game.Rust.Libraries.Server Server = new Game.Rust.Libraries.Server();

		private static System.Net.IPAddress address;

		private static System.Net.IPAddress localAddress;
	}
}
