﻿using System.Data.Common;

namespace Oxide.Core.Database;

public class Connection
{
	public string ConnectionString;
	public bool ConnectionPersistent;
	public DbConnection Con;
	public Plugin Plugin;
	public long LastInsertRowId;

	public Connection(string connection, bool persistent)
	{
		ConnectionString = connection;
		ConnectionPersistent = persistent;
	}
}
