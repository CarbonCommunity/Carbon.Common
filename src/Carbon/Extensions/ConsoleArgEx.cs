﻿namespace Carbon.Extensions;

public static class ConsoleArgEx
{
	public static char[] CommandSpacing = new char[] { ' ' };

	public static bool IsPlayerCalledOrAdmin(this ConsoleSystem.Arg arg)
	{
		return arg.Player() == null || arg.IsAdmin;
	}
}
