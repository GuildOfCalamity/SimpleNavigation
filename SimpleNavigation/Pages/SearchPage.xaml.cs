using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SearchPage : Page, INotifyPropertyChanged
{
    #region [Properties]

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    CancellationTokenSource? _cts;
    System.Diagnostics.Process? _process = null;

    public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private string _searchPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	public string SearchPath
	{
		get => _searchPath;
		set
		{
			_searchPath = value;
			OnPropertyChanged();
		}
	}

	private string _fileExt = "*.*";
	public string FileExt
	{
		get => _fileExt;
		set
		{
			_fileExt = value;
			OnPropertyChanged();
		}
	}

	private int _minSize = 1000000; // 1MB default
	public int MinSize
	{
		get => _minSize;
		set
		{
			_minSize = value;
			OnPropertyChanged();
		}
	}

	private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    ObservableCollection<Item> _items = new();
    public ObservableCollection<Item> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged();
        }
    }
    #endregion

    public SearchPage()
	{
		this.InitializeComponent();
        this.Loaded += (s, e) => 
		{ 
			Status = $"⇦ Click search"; 
		};
	}

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            // ⇦ ⇨ ⇧ ⇩  🡐 🡒 🡑 🡓  🡄 🡆 🡅 🡇  http://xahlee.info/comp/unicode_arrows.html
            Debug.WriteLine($"You sent '{sys.Title}'");
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"OnNavigatedTo ⇨ {sys.Title}",
                Severity = InfoBarSeverity.Informational,
            });
        }
        else
        {
            Debug.WriteLine($"Parameter is not of type '{nameof(SystemState)}'");
        }
        base.OnNavigatedTo(e);
    }

    /// <summary>
    /// <see cref="Button"/> event.
    /// </summary>
    async void SearchButton_Click(object sender, RoutedEventArgs e)
	{
        Status = $"Searching...";

        PostMessageEvent?.Invoke(this, new Message
        {
            Content = $"Search started",
            Severity = InfoBarSeverity.Informational,
        });

        IsBusy = true;

		_cts = new CancellationTokenSource(TimeSpan.FromMinutes(60)); // allow search to run for 1 hour max
		var results = await SearchAsync(SearchPath, FileExt, MinSize, _cts.Token);
		Status = $"Found {results.Count()} files.";
        if (results != null && results.Count > 0)
        {
            // Only clear the list if we have something different.
			Items.Clear();

			PostMessageEvent?.Invoke(this, new Message
			{
				Content = $"Found {results.Count()} files",
				Severity = InfoBarSeverity.Success,
			});

            var sort = results?.OrderByDescending(x => x.Time).ToList();
			if (sort != null)
				foreach (var item in sort)
					Items.Add(item);
        }
		else
		{
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"No files match the criteria",
                Severity = InfoBarSeverity.Warning,
            });
        }

        IsBusy = false;

    }

    /// <summary>
    /// <see cref="Button"/> event.
    /// </summary>
    void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		if (_cts != null)
		{
			_cts?.Cancel();
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"Cancel requested",
                Severity = InfoBarSeverity.Warning,
            });
        }
    }

	/// <summary>
	/// <see cref="ListView"/> event.
	/// </summary>
    void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            foreach (var obj in e.AddedItems)
            {
                if (obj is Item item)
                {
                    Debug.WriteLine($">> ListView selected items count: {e.AddedItems.Count}");
                    var test = System.IO.Path.GetDirectoryName(item.Content);
                    if (!string.IsNullOrEmpty(test))
                    {
                        Debug.WriteLine("Opening Explorer…");
                        try
                        {   // Close existing explorer.
                            if (_process != null)
                            {
                                if (_process.CloseMainWindow())
                                    Debug.WriteLine($"Process window close command was successful.");
                                else
                                    Debug.WriteLine($"Process window close command failed.");
                            }
                            // Open new explorer and select the file.
                            _process = System.Diagnostics.Process.Start($"explorer.exe", $"/select,\"{item.Content}\"");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"ListView_SelectionChanged: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    #region [Search Methods]
    /// <summary>
    /// Asynchronous entry point for the search methods.
    /// </summary>
    public async Task<List<Item>> SearchAsync(string path, string ext, long minSize = 0, CancellationToken token = default)
	{
		var tmp = await Task.Run(() => { return IndexFolder(path, ext, token, minSize); });

		if (tmp != null)
			return tmp;
		else
			return new List<Item>();
	}

	/// <summary>
	/// Load any existing data for the folder and begin parsing.
	/// </summary>
	public List<Item> IndexFolder(string folderPath, string ext, CancellationToken token, long minSize = 0, int maxThreads = 2)
	{
		List<Item> results = new();

		try
		{
			// You don't want to go crazy with the thread count here.
			// In most cases throwing more threads at a problem does not
			// make it better, e.g. using 20 threads may cause the process
			// to finish slightly faster but will bring the client machine
			// to its knees; 2 to 4 threads is good for most searches.
			if (maxThreads > 1)
			{
				var semaphore = new SemaphoreSlim(maxThreads, maxThreads);

				// This ignore list could be moved to a config file.
				List<string> ignores = new();
				ignores.Add(@"\.git");
				ignores.Add(@"\.vs");
				ignores.Add(@"\bin");
				ignores.Add(@"\obj");

				// If multi-threaded config, handle root folder files first.
				IndexFiles(folderPath, ref results, token, true, minSize, ext);

				// Now handle all subs.
				var dirs = Directory.GetDirectories(folderPath);
				if (dirs.Length > 0)
				{
					Debug.WriteLine($"Running {dirs.Length} tasks using {maxThreads} threads");
					int idx = 0;
					Task[] semTasks = new Task[dirs.Length];
					foreach (var dir in dirs)
					{
						if (token.IsCancellationRequested)
							break;

						semTasks[idx++] = Task.Run(() =>
						{
							//var maxSearchTime = new CancellationTokenSource(new TimeSpan(0, 5, 0));
							//SemaphoreSearch(dir, semaphore, maxSearchTime.Token, ref results);
							SemaphoreSearch(dir, semaphore, token, ref results, ignores, minSize, ext);
						});
						Thread.Sleep(10);
					}
					Debug.WriteLine("Task queue filled");

					try
					{
						Task.WaitAll(semTasks);
					}
					catch (AggregateException aex)
					{
						aex.Flatten().Handle(ex =>
						{
							Debug.WriteLine($"ExceptionType: {ex.GetType()}");
							Debug.WriteLine($"ExceptionMessage: {ex.Message}");
							return true;
						});
					}
				}
			}
			else // use single thread for everything
			{
				IndexFiles(folderPath, ref results, token, false, minSize, ext);
			}

			return results;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
			return results;
		}
	}

	/// <summary>
	/// Single-threaded method to traverse folders and files.
	/// Will read the files in this folder and all subfolders.
	/// </summary>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <remarks>This is a recursive method.</remarks>
	void IndexFiles(string path, ref List<Item> indexDB, CancellationToken token, bool rootOnly = false, long minSize = 0, string ext = "*.*")
	{
		try
		{
			// Check for "path too long".
			if (!Extensions.IsPathTooLong(path))
			{
				foreach (var fi in Directory.GetFiles(path))
				{
					if (token.IsCancellationRequested)
						break;
					try
					{
						if (!Extensions.IsPathTooLong(fi))
						{
							// Assemble file information for hashing.
							var name = Path.GetFileName(fi);
							var extension = Path.GetExtension(fi);
							var info = new FileInfo(fi);
							if (ext.Contains(".*", StringComparison.OrdinalIgnoreCase))
							{
								if (minSize > 0 && info.Length >= minSize)
								{
									indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
								}
								else if (minSize > 0 && info.Length < minSize)
								{
									//Debug.WriteLine($"Skipping '{Path.GetFileName(fi)}' due to file size filter.");
								}
								else
								{
									indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
								}
							}
							else if (extension.Contains(ext, StringComparison.OrdinalIgnoreCase))
							{
								if (minSize > 0 && info.Length >= minSize)
								{
									indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
								}
								else if (minSize > 0 && info.Length < minSize)
								{
									//Debug.WriteLine($"Skipping '{Path.GetFileName(fi)}' due to file size filter.");
								}
								else
								{
									indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
								}
							}
						}
					}
					catch (Exception ex)
					{
						// There is typically a path name involved with this error, and
						// it may be very long, so just truncate the text before writing.
						Debug.WriteLine(ex.Message);
					}
				}
			}
			else
			{
				Debug.WriteLine($"No way: '{path}'");
			}

			if (!rootOnly)
			{
				foreach (var di in Directory.GetDirectories(path))
				{
					if (token.IsCancellationRequested)
						break;

					// Recursive
					IndexFiles(di, ref indexDB, token, rootOnly, minSize, ext);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
		}
	}

	/// <summary>
	/// Indexing method to be called by multiple threads.
	/// </summary>
	void SemaphoreSearch(string dir, SemaphoreSlim semaphore, CancellationToken cts, ref List<Item> indexDB, List<string> ignores, long minSize = 0, string ext = "*.*")
	{
		var isCompleted = false;
		while (!isCompleted && !cts.IsCancellationRequested)
		{
			try
			{
				if (semaphore.Wait(5000, cts)) // Will be true if the current thread was allowed to enter the semaphore.
				{
					var currId = Task.CurrentId;
					try
					{
						// Check for intermediate files.
						var ignore = ignores.Where(i => dir.Contains(i)).Any();
						if (ignore)
						{
							Debug.WriteLine($"Ignoring '{dir}'");
						}
						else
						{
							if (!Extensions.IsPathTooLong(dir))
							{
								var results = Directory.GetFiles(dir, ext, SearchOption.AllDirectories)
									.Where(filePath => !Extensions.IsPathTooLong(filePath))
									.Select(f => f);

								foreach (var fi in results)
								{
									if (cts.IsCancellationRequested)
										break;

									ignore = ignores.Where(i => fi.Contains(i)).Any();
									if (ignore)
									{
										// We should never get here, but there could be a sitch where a "bin" folder
										// contains another "obj" folder, so just ignore these intermediates.
									}
									else
									{
										// Check the path one more time since the addition
										// of a file name could put us over the edge.
										if (!Extensions.IsPathTooLong(fi))
										{
											// Assemble file information for object creation.
											var name = Path.GetFileName(fi);
											var info = new FileInfo(fi);
											if (minSize > 0 && info.Length >= minSize)
											{
												indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
											}
											else if (minSize > 0 && info.Length < minSize)
											{
												//Debug.WriteLine($"Skipping '{Path.GetFileName(fi)}' due to file size filter.");
											}
											else
											{
												indexDB.Add(new Item { Content = fi, Attribs = info.Attributes, Size = info.Length.ToFileSize(), Time = $"{info.LastWriteTime.ToString("yyyy-MM-dd")}" });
											}
										}
										else
										{
											Debug.WriteLine($"Bad path: '{fi}'");
										}
									}
								}
							}
							else
							{
								Debug.WriteLine($"No way: {dir}");
							}
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"SemaphoreSearch: {ex.Message}");
					}
					finally
					{
						semaphore.Release();
						isCompleted = true;
						//_logQueue.Enqueue(new LogEntry { Message = $"Search task #{currId} is finished, tasks available: {semaphore.CurrentCount}", Severity = LogLevel.Verbose, Time = DateTime.Now });
						Debug.WriteLine($"Search task #{currId} is finished, tasks available: {semaphore.CurrentCount}");
					}
				}
				else // Current thread was not allowed to enter the semaphore, so keep waiting.
				{
					//Debug.WriteLine($"Still waiting for task #{Task.CurrentId} to finish.");
				}

				// Extra precaution, but the Wait should properly observe the cts.
				if (cts.IsCancellationRequested)
				{
					Debug.WriteLine($"Cancellation requested for task #{Task.CurrentId}");
					break;
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine($"Search canceled for task #{Task.CurrentId}");
				break; //throw new OperationCanceledException();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
			}
		}
	}

    #endregion
}
