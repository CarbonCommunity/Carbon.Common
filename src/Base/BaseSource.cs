/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Base;

public class BaseSource : IDisposable, ISource
{
	public string ContextFilePath { get; set; }
	public string ContextFileName { get; set; }
	public string FilePath { get; set; }
	public string FileName { get; set; }
	public string Content { get; set; }

	public void Dispose()
	{
		ContextFilePath = null;
		ContextFileName = null;
		FilePath = null;
		FileName = null;
		Content = null;
	}
}
