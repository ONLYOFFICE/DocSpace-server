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

namespace ASC.Data.Backup.Tasks;

public class ProgressChangedEventArgs(int progress) : EventArgs
{
    public int Progress { get; private set; } = progress;
}

public abstract class PortalTaskBase(
    DbFactory dbFactory,
    ILogger logger,
    StorageFactory storageFactory,
    StorageFactoryConfig storageFactoryConfig,
    ModuleProvider moduleProvider) : IDisposable
{
    protected const int TasksLimit = 10;

    protected StorageFactory StorageFactory { get; set; } = storageFactory;
    protected StorageFactoryConfig StorageFactoryConfig { get; set; } = storageFactoryConfig;
    protected ILogger Logger { get; set; } = logger;
    public int Progress { get; private set; }
    public int TenantId { get; private set; }
    public bool ProcessStorage { get; set; } = true;
    protected IDataWriteOperator WriteOperator { get; set; }
    protected ModuleProvider ModuleProvider { get; set; } = moduleProvider;
    protected DbFactory DbFactory { get; init; } = dbFactory;

    protected readonly List<ModuleName> _ignoredModules = [];
    protected readonly List<string> _ignoredTables = []; //todo: add using to backup and transfer tasks

    public void Init(int tenantId)
    {
        TenantId = tenantId;
        IgnoreTable("hosting_instance_registration");
    }

    public void IgnoreModule(ModuleName moduleName)
    {
        if (!_ignoredModules.Contains(moduleName))
        {
            _ignoredModules.Add(moduleName);
        }
    }

    public void IgnoreTable(string tableName)
    {
        if (!_ignoredTables.Contains(tableName))
        {
            _ignoredTables.Add(tableName);
        }
    }

    public abstract Task RunJob();

    internal IEnumerable<IModuleSpecifics> GetModulesToProcess()
    {
        return ModuleProvider.AllModules.Where(module => !_ignoredModules.Contains(module.ModuleName));
    }

    protected async IAsyncEnumerable<BackupFileInfo> GetFilesToProcess(int tenantId)
    {
        foreach (var module in StorageFactoryConfig.GetModuleList().Where(IsStorageModuleAllowed))
        {
            var store = await StorageFactory.GetStorageAsync(tenantId, module);
            var domainFolders = StorageFactoryConfig.GetDomainList(module, false).Select(domain => store.GetRootDirectory(domain)).ToList();

            var files = store.ListFilesRelativeAsync(string.Empty, "\\", "*", true)
                          .Where(path => domainFolders.All(domain => !path.Contains(domain + "/") && !path.Contains(domain + "\\")))
                         .Select(path => new BackupFileInfo(string.Empty, module, path, tenantId));

            await foreach (var file in files)
            {
                yield return file;
            }

            foreach (var domain in StorageFactoryConfig.GetDomainList(module))
            {
                files = store.ListFilesRelativeAsync(domain, "\\", "*", true)
                    .Select(path => new BackupFileInfo(domain, module, path, tenantId));

                await foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }

    protected bool IsStorageModuleAllowed(string storageModuleName)
    {
        var allowedStorageModules = new List<string>
                {
                    "forum",
                    "photo",
                    "bookmarking",
                    "wiki",
                    "files",
                    "crm",
                    "projects",
                    "logo",
                    "fckuploaders",
                    "talk",
                    "mailaggregator",
                    "whitelabel",
                    "customnavigation",
                    "room_logos",
                    "webplugins",
                    "mcp_icons"
                };

        if (!allowedStorageModules.Contains(storageModuleName))
        {
            return false;
        }

        var moduleSpecifics = ModuleProvider.GetByStorageModule(storageModuleName);

        return moduleSpecifics == null || !_ignoredModules.Contains(moduleSpecifics.ModuleName);
    }

    #region Progress

    public Func<ProgressChangedEventArgs, Task> ProgressChanged;

    private int _stepsCount = 1;
    private volatile int _stepsCompleted;
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private readonly SemaphoreSlim _progressSemaphore = new(1, 1);
    private const int ProgressUpdateIntervalMs = 1000;

    protected void SetStepsCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

        _stepsCount = value;
        Logger.DebugCountSteps(+_stepsCount);
    }

    protected async Task SetStepCompleted(int increment = 1)
    {
        if (_stepsCount == 1)
        {
            return;
        }
        if (_stepsCompleted == _stepsCount)
        {
            throw new InvalidOperationException("All steps completed.");
        }
        _stepsCompleted += increment;
        await SetProgress(100 * _stepsCompleted / _stepsCount);
    }

    protected async Task SetCurrentStepProgress(int value)
    {
        switch (value)
        {
            case < 0 or > 100:
                throw new ArgumentOutOfRangeException(nameof(value));
            case 100:
                await SetStepCompleted();
                await SetProgress(100);
                break;
            default:
                await SetProgress((100 * _stepsCompleted + value) / _stepsCount);
                break;
        }
    }

    private async Task SetProgress(int value)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value < Progress)
        {
            return;
        }

        await _progressSemaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = (now - _lastProgressUpdate).TotalMilliseconds;

            if (timeSinceLastUpdate < ProgressUpdateIntervalMs && value < 100)
            {
                return;
            }

            Progress = value;
            _lastProgressUpdate = now;
            await OnProgressChanged(new ProgressChangedEventArgs(value));
        }
        finally
        {
            _progressSemaphore.Release();
        }
    }

    private async Task OnProgressChanged(ProgressChangedEventArgs eventArgs)
    {
        if (ProgressChanged != null)
        {
            await ProgressChanged(eventArgs);
        }
    }

    #endregion

    private Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var result = new Dictionary<string, string>();

        var parsed = connectionString.Split(';');

        foreach (var p in parsed)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }

            var keyValue = p.Split('=');
            result.Add(keyValue[0].ToLowerInvariant(), keyValue[1]);
        }

        return result;
    }

    protected void RunMysqlFile(string file, bool db = false)
    {
        var connectionString = ParseConnectionString(DbFactory.ConnectionStringSettings());
        var args = new StringBuilder()
                .Append($"-h {connectionString["server"]} ")
                .Append($"-u {connectionString["user id"]} ")
                .Append($"-p{connectionString["password"]} ");

        if (db)
        {
            args.Append($"-D {connectionString["database"]} ");
        }

        args.Append($"-e \" source {file}\"");
        Logger.DebugRunMySQlFile(file, args.ToString());

        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = false,
            UseShellExecute = false,
            FileName = "mysql",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = args.ToString()
        };

        using (var proc = Process.Start(startInfo))
        {
            if (proc != null)
            {
                proc.WaitForExit();

                var error = proc.StandardError.ReadToEnd();
                Logger.Error(!string.IsNullOrEmpty(error) ? error : proc.StandardOutput.ReadToEnd());
            }
        }

        Logger.DebugCompleteMySQlFile(file);
    }

    protected async ValueTask RunMysqlFile(Stream stream, string db, string delimiter = ";")
    {
        if (stream == null)
        {
            return;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string commandText;

        await using var connection = DbFactory.OpenConnection(connectionString: db);
        var command = connection.CreateCommand();
        command.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
        await command.ExecuteNonQueryAsync();

        if (delimiter != null)
        {
            while ((commandText = await reader.ReadLineAsync()) != null)
            {
                var sb = new StringBuilder(commandText);
                if (!commandText.EndsWith(delimiter))
                {
                    var newline = "";
                    while (!newline.EndsWith(delimiter))
                    {
                        newline = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(newline))
                        {
                            break;
                        }

                        sb.Append(newline);
                    }

                    commandText = sb.ToString();
                }

                var attempt = 0;
                var rewrittenForGeneratedCol = false;
                while (true)
                {
                    try
                    {
                        command = connection.CreateCommand();
                        command.CommandText = commandText;
                        await command.ExecuteNonQueryAsync();
                        break;
                    }
                    catch (MySqlException ex) when (!rewrittenForGeneratedCol && ex.Number == 3105)
                    {
                        rewrittenForGeneratedCol = true;
                        var colMatch = Regex.Match(ex.Message, @"generated column '(\w+)'", RegexOptions.IgnoreCase);
                        var rewritten = colMatch.Success ? RemoveColumnFromInsert(commandText, colMatch.Groups[1].Value) : null;
                        if (rewritten != null)
                        {
                            commandText = rewritten;
                            continue;
                        }
                        Logger.ErrorRestore(ex);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt >= 5)
                        {
                            Logger.ErrorRestore(ex);
                            break;
                        }
                        attempt++;
                    }
                    Thread.Sleep(1000);//avoiding deadlock
                }
            }
        }
        else
        {
            commandText = await reader.ReadToEndAsync();

            try
            {
                command = connection.CreateCommand();
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Logger.ErrorRestore(e);
            }
        }
    }

    private static string RemoveColumnFromInsert(string sql, string column)
    {
        var tableMatch = Regex.Match(sql, @"(?:REPLACE|INSERT)\s+INTO\s+`?\w+`?\s*\(", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
        {
            return null;
        }

        var colListStart = tableMatch.Index + tableMatch.Length;
        var colListEnd = sql.IndexOf(')', colListStart);
        if (colListEnd < 0)
        {
            return null;
        }

        var cols = sql.Substring(colListStart, colListEnd - colListStart)
            .Split(',')
            .Select(c => c.Trim().Trim('`'))
            .ToList();

        var removeIdx = cols.FindIndex(c => c.Equals(column, StringComparison.OrdinalIgnoreCase));
        if (removeIdx < 0)
        {
            return null;
        }

        var newColSection = string.Join(",", cols.Where((_, i) => i != removeIdx).Select(c => $"`{c}`"));

        var valuesKeyword = sql.IndexOf("VALUES", colListEnd, StringComparison.OrdinalIgnoreCase);
        if (valuesKeyword < 0)
        {
            return null;
        }

        var pos = sql.IndexOf('(', valuesKeyword);
        if (pos < 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.Append(sql, 0, colListStart);
        sb.Append(newColSection);
        sb.Append(") VALUES ");

        var firstTuple = true;
        while (pos < sql.Length)
        {
            if (sql[pos] != '(')
            {
                pos++;
                continue;
            }

            pos++; // skip '('
            var values = new List<string>();
            while (pos < sql.Length && sql[pos] != ')')
            {
                values.Add(ReadSqlValue(sql, ref pos));
                if (pos < sql.Length && sql[pos] == ',')
                {
                    pos++;
                }
            }

            if (pos < sql.Length)
            {
                pos++; // skip ')'
            }

            if (!firstTuple)
            {
                sb.Append(',');
            }
            sb.Append('(');
            sb.Append(string.Join(",", values.Where((_, i) => i != removeIdx)));
            sb.Append(')');
            firstTuple = false;

            while (pos < sql.Length && sql[pos] != '(' && sql[pos] != ';')
            {
                pos++;
            }

            if (pos >= sql.Length || sql[pos] == ';')
            {
                sb.Append(';');
                break;
            }
        }

        return sb.ToString();
    }

    private static string ReadSqlValue(string text, ref int pos)
    {
        var start = pos;
        if (pos < text.Length && text[pos] == '\'')
        {
            pos++; // skip opening quote
            while (pos < text.Length)
            {
                if (text[pos] == '\\')
                {
                    pos += 2; // skip escaped character
                }
                else if (text[pos] == '\'')
                {
                    pos++; // skip closing quote
                    break;
                }
                else
                {
                    pos++;
                }
            }
        }
        else
        {
            while (pos < text.Length && text[pos] != ',' && text[pos] != ')')
            {
                pos++;
            }
        }

        return text.Substring(start, pos - start);
    }

    public void Dispose()
    {
        _progressSemaphore?.Dispose();
    }
}