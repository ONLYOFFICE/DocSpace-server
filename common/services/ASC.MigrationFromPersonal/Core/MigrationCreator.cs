﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.MigrationFromPersonal;

[Transient]
public class MigrationCreator
{
    private readonly TenantDomainValidator _tenantDomainValidator;
    private readonly TempStream _tempStream;
    private readonly DbFactory _dbFactory;
    private readonly StorageFactory _storageFactory;
    private readonly StorageFactoryConfig _storageFactoryConfig;
    private readonly ModuleProvider _moduleProvider;
    private readonly IMapper _mapper;
    private readonly CreatorDbContext _creatorDbContext;
    private readonly ILogger<MigrationCreator> _logger;
    private long _totalSize;

    private List<IModuleSpecifics> _modules;
    private string _pathToSave;
    private string _userName;
    private string _mail;
    private string _toRegion;
    private string _toAlias;
    private string _fromAlias;
    private int _fromTenantId;
    private readonly object _locker = new object();
    private readonly int _limit = 1000;
    private readonly List<ModuleName> _namesModules = new List<ModuleName>()
    {
        ModuleName.Core,
        ModuleName.Files,
        ModuleName.Files2,
        ModuleName.Tenants,
        ModuleName.WebStudio
    };

    private readonly List<ModuleName> _namesModulesForAlreadyExistPortal = new List<ModuleName>()
    {
        ModuleName.Core,
        ModuleName.Files,
        ModuleName.Files2,
    };

    public string NewAlias { get; private set; }

    public MigrationCreator(
        TenantDomainValidator tenantDomainValidator,
        TempStream tempStream,
        DbFactory dbFactory,
        StorageFactory storageFactory,
        StorageFactoryConfig storageFactoryConfig,
        ModuleProvider moduleProvider,
        IMapper mapper,
        CreatorDbContext сreatorDbContext,
        ILogger<MigrationCreator> logger)
    {
        _tenantDomainValidator = tenantDomainValidator;
        _tempStream = tempStream;
        _dbFactory = dbFactory;
        _storageFactory = storageFactory;
        _storageFactoryConfig = storageFactoryConfig;
        _moduleProvider = moduleProvider;
        _mapper = mapper;
        _creatorDbContext = сreatorDbContext;
        _logger = logger;
    }

    public async Task<(string, string, long)> CreateAsync(string fromAlias, string mail, string toRegion, string toAlias)
    {
        Init(fromAlias, mail, toRegion, toAlias);

        var id = GetUserId();
        await CheckTotalSizeAsync(id);
        CheckCountManager();
        var fileName = _userName + ".tar.gz";
        var path = Path.Combine(_pathToSave, fileName);
        await using (var writer = new ZipWriteOperator(_tempStream, path))
        {
            await DoMigrationDb(id, writer);
            await DoMigrationStorage(id, writer);
        }
        return (fileName, NewAlias, _totalSize);
    }

    private void Init(string fromAlias, string mail, string toRegion, string toAlias)
    {
        _pathToSave = "";
        _toRegion = toRegion;
        _mail = mail;
        _fromAlias = fromAlias;
        _toAlias = toAlias;

        using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>();
        var tenant = dbContextTenant.Tenants.SingleOrDefault(q => q.Alias == _fromAlias);

        if (tenant == null)
        {
            throw new ArgumentException("tenant was not found");
        }
        _fromTenantId = tenant.Id;

        _modules = string.IsNullOrEmpty(_toAlias)
            ? _moduleProvider.AllModules.Where(m => _namesModules.Contains(m.ModuleName)).ToList()
            : _moduleProvider.AllModules.Where(m => _namesModulesForAlreadyExistPortal.Contains(m.ModuleName)).ToList();
    }

    private Guid GetUserId()
    {
        try
        {
            using var userDbContext = _creatorDbContext.CreateDbContext<UserDbContext>();
            User user = null;
            if (string.IsNullOrEmpty(_userName) || string.IsNullOrEmpty(_mail))
            {
                if (string.IsNullOrEmpty(_userName))
                {
                    user = userDbContext.Users.FirstOrDefault(q => q.TenantId == _fromTenantId && q.Status == EmployeeStatus.Active && q.Email == _mail);
                    _userName = user.UserName;
                }
                else
                {
                    user = userDbContext.Users.FirstOrDefault(q => q.TenantId == _fromTenantId && q.Status == EmployeeStatus.Active && q.UserName == _userName);
                }
            }
            else
            {
                user = userDbContext.Users.FirstOrDefault(q => q.TenantId == _fromTenantId && q.Status == EmployeeStatus.Active && q.UserName == _userName && q.Email == _mail);
            }
            if (!string.IsNullOrEmpty(_toAlias))
            {
                using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(_toRegion);
                using var userDbContextToregion = _creatorDbContext.CreateDbContext<UserDbContext>(_toRegion);
                var tenant = dbContextTenant.Tenants.SingleOrDefault(t => t.Alias == _toAlias);
                if (tenant == null)
                {
                    throw new ArgumentException("tenant was not found");
                }
                else
                {
                    if (userDbContextToregion.Users.Any(q => q.TenantId == tenant.Id && q.UserName == _userName || q.Email == _mail))
                    {
                        throw new ArgumentException("username already exist in the portal");
                    }
                }
            }
            return user.Id;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArgumentException("username was not found");
        }
    }

    private async Task CheckTotalSizeAsync(Guid userId)
    {
        await using var filesDbContext = _creatorDbContext.CreateDbContext<FilesDbContext>();
        _totalSize = await filesDbContext.Files.Where(f => f.CreateBy == userId).SumAsync(f => f.ContentLength);

        await using var coreDbContext = _creatorDbContext.CreateDbContext<CoreDbContext>(_toRegion);
        var quota = _mapper.Map<DbQuota, TenantQuota>(await coreDbContext.Quotas.SingleOrDefaultAsync(r => r.TenantId == -3));

        if(quota.MaxTotalSize < _totalSize)
        {
            throw new ArgumentException("personal total size more than docspace totalsize");
        }
    }
    
    private void CheckCountManager()
    {
        if (!string.IsNullOrEmpty(_toAlias))
        {
            using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(_toRegion);
            var tenant = dbContextTenant.Tenants.SingleOrDefault(t => t.Alias == _toAlias);

            using var coreDbContext = _creatorDbContext.CreateDbContext<CoreDbContext>(_toRegion);
            var qouta = coreDbContext.Quotas
                 .Where(r => r.TenantId == tenant.Id)
                 .ProjectTo<TenantQuota>(_mapper.ConfigurationProvider)
                 .SingleOrDefault();

            using var userDbContextToregion = _creatorDbContext.CreateDbContext<UserDbContext>(_toRegion);
            var usersCount = userDbContextToregion.Users
                .Join(userDbContextToregion.UserGroups, u => u.Id, ug => ug.Userid, (u, ug) => new { u, ug })
                .Where(q => q.u.TenantId == tenant.Id && q.ug.UserGroupId == Common.Security.Authorizing.Constants.DocSpaceAdmin.ID).Count();
            if (usersCount > qouta.CountRoomAdmin)
            {
                throw new ArgumentException("user count exceed");
            }
        }
    }

    private async Task DoMigrationDb(Guid id, IDataWriteOperator writer)
    {
        if (!string.IsNullOrEmpty(_toAlias))
        {
            using (var connection = _dbFactory.OpenConnection())
            {
                var tenantsModule = _moduleProvider.AllModules.Single(q => q.ModuleName == ModuleName.Tenants);
                var coreUserTable = tenantsModule.Tables.Single(q => q.Name == "core_user");
                await ArchiveTable(coreUserTable, writer, tenantsModule, connection, id);
            }
        }

        _logger.LogDebug($"Archive tables");
        foreach (var module in _modules)
        {
            var tablesToProcess = module.Tables.Where(t => t.InsertMethod != InsertMethod.None).ToList();

            using (var connection = _dbFactory.OpenConnection())
            {
                foreach (var table in tablesToProcess)
                {
                    if (table.Name == "files_room_settings" ||
                        table.Name == "files_thirdparty_account" ||
                        table.Name == "files_thirdparty_id_mapping" ||
                        table.Name == "core_subscription" ||
                        table.Name == "files_security" ||
                        table.Name == "tenants_quotarow"||
                        table.Name == "tenants_tariff")
                    {
                        continue;
                    }
                    _logger.LogDebug($"Archive table - {table.Name}");
                    await ArchiveTable(table, writer, module, connection, id);
                }
            }
        }
        _logger.LogDebug($"Archive tables end");
    }

    private async Task ArchiveTable(TableInfo table, IDataWriteOperator writer, IModuleSpecifics module, DbConnection connection, Guid id)
    {
        _logger.LogDebug($"backup table {table.Name}");
        using (var data = new DataTable(table.Name))
        {
            try
            {
                data.Clear();
                int counts;
                var offset = 0;
                do
                {
                    var dataAdapter = _dbFactory.CreateDataAdapter();
                    dataAdapter.SelectCommand = module.CreateSelectCommand(connection.Fix(), _fromTenantId, table, _limit, offset, id).WithTimeout(600);
                    counts = ((DbDataAdapter)dataAdapter).Fill(data);
                    offset += _limit;
                } while (counts == _limit);

                foreach (var col in data.Columns.Cast<DataColumn>().Where(col => col.DataType == typeof(DateTime)))
                {
                    col.DateTimeMode = DataSetDateTime.Unspecified;
                }

                module.PrepareData(data);

                if (data.TableName == "tenants_tenants")
                {
                    ChangeAlias(data);
                    ChangeName(data);
                    ChangeIndustry(data);
                }

                if (data.TableName == "files_bunch_objects")
                {
                    ClearCommonBunch(data);
                }

                if (data.TableName == "files_file")
                {
                    ChangeThumb(data);
                }

                await WriteEnrty(data, writer, module);
            }
            catch (MySqlException)
            {
                if (table.Name != "tenants_tariffrow")
                {
                    throw;
                }
            }
        }
    }

    private async Task WriteEnrty(DataTable data, IDataWriteOperator writer, IModuleSpecifics module)
    {
        await using var file = _tempStream.Create();
        data.WriteXml(file, XmlWriteMode.WriteSchema);
        data.Clear();

        await writer.WriteEntryAsync(KeyHelper.GetTableZipKey(module, data.TableName), file, () => Task.CompletedTask);
    }

    private async void ChangeAlias(DataTable data)
    {
        var aliases = GetAliases();
        NewAlias = _userName.ToLower();
        while (true)
        {
            try
            {
                NewAlias = RemoveInvalidCharacters(NewAlias);
                _tenantDomainValidator.ValidateDomainLength(NewAlias);
                _tenantDomainValidator.ValidateDomainCharacters(NewAlias);
                if (aliases.Contains(NewAlias))
                {
                    throw new Exception($"Alias {NewAlias} is busy");
                }
                break;
            }
            catch (TenantTooShortException ex)
            {
                if (NewAlias.Length > 100)
                {
                    NewAlias = NewAlias.Substring(0, 50);
                }
                else
                {
                    NewAlias = $"DocSpace{NewAlias}";
                }
                _logger.LogDebug(ex.Message);
            }
            catch (Exception ex)
            {
                var last = NewAlias.Substring(NewAlias.Length - 1);
                if (int.TryParse(last, out var lastNumber))
                {
                    NewAlias = NewAlias.Substring(0, NewAlias.Length - 1) + (lastNumber + 1);
                }
                else
                {
                    NewAlias = NewAlias + 1;
                }

                _logger.LogDebug(ex.Message);
            }
        }
        _logger.LogDebug($"Alias is - {NewAlias}");
        data.Rows[0]["alias"] = NewAlias;

        using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(_toRegion);
        var dbTenant = new DbTenant();
        dbTenant.Alias = NewAlias;
        dbTenant.Version = 2;
        dbTenant.Name = "";
        dbTenant.Status = TenantStatus.Suspended;
        dbTenant.LastModified = DateTime.Now;
        await dbContextTenant.Tenants.AddAsync(dbTenant);
        await dbContextTenant.SaveChangesAsync();
    }

    private string RemoveInvalidCharacters(string alias)
    {
        return Regex.Replace(alias, "[^a-z0-9]", "", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private List<string> GetAliases()
    {
        using var dbContext = _creatorDbContext.CreateDbContext<TenantDbContext>(_toRegion);
        var tenants = dbContext.Tenants.Select(t => t.Alias).ToList();
        var forbidens = dbContext.TenantForbiden.Select(tf => tf.Address).ToList();
        return tenants.Union(forbidens).Select(t=> t.ToLower()).ToList();
    }

    private void ChangeName(DataTable data)
    {
        data.Rows[0]["name"] = "";
    }

    private void ChangeIndustry(DataTable data)
    {
        data.Rows[0]["industry"] = "0";
    }

    private void ClearCommonBunch(DataTable data)
    {
        for (var i = 0; i < data.Rows.Count; i++)
        {
            if (data.Rows[i]["right_node"].ToString().EndsWith('/'))
            {
                data.Rows.RemoveAt(i);
                i--;
            }
        }
    }

    private void ChangeThumb(DataTable data)
    {
        for (var i = 0; i < data.Rows.Count; i++)
        {
            data.Rows[i]["thumb"] = "0";
        }
    }

    private async Task DoMigrationStorage(Guid id, IDataWriteOperator writer)
    {
        _logger.LogDebug($"start backup storage");
        var fileGroups = await GetFilesGroup(id);
        foreach (var group in fileGroups)
        {
            _logger.LogDebug($"start backup fileGroup: {group.Key}");
            foreach (var file in group)
            {
                var storage = await _storageFactory.GetStorageAsync(_fromTenantId, group.Key);
                var file1 = file;
                await ActionInvoker.TryAsync(async (state, logger) =>
                {
                    var f = (BackupFileInfo)state;
                    logger.LogDebug($"start backup file: {f.Path}");
                    using var fileStream = await storage.GetReadStreamAsync(f.Domain, f.Path);
                    await writer.WriteEntryAsync(file1.GetZipKey(), fileStream, () => Task.CompletedTask);
                    logger.LogDebug($"end backup file: {f.Path}");
                }, file, _logger, 5, onFailure: error => _logger.WarningWithException("file can not backup", error));
            }
            _logger.LogDebug($"end backup fileGroup: {group.Key}");
        }

        var restoreInfoXml = new XElement(
            "storage_restore",
            fileGroups
                .SelectMany(group => group.Select(file => (object)file.ToXElement()))
                .ToArray());

        using (var tmpFile = _tempStream.Create())
        {
            restoreInfoXml.WriteTo(tmpFile);
            await writer.WriteEntryAsync(KeyHelper.GetStorageRestoreInfoZipKey(), tmpFile, () => Task.CompletedTask);
        }
        _logger.LogDebug($"end backup storage");
    }

    private async Task<List<IGrouping<string, BackupFileInfo>>> GetFilesGroup(Guid id)
    {
        var files = (await GetFilesToProcess(id)).Where(f=> !f.Path.Contains("/thumb.")).ToList();

        return files.GroupBy(file => file.Module).ToList();
    }

    private async Task<IEnumerable<BackupFileInfo>> GetFilesToProcess(Guid id)
    {
        var files = new List<BackupFileInfo>();

        var filesDbContext = _creatorDbContext.CreateDbContext<FilesDbContext>();

        var module = _storageFactoryConfig.GetModuleList().Where(m => m == "files").Single();

        var store = await _storageFactory.GetStorageAsync(_fromTenantId, module);

        var dbFiles = filesDbContext.Files.Where(q => q.CreateBy == id && q.TenantId == _fromTenantId).ToList();

        var tasks = new List<Task>(20);
        foreach (var dbFile in dbFiles)
        {
            if (tasks.Count != 20)
            {
                tasks.Add(FindFiles(files, store, dbFile, module));
            }
            else
            {
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
            }
        }
        Task.WaitAll(tasks.ToArray());
        return files.Distinct();
    }

    private async Task FindFiles(List<BackupFileInfo> list, IDataStore store, DbFile dbFile, string module)
    {
        var files = await store.ListFilesRelativeAsync(string.Empty, $"\\{GetUniqFileDirectory(dbFile.Id)}", "*.*", true)
                 .Select(path => new BackupFileInfo(string.Empty, module, $"{GetUniqFileDirectory(dbFile.Id)}\\{path}", _fromTenantId))
                 .ToListAsync();

        lock (_locker)
        {
            list.AddRange(files);
        }

        if (files.Any())
        {
            _logger.LogDebug($"file {dbFile.Id} found");
        }
        else
        {
            _logger.LogDebug($"file {dbFile.Id} not found");
        }
    }

    private string GetUniqFileDirectory(int fileId)
    {
        if (fileId == 0)
        {
            throw new ArgumentNullException("fileIdObject");
        }

        return string.Format("folder_{0}/file_{1}", (fileId / 1000 + 1) * 1000, fileId);
    }
}