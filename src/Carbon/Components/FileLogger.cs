﻿namespace Carbon;

/// <summary>
/// Base file logger class used by most logging systems of Carbon.
/// </summary>
public class FileLogger : IDisposable
{
	public string Name { get; set; } = "default";

	/// <summary>
	/// By default, each log file gets split when it reaches exactly 2.5MB in file size and sent in the archive folder.
	/// </summary>
	public int SplitSize { get; set; } = (int)(5f * 1000000f);

	public bool HasInit { get; private set; }

	internal List<string> _buffer = new();
	internal StreamWriter _file;

	public FileLogger() { }
	public FileLogger(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Initializes current file logger.
	/// </summary>
	/// <param name="archive">If true, it'll archive the existent log file before initializing this new logger instance.</param>
	/// <param name="backup">If true, it'll back up the existent log file.</param>
	public virtual void Init(bool archive = false, bool backup = false)
	{
		if (HasInit && !archive)
		{
			return;
		}

		var path = Path.Combine(Defines.GetLogsFolder(), $"{Name}.log");
		var archiveFolder = Path.Combine(Defines.GetLogsFolder(), "archive");
		var backupFailed = false;
		OsEx.Folder.Create(archiveFolder);

		if (backup && OsEx.File.Exists(path))
		{
			try
			{
				var backupPath = Path.Combine(archiveFolder, $"{Name}.backup.{DateTime.Now:yyyy.MM.dd}.log");
				var logContent = OsEx.File.ReadText(path);

				if (OsEx.File.Exists(backupPath))
				{
					File.AppendAllText(backupPath, logContent);
				}
				else
				{
					OsEx.File.Create(backupPath, logContent);
				}
			}
			catch (Exception ex)
			{
				backupFailed = true;
				Debug.LogError($"Failed backing up the current log file. Most likely because it's in use. ({ex.Message})\n{ex.StackTrace}");
			}
		}

		if (archive && !backupFailed)
		{
			if (OsEx.File.Exists(path))
			{
				OsEx.File.Move(path, Path.Combine(archiveFolder, $"{Name}.{DateTime.Now:yyyy.MM.dd.HHmmss}.log"));
			}
		}

		if (!backupFailed)
		{
			try
			{
				File.Delete(path);
			}
			catch { }
		}
		else
		{
			path = Path.Combine(Defines.GetLogsFolder(), $"{Name}_locked.log");
		}

		HasInit = true;

		_file = new StreamWriter(path, append: true);
	}

	/// <summary>
	/// File logger disposal. Flushes all changes to file, closes and terminates the file stream.
	/// </summary>
	public virtual void Dispose()
	{
		_file.Flush();
		_file.Close();
		_file.Dispose();

		HasInit = false;
	}

	/// <summary>
	/// Forcefully writes all queued logs to file.
	/// </summary>
	public virtual void Flush()
	{
		var buffer = Facepunch.Pool.Get<List<string>>();
		buffer.AddRange(_buffer);

		foreach (var line in buffer)
		{
			_file?.WriteLine(line);
		}

		_file.Flush();
		_buffer.Clear();
		Facepunch.Pool.FreeUnmanaged(ref buffer);

		if (_file.BaseStream.Length > SplitSize)
		{
			Dispose();
			Init(archive: true);
		}
	}

	/// <summary>
	/// Queues a log to the file logger buffer, pending flushing, unless the configuration specifies immediate flushing.
	/// </summary>
	/// <param name="message"></param>
	public virtual void QueueLog(object message)
	{ 
		/// Logging is disabled
		if (Community.IsConfigReady && Community.Runtime.Config.Logging.LogFileMode == 0)
		{
			return;
		}

		_buffer.Add($"[{Logger.GetDate()}] {message}");

		/// If logging allowes immediate flushing, flush
		if (Community.IsConfigReady && Community.Runtime.Config.Logging.LogFileMode == 2)
		{
			Flush();
		}
	}
}
