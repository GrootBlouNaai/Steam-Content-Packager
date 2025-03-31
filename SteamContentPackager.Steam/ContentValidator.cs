using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SteamContentPackager.Packing;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Steam;

public class ContentValidator : SubTask
{
	private readonly int _totalChunks;

	private int _processedChunks;

	private bool _aborted;

	private readonly List<FileMapping> _mappings;

	[DllImport("Adler32.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern uint ComputeHash(uint adler, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int length);

	public ContentValidator(PackageTask parentTask, IEnumerable<SteamApp.Depot> depots)
		: base(parentTask)
	{
		_mappings = GetMappings(depots);
		_totalChunks = _mappings.SelectMany((FileMapping x) => x.Chunks ?? new List<ChunkData>()).Count();
	}

	private List<FileMapping> GetMappings(IEnumerable<SteamApp.Depot> depots)
	{
		Dictionary<string, FileMapping> dictionary = new Dictionary<string, FileMapping>();
		foreach (SteamApp.Depot depot in depots)
		{
			Manifest manifest = new Manifest($"{ParentTask.AppConfig.LibraryFolder}\\depotcache\\{depot.Id}_{depot.ManifestId}.manifest", depot.Id);
			foreach (FileMapping file in manifest.Files)
			{
				dictionary[file.FileName.ToLower()] = file;
			}
		}
		return dictionary.Values.ToList();
	}

	public override async Task<Result> Run()
	{
		await Task.Run(delegate
		{
			try
			{
				string text = $"{ParentTask.AppConfig.LibraryFolder}\\steamapps\\common\\{ParentTask.AppConfig.AppInstallDir}\\";
				Log.Write($"Library Folder: {ParentTask.AppConfig.LibraryFolder}");
				Log.Write($"Install Dir: {text}");
				if (!Directory.Exists(text))
				{
					return;
				}
				Log.Write("Validating existing files");
				foreach (FileMapping mapping in _mappings)
				{
					FileInfo fileInfo = new FileInfo($"{text}{mapping.FileName}");
					if (!File.Exists(fileInfo.FullName))
					{
						_processedChunks += mapping.Chunks?.Count ?? 0;
						ParentTask.Progress = (float)_totalChunks / (float)_processedChunks * 100f;
					}
					else
					{
						using FileStream fileStream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
						if (mapping.Chunks == null)
						{
							mapping.Valid = fileStream.Length == 0;
							fileStream.Close();
						}
						else
						{
							if (fileStream.Length != (long)mapping.Size)
							{
								fileStream.SetLength((long)mapping.Size);
							}
							foreach (ChunkData item in mapping.Chunks?.OrderBy((ChunkData x) => x.Offset))
							{
								byte[] array = new byte[item.Size];
								CheckPaused();
								if (CheckCancelledOrAborted())
								{
									break;
								}
								try
								{
									fileStream.Read(array, 0, array.Length);
								}
								catch (Exception)
								{
									_processedChunks++;
									continue;
								}
								uint num = ComputeHash(0u, array, array.Length);
								item.Valid = num == item.Adler32;
								item.ParentMapping.Valid = item.ParentMapping.Chunks.All((ChunkData x) => x.Valid);
								_processedChunks++;
								ParentTask.Progress = (float)_totalChunks / (float)_processedChunks * 100f;
							}
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Log.WriteException(ex2);
				Log.Write($"Validation failed: {ex2.Message}");
				_aborted = true;
			}
		});
		ValidationResult result = new ValidationResult
		{
			InvalidFiles = _mappings.Where((FileMapping x) => !x.Valid).ToList(),
			Success = !CheckCancelledOrAborted(),
			TimeElapsed = TimeSpan.Zero
		};
		if (result.Success)
		{
			Log.Write("Validation completed");
		}
		return result;
	}

	private void CheckPaused()
	{
		while (ParentTask.State == TaskState.Paused && !ParentTask.CancellationTokenSource.IsCancellationRequested)
		{
			Thread.Sleep(100);
		}
	}

	private bool CheckCancelledOrAborted()
	{
		if (ParentTask.CancellationTokenSource.IsCancellationRequested || _aborted)
		{
			return true;
		}
		return false;
	}
}
