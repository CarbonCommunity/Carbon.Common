﻿using System.Text;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public struct StringTable : IDisposable
{
	public IList<object> Columns { get; set; }
	public IList<object[]> Rows { get; private set; }

	public ConsoleTableOptions Options { get; private set; }

	public StringTable(params string[] columns)
		: this(new ConsoleTableOptions { Columns = columns }) { }

	public StringTable(ConsoleTableOptions options)
	{
		Options = options ?? throw new ArgumentNullException("options");
		Rows = new List<object[]>();
		Columns = new List<object>(options.Columns);
	}

	public StringTable AddColumn(params string[] names)
	{
		foreach (var name in names)
		{
			Columns.Add(name);
		}

		return this;
	}

	public StringTable AddRow(params object[] values)
	{
		if (values == null)
		{
			throw new ArgumentNullException(nameof(values));
		}

		if (!Columns.Any())
		{
			throw new Exception("Please set the columns first");
		}

		if (Columns.Count != values.Length)
		{
			throw new Exception(
				$"The number columns in the row ({Columns.Count}) does not match the values ({values.Length}");
		}

		Rows.Add(values);
		return this;
	}

	public static StringTable From<T>(params T[] values)
	{
		var table = new StringTable();

		var columns = GetColumns<T>();

		table.AddColumn(columns);

		foreach (var propertyValues in values.Select(value => columns.Select(column => GetColumnValue<T>(value, column))))
		{
			table.AddRow(propertyValues.ToArray());
		}

		return table;
	}

	public override string ToString()
	{
		return ToStringDefault();
	}

	private string ToStringNone()
	{
		using var builder = new StringBody();
		var columnLengths = ColumnLengths();
		var format = Format(columnLengths, char.MinValue);
		var temp1 = Columns.ToArray();
		var columnHeaders = string.Format(format, temp1);
		var results = Rows.Select(row => string.Format(format, row));

		builder.Add(columnHeaders);

		foreach(var item in results)
		{
			builder.Add(item);
		}

		Array.Clear(temp1, 0, temp1.Length);
		temp1 = null;
		results = null;
		columnLengths = null;
		return builder.ToNewLine();
	}

	private string ToStringDefault()
	{
		var builder = new StringBuilder();

		var columnLengths = ColumnLengths();
		var format = Enumerable.Range(0, Columns.Count)
			.Select(i => " | {" + i + ",-" + columnLengths.ElementAt(i) + "}")
			.Aggregate((s, a) => s + a) + " |";

		var temp1 = Columns.ToArray();
		var maxRowLength = Math.Max(0, Rows.Any() ? Rows.Max(row => string.Format(format, row).Length) : 0);
		var columnHeaders = string.Format(format, temp1);
		var longestLine = Math.Max(maxRowLength, columnHeaders.Length);
		var results = Rows.Select(row => string.Format(format, row));
		var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

		Array.Clear(temp1, 0, temp1.Length);
		temp1 = null;
		columnLengths = null;

		builder.AppendLine(divider);
		builder.AppendLine(columnHeaders);

		foreach (var row in results)
		{
			builder.AppendLine(divider);
			builder.AppendLine(row);
		}

		builder.AppendLine(divider);

		if (Options.EnableCount)
		{
			builder.AppendLine("");
			builder.AppendFormat(" Count: {0}", Rows.Count);
		}

		return builder.ToString();
	}

	private string ToStringMarkDown()
	{
		return ToStringMarkDown('|');
	}

	private string ToStringMarkDown(char delimiter)
	{
		var builder = new StringBuilder();

		var columnLengths = ColumnLengths();
		var format = Format(columnLengths, delimiter);
		var columnHeaders = string.Format(format, Columns.ToArray());
		var results = Rows.Select(row => string.Format(format, row));
		var divider = Regex.Replace(columnHeaders, @"[^|]", "-");

		columnLengths = null;

		builder.AppendLine(columnHeaders);
		// builder.AppendLine(divider);

		foreach(var item in results)
		{
			builder.AppendLine(item);
		}

		return builder.ToString();
	}

	public string ToStringMinimal()
	{
		return ToStringMarkDown(char.MinValue);
	}

	public string ToStringAlternative()
	{
		var builder = new StringBuilder();

		var temp1 = Columns.ToArray();
		var columnLengths = ColumnLengths();
		var format = Format(columnLengths);
		var columnHeaders = string.Format(format, temp1);
		var results = Rows.Select(row => string.Format(format, row));
		var divider = Regex.Replace(columnHeaders, @"[^|]", "-");
		var dividerPlus = divider.Replace("|", "+");

		Array.Clear(temp1, 0, temp1.Length);
		temp1 = null;
		columnLengths = null;

		builder.AppendLine(dividerPlus);
		builder.AppendLine(columnHeaders);

		foreach (var row in results)
		{
			builder.AppendLine(dividerPlus);
			builder.AppendLine(row);
		}
		builder.AppendLine(dividerPlus);

		return builder.ToString();
	}

	private string Format(IEnumerable<int> columnLengths, char delimiter = '|')
	{
		var delimiterStr = delimiter == char.MinValue ? string.Empty : delimiter.ToString();
		var format = (Enumerable.Range(0, Columns.Count)
			.Select(i => " " + delimiterStr + " {" + i + ",-" + columnLengths.ElementAt(i) + "}")
			.Aggregate((s, a) => s + a) + " " + delimiterStr).Trim();
		return format;
	}

	private IEnumerable<int> ColumnLengths()
	{
		var rows = Rows;
		var columns = Columns;

		var columnLengths = Columns
			.Select((t, i) => rows.Select(x => x[i])
				.Union(new[] { columns[i] })
				.Where(x => x != null)
				.Select(x => x.ToString().Length).Max());
		return columnLengths;
	}

	public string Write(FormatTypes format = FormatTypes.Default)
	{
		return format switch
		{
			FormatTypes.None => ToStringNone(),
			FormatTypes.Default => ToStringDefault(),
			FormatTypes.MarkDown => ToStringMarkDown(),
			FormatTypes.Alternative => ToStringAlternative(),
			FormatTypes.Minimal => ToStringMinimal(),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
		};
	}

	private static string[] GetColumns<T>()
	{
		return typeof(T).GetProperties().Select(x => x.Name).ToArray();
	}

	private static object GetColumnValue<T>(object target, string column)
	{
		return typeof(T).GetProperty(column).GetValue(target, null);
	}

	public void Dispose()
	{
		Options = null;

		Rows.Clear();
		Columns.Clear();

		Rows = null;
		Columns = null;
	}

	public class ConsoleTableOptions
	{
		public string[] Columns { get; set; } = new string[0];
		public bool EnableCount { get; set; } = true;
	}

	public enum FormatTypes
	{
		None = 0,
		Default = 1,
		MarkDown = 2,
		Alternative = 3,
		Minimal = 4,
	}
}
