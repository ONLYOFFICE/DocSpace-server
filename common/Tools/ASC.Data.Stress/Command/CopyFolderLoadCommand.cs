// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Data.Stress.Command;

/// <summary>
/// The "copy a folder N times" load test (docspace_copy_loadtest_v2.sh) as a command: PARALLEL copy operations
/// at a time with no pause between them, one CSV row per finished iteration with the real number of copies in the
/// destination and the memory at that moment, a stop after MAX_FAILS network failures or 5xx in a row, and at the
/// end a full-text check that the control word is found once per copy.
/// </summary>
public class CopyFolderLoadCommand : AsyncCommand<CopyFolderLoadCommand.Settings>, IBaseCommand
{
    public static string Name => "copy-folder-load";
    public static string Description => "Copies a folder over and over and records memory after every iteration (load test)";

    private const string DefaultSourceName = "Samples Document 2025 for editors";
    private static readonly CultureInfo _inv = CultureInfo.InvariantCulture;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public class Settings : CommandSettings
    {
        [CommandOption("--portal")]
        [Description("Portal URL; the local Aspire router by default")]
        public string Portal { get; set; } = ApiHelper.DefaultBasePath;

        [CommandOption("--api-key")]
        [Description("API key (Bearer). Falls back to the DOCSPACE_TOKEN variable, then to --email/--password")]
        public string? ApiKey { get; set; }

        [CommandOption("--email")]
        public string? Email { get; set; }

        [CommandOption("--password")]
        public string? Password { get; set; }

        [CommandOption("--source-folder-id")]
        [Description("Folder to copy. Without it a folder is created in My documents from the files of --source-dir")]
        public int SourceFolderId { get; set; }

        [CommandOption("--source-dir")]
        [Description("Local directory whose files (docx, xlsx, pptx, pdf form...) are uploaded into the new source folder")]
        public string? SourceDir { get; set; }

        [CommandOption("--source-name")]
        [Description("Title of the created source folder")]
        public string? SourceName { get; set; }

        [CommandOption("--dest-folder-id")]
        [Description("Where to copy; copies are counted as its subfolders above the start count. Without it a new folder is created in My documents")]
        public int DestFolderId { get; set; }

        [CommandOption("--iterations")]
        public int Iterations { get; set; } = 1000;

        [CommandOption("--start-iteration")]
        public int StartIteration { get; set; } = 1;

        [CommandOption("--parallel")]
        public int Parallel { get; set; } = 4;

        [CommandOption("--poll-interval")]
        [Description("Seconds between two polls of an operation")]
        public double PollInterval { get; set; } = 0.5;

        [CommandOption("--operation-timeout")]
        [Description("Seconds one copy operation may take")]
        public int OperationTimeout { get; set; } = 300;

        [CommandOption("--request-timeout")]
        [Description("Seconds one HTTP request may take")]
        public int RequestTimeout { get; set; } = 30;

        [CommandOption("--max-fails")]
        [Description("Network failures or 5xx in a row after which the server counts as not responding")]
        public int MaxFails { get; set; } = 5;

        [CommandOption("--search-text")]
        [Description("Control word from the content of the source files for the full-text check at the end")]
        public string? SearchText { get; set; }

        [CommandOption("--search-timeout")]
        [Description("Seconds to wait for the search index to catch up with the copies")]
        public int SearchTimeout { get; set; } = 300;

        [CommandOption("--processes")]
        [Description("Comma-separated service process names whose memory goes into the CSV")]
        public string Processes { get; set; } = "ASC.Files,ASC.Files.Worker,ASC.Web.Api,ASC.People,ASC.Web.Studio";

        [CommandOption("--out")]
        [Description("CSV file; copy-load_<utc>.csv in the current directory by default")]
        public string? Out { get; set; }
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (settings.SourceFolderId == 0 && string.IsNullOrEmpty(settings.SourceDir))
        {
            return ValidationResult.Error("Pass --source-folder-id, or --source-dir to upload the source files into a new folder");
        }

        if (!string.IsNullOrEmpty(settings.SourceDir) && !Directory.Exists(settings.SourceDir))
        {
            return ValidationResult.Error($"Directory {settings.SourceDir} does not exist");
        }

        if (settings.Parallel < 1 || settings.Iterations < settings.StartIteration)
        {
            return ValidationResult.Error("--parallel must be positive and --iterations not less than --start-iteration");
        }

        settings.ApiKey ??= Environment.GetEnvironmentVariable("DOCSPACE_TOKEN");

        if (string.IsNullOrEmpty(settings.ApiKey))
        {
            if (string.IsNullOrEmpty(settings.Email))
            {
                settings.Email = AnsiConsole.Ask("Enter user [green]email[/]:", "test@onlyoffice.com");
            }

            if (string.IsNullOrEmpty(settings.Password))
            {
                settings.Password = AnsiConsole.Ask("Enter user [green]password[/]:", "11111111");
            }
        }

        return ValidationResult.Success();
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var portal = settings.Portal.TrimEnd('/');
        var timeout = TimeSpan.FromSeconds(settings.RequestTimeout);

        var configuration = string.IsNullOrEmpty(settings.ApiKey)
            ? await ApiHelper.GetConfigurationAsync(settings.Email!, settings.Password!, portal)
            : ApiHelper.GetConfiguration(settings.ApiKey, portal);
        configuration.Timeout = timeout;

        // one client per API for the whole run: every SDK API instance owns its HttpClient
        using var foldersApi = new FoldersApi(configuration);
        using var operationsApi = new OperationsApi(configuration);

        // the copy itself goes without the SDK: 4.0.0 has no returnSingleOperation in BatchRequestDto
        using var httpClient = new HttpClient { BaseAddress = new Uri(portal + "/"), Timeout = timeout };
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(configuration.DefaultHeaders["Authorization"]);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        await using var run = new LoadRun(settings, foldersApi, operationsApi, httpClient, cancellationToken);

        return await run.ExecuteAsync();
    }

    private sealed class LoadRun(Settings settings, FoldersApi foldersApi, OperationsApi operationsApi, HttpClient httpClient, CancellationToken stopToken) : IAsyncDisposable
    {
        private readonly string[] _processes = settings.Processes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        private readonly Lock _csvLock = new();
        private readonly List<Row> _rows = [];
        private int _consecutiveFails;
        private int _sourceFolderId = settings.SourceFolderId;
        private int _destFolderId = settings.DestFolderId;
        private string _sourceName = settings.SourceName ?? DefaultSourceName;
        private int _filesPerCopy;
        private int _baseTotal;
        private StreamWriter? _csv;

        public ValueTask DisposeAsync() => _csv?.DisposeAsync() ?? ValueTask.CompletedTask;

        public async Task<int> ExecuteAsync()
        {
            await PrepareFoldersAsync();

            var baseTotal = await CountCopiesAsync();
            if (baseTotal is null)
            {
                AnsiConsole.MarkupLine($"[red]Cannot read the content of folder {_destFolderId}: check the token and the id[/]");
                return 1;
            }

            _baseTotal = baseTotal.Value;

            var outPath = Path.GetFullPath(settings.Out ?? $"copy-load_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            _csv = new StreamWriter(outPath, false, new UTF8Encoding(false)) { AutoFlush = true };

            AnsiConsole.MarkupLine($"Source [green]{_sourceFolderId}[/] '{Markup.Escape(_sourceName)}' ({_filesPerCopy} files) -> destination [green]{_destFolderId}[/], copies before start: {_baseTotal}");
            AnsiConsole.MarkupLine($"CSV: [green]{Markup.Escape(outPath)}[/]");

            var header = "iter,time_utc,status,real_copies,real_files_copied,mem_free_mb,mem_available_mb,duration_s,copy_request_ms,polls"
                         + string.Concat(_processes.Select(p => $",{p}_ws_mb,{p}_private_mb"));
            WriteLine(header);
            WriteRow(new Row("0", "baseline", 0, 0, 0, MemorySampler.Take(_processes)) { Copies = 0 });

            var started = Stopwatch.StartNew();
            await RunIterationsAsync();

            // the original waits 10 s for the tail of the operations before the final count
            await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);

            var final = await CountCopiesAsync();
            var real = (final ?? _baseTotal) - _baseTotal;
            WriteRow(new Row("FINISH", "done", started.Elapsed.TotalSeconds, 0, 0, MemorySampler.Take(_processes)) { Copies = real });

            await CheckFullTextAsync(real);
            PrintSummary(real, outPath);

            return 0;
        }

        private async Task PrepareFoldersAsync()
        {
            int? myFolderId = null;

            async Task<int> GetMyFolderAsync()
            {
                if (myFolderId is null)
                {
                    var roots = (await foldersApi.GetRootFoldersAsync(cancellationToken: stopToken)).Response;
                    myFolderId = roots.First(r => r.Current.RootFolderType is FolderType.USER).Current.Id;
                }

                return myFolderId.Value;
            }

            if (_sourceFolderId == 0)
            {
                var folder = (await foldersApi.CreateFolderAsync(await GetMyFolderAsync(), new CreateFolderRequest(_sourceName), stopToken)).Response;
                _sourceFolderId = folder.Id;

                var files = Directory.GetFiles(settings.SourceDir!);
                await AnsiConsole.Progress().StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"Upload {files.Length} files to '{Markup.Escape(_sourceName)}'", maxValue: files.Length);
                    foreach (var path in files)
                    {
                        await using var stream = File.OpenRead(path);
                        await foldersApi.UploadFileAsync(_sourceFolderId, file: new FileParameter(Path.GetFileName(path), stream), cancellationToken: stopToken);
                        task.Increment(1);
                    }
                });
            }
            else
            {
                _sourceName = settings.SourceName ?? (await foldersApi.GetFolderInfoAsync(_sourceFolderId, stopToken)).Response.Title;
            }

            if (_destFolderId == 0)
            {
                var title = $"copy-load {DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}";
                _destFolderId = (await foldersApi.CreateFolderAsync(await GetMyFolderAsync(), new CreateFolderRequest(title), stopToken)).Response.Id;
            }

            _filesPerCopy = (await foldersApi.GetFolderByFolderIdAsync(_sourceFolderId, filterType: FilterType.FilesOnly, withSubFolders: true, count: 1, cancellationToken: stopToken)).Response.Total;
        }

        private async Task RunIterationsAsync()
        {
            using var slots = new SemaphoreSlim(settings.Parallel);
            var running = new List<Task>();

            for (var i = settings.StartIteration; i <= settings.Iterations; i++)
            {
                if (Volatile.Read(ref _consecutiveFails) >= settings.MaxFails)
                {
                    WriteRow(new Row("END", $"server_not_responding_after_{settings.MaxFails}_fails", 0, 0, 0, MemorySampler.Take(_processes)));
                    break;
                }

                try
                {
                    await slots.WaitAsync(stopToken);
                }
                catch (OperationCanceledException)
                {
                    WriteRow(new Row("END", "stopped_by_user", 0, 0, 0, MemorySampler.Take(_processes)));
                    break;
                }

                var iteration = i;
                running.Add(Task.Run(async () =>
                {
                    try
                    {
                        await RunIterationAsync(iteration);
                    }
                    finally
                    {
                        slots.Release();
                    }
                }, CancellationToken.None));

                running.RemoveAll(t => t.IsCompleted);
            }

            await Task.WhenAll(running);
        }

        private async Task RunIterationAsync(int iteration)
        {
            var watch = Stopwatch.StartNew();
            var polls = 0;

            // the original pipeline: the PUT, then polling until the operation reports finished
            var (started, code) = await CallAsync(StartCopyAsync);
            var copyRequestMs = watch.Elapsed.TotalMilliseconds;

            if (started is null)
            {
                await LogAsync(iteration, $"copy_http_{code:000}", watch, copyRequestMs, polls);
                return;
            }

            if (started.Id.Length == 0)
            {
                await LogAsync(iteration, "not_started", watch, copyRequestMs, polls);
                return;
            }

            // a finished operation is reported once and dropped, so a copy already done in the PUT answer is final here
            if (started.Finished)
            {
                await LogAsync(iteration, string.IsNullOrEmpty(started.Error) ? "ok" : $"op_error:{started.Error.Replace(',', ';')}", watch, copyRequestMs, polls);
                return;
            }

            var opId = started.Id;

            var deadline = DateTime.UtcNow.AddSeconds(settings.OperationTimeout);
            while (true)
            {
                if (DateTime.UtcNow > deadline)
                {
                    await LogAsync(iteration, "op_timeout", watch, copyRequestMs, polls);
                    return;
                }

                polls++;

                // GET fileops?id=: narrowed to this operation, so a parallel poller never consumes it (a finished
                // operation is reported once and then dropped) - the v2 script read the whole list each time
                var (statuses, pollCode) = await CallAsync(() => operationsApi.GetOperationStatusesAsync(opId, CancellationToken.None));
                if (statuses is not null)
                {
                    var op = statuses.Response?.FirstOrDefault(o => o.Id == opId);
                    if (op is null)
                    {
                        await LogAsync(iteration, "gone_unverified", watch, copyRequestMs, polls);
                        return;
                    }

                    if (op.Finished)
                    {
                        var status = string.IsNullOrEmpty(op.Error) ? "ok" : $"op_error:{op.Error.Replace(',', ';')}";
                        await LogAsync(iteration, status, watch, copyRequestMs, polls);
                        return;
                    }
                }
                else if (IsNetworkFail(pollCode))
                {
                    await LogAsync(iteration, $"poll_http_{pollCode:000}", watch, copyRequestMs, polls);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(settings.PollInterval), CancellationToken.None);
            }
        }

        /// <summary>
        /// PUT fileops/copy with returnSingleOperation=true. Without it the answer lists every operation of the user,
        /// and since a finished operation is reported once and then dropped, the PUT of one iteration consumed the
        /// finished copy of another: a smoke run had 5 of 20 iterations end as gone_unverified and 1 as not_started
        /// while all 20 copies were made. The v2 script has the same race.
        /// </summary>
        private async Task<StartedOperation> StartCopyAsync()
        {
            var body = new CopyRequest([_sourceFolderId], [], _destFolderId, (int)FileConflictResolveType.Duplicate, false, true);

            using var response = await httpClient.PutAsJsonAsync("api/2.0/files/fileops/copy", body, _json, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException((int)response.StatusCode, $"PUT fileops/copy: {(int)response.StatusCode}");
            }

            var answer = await response.Content.ReadFromJsonAsync<OperationsAnswer>(_json, CancellationToken.None);
            var op = answer?.Response?.FirstOrDefault();

            return new StartedOperation(op?.Id ?? "", op?.Finished ?? false, op?.Error);
        }

        /// <summary>Runs one request and keeps the fail streak: a network failure or 5xx adds one, any answer resets it.</summary>
        private async Task<(T? Result, int Code)> CallAsync<T>(Func<Task<T>> call) where T : class
        {
            int code;
            try
            {
                var result = await call();
                Interlocked.Exchange(ref _consecutiveFails, 0);
                return (result, 200);
            }
            catch (ApiException e)
            {
                code = e.ErrorCode;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                code = 0;
            }

            if (IsNetworkFail(code))
            {
                Interlocked.Increment(ref _consecutiveFails);
            }
            else
            {
                Interlocked.Exchange(ref _consecutiveFails, 0);
            }

            return (null, code);
        }

        private static bool IsNetworkFail(int code) => code is 0 or >= 500;

        private async Task LogAsync(int iteration, string status, Stopwatch watch, double copyRequestMs, int polls)
        {
            var duration = watch.Elapsed.TotalSeconds;
            var total = await CountCopiesAsync();
            var row = new Row(iteration.ToString(_inv), status, duration, copyRequestMs, polls, MemorySampler.Take(_processes))
            {
                Copies = total - _baseTotal
            };

            WriteRow(row);
        }

        /// <summary>
        /// Subfolders of the destination, read from the database. Not by title: with a filterValue the listing goes
        /// through OpenSearch, which lags the copies by seconds (a smoke run counted 16 of 20 copies 10 s after the
        /// last one), and parallel copies can even get the same "name (N)" title.
        /// </summary>
        private async Task<int?> CountCopiesAsync()
        {
            try
            {
                var content = await foldersApi.GetFolderByFolderIdAsync(_destFolderId, filterType: FilterType.FoldersOnly, count: 1, cancellationToken: CancellationToken.None);
                return content.Response.Total;
            }
            catch (Exception e) when (e is ApiException or HttpRequestException or TaskCanceledException)
            {
                return null;
            }
        }

        private async Task CheckFullTextAsync(int copies)
        {
            if (string.IsNullOrEmpty(settings.SearchText))
            {
                return;
            }

            // matches per copy = matches in the source itself, so the expected total holds for any control word
            var perCopy = await SearchTotalAsync(_sourceFolderId) ?? 0;
            var expected = copies * perCopy;
            var watch = Stopwatch.StartNew();
            int? found;

            do
            {
                found = await SearchTotalAsync(_destFolderId);
                if (found >= expected)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            while (watch.Elapsed.TotalSeconds < settings.SearchTimeout);

            var color = found == expected ? "green" : "red";
            AnsiConsole.MarkupLine($"Full-text '{Markup.Escape(settings.SearchText)}': found [{color}]{found?.ToString(_inv) ?? "error"}[/], expected {expected} ({perCopy} per copy x {copies} copies), waited {watch.Elapsed.TotalSeconds:F0} s");
            WriteLine($"SEARCH,{DateTime.UtcNow.ToString("s", _inv)},{(found == expected ? "ok" : "mismatch")},{found},{expected},,,{watch.Elapsed.TotalSeconds.ToString("F1", _inv)},,");
        }

        private async Task<int?> SearchTotalAsync(int folderId)
        {
            try
            {
                var content = await foldersApi.GetFolderByFolderIdAsync(folderId, filterType: FilterType.FilesOnly, withSubFolders: true, filterValue: settings.SearchText, count: 1, cancellationToken: CancellationToken.None);
                return content.Response.Total;
            }
            catch (Exception e) when (e is ApiException or HttpRequestException or TaskCanceledException)
            {
                return null;
            }
        }

        private void WriteRow(Row row)
        {
            var memory = row.Memory;
            var sb = new StringBuilder()
                .Append(row.Iteration).Append(',')
                .Append(DateTime.UtcNow.ToString("s", _inv)).Append(',')
                .Append(row.Status).Append(',')
                .Append(row.Copies?.ToString(_inv)).Append(',')
                .Append((row.Copies * _filesPerCopy)?.ToString(_inv)).Append(',')
                .Append(memory.FreeMb?.ToString(_inv)).Append(',')
                .Append(memory.AvailableMb?.ToString(_inv)).Append(',')
                .Append(row.DurationSeconds.ToString("F1", _inv)).Append(',')
                .Append(row.CopyRequestMs.ToString("F0", _inv)).Append(',')
                .Append(row.Polls.ToString(_inv));

            foreach (var process in memory.Processes)
            {
                sb.Append(',').Append(process.Count > 0 ? process.WorkingSetMb.ToString(_inv) : "")
                  .Append(',').Append(process.Count > 0 ? process.PrivateMb.ToString(_inv) : "");
            }

            lock (_csvLock)
            {
                _rows.Add(row);
                WriteLine(sb.ToString());
            }
        }

        private void WriteLine(string line)
        {
            lock (_csvLock)
            {
                _csv!.WriteLine(line);
                AnsiConsole.WriteLine(line);
            }
        }

        private void PrintSummary(int copies, string outPath)
        {
            var iterations = _rows.Where(r => int.TryParse(r.Iteration, out var n) && n > 0).ToList();
            var ok = iterations.Where(r => r.Status == "ok").Select(r => r.DurationSeconds).Order().ToList();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]===== Summary =====[/]");
            AnsiConsole.MarkupLine($"Iterations started: {iterations.Count}");

            foreach (var group in iterations.GroupBy(r => r.Status).OrderByDescending(g => g.Count()))
            {
                AnsiConsole.MarkupLine($"  {Markup.Escape(group.Key),-30} {group.Count()}");
            }

            AnsiConsole.MarkupLine($"Copies really created: {copies} (files: {copies * _filesPerCopy})");

            if (ok.Count > 0)
            {
                AnsiConsole.MarkupLine($"Iteration duration, s: p50 {Percentile(ok, 0.5):F1}, p95 {Percentile(ok, 0.95):F1}, max {ok[^1]:F1}");
            }

            // step 8 of the scenario: memory growth over the run - start, end, peak and the trend per 100 iterations
            var table = new Table().AddColumns("Memory, MB", "start", "end", "max", "trend / 100 iter");
            var baseline = _rows.FirstOrDefault(r => r.Iteration == "0")?.Memory;

            AddTrend(table, "machine available", baseline?.AvailableMb, iterations, r => r.Memory.AvailableMb);
            for (var p = 0; p < _processes.Length; p++)
            {
                var index = p;
                AddTrend(table, $"{_processes[p]} working set", baseline?.Processes[p].WorkingSetMb, iterations,
                    r => r.Memory.Processes[index].Count > 0 ? r.Memory.Processes[index].WorkingSetMb : null);
                AddTrend(table, $"{_processes[p]} private", baseline?.Processes[p].PrivateMb, iterations,
                    r => r.Memory.Processes[index].Count > 0 ? r.Memory.Processes[index].PrivateMb : null);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"CSV: [green]{Markup.Escape(outPath)}[/]");
        }

        private static void AddTrend(Table table, string name, long? start, List<Row> rows, Func<Row, long?> value)
        {
            var points = rows
                .Select(r => (X: double.Parse(r.Iteration, _inv), Y: value(r)))
                .Where(p => p.Y is not null)
                .Select(p => (p.X, Y: (double)p.Y!.Value))
                .OrderBy(p => p.X)
                .ToList();

            if (points.Count == 0)
            {
                return;
            }

            // least-squares slope: a steady climb shows up even under GC sawtooth
            var meanX = points.Average(p => p.X);
            var meanY = points.Average(p => p.Y);
            var sxx = points.Sum(p => (p.X - meanX) * (p.X - meanX));
            var slope = sxx == 0 ? 0 : points.Sum(p => (p.X - meanX) * (p.Y - meanY)) / sxx;

            table.AddRow(name, start?.ToString(_inv) ?? "", points[^1].Y.ToString("F0", _inv), points.Max(p => p.Y).ToString("F0", _inv), (slope * 100).ToString("+0.0;-0.0", _inv));
        }

        private static double Percentile(List<double> sorted, double p)
        {
            return sorted[(int)Math.Min(sorted.Count - 1, Math.Ceiling(p * sorted.Count) - 1)];
        }
    }

    private sealed record CopyRequest(int[] FolderIds, int[] FileIds, int DestFolderId, int ConflictResolveType, bool DeleteAfter, bool ReturnSingleOperation);

    private sealed record OperationsAnswer(List<OperationState>? Response);

    private sealed record OperationState(string? Id, bool Finished, string? Error);

    private sealed record StartedOperation(string Id, bool Finished, string? Error);

    private sealed record Row(string Iteration, string Status, double DurationSeconds, double CopyRequestMs, int Polls, MemorySample Memory)
    {
        public int? Copies { get; init; }
    }
}
