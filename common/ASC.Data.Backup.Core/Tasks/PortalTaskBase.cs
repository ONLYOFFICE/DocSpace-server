// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Data.Backup.Tasks;

public class ProgressChangedEventArgs(int progress) : EventArgs
{
    public int Progress { get; private set; } = progress;
}

public abstract class PortalTaskBase(DbFactory dbFactory, ILogger logger, StorageFactory storageFactory, StorageFactoryConfig storageFactoryConfig, ModuleProvider moduleProvider)
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
            var domainFolders = new List<string>();

            foreach(var domain in StorageFactoryConfig.GetDomainList(module, false))
            {
                domainFolders.Add(store.GetRootDirectory(domain));
            }
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
                    "webplugins"
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

    protected void SetStepsCount(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
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
        Progress = value;
        await OnProgressChanged(new ProgressChangedEventArgs(value));
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

                try
                {
                    command = connection.CreateCommand();
                    command.CommandText = commandText;
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception)
                {
                    try
                    {
                        Thread.Sleep(2000);//avoiding deadlock
                            command = connection.CreateCommand();
                            command.CommandText = commandText;
                            await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorRestore(ex);
                    }
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
}
