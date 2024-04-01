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

namespace ASC.Migration.Core;

[Scope]
public abstract class AbstractMigration<TMigrationInfo, TUser, TFiles, TGroup>(MigrationLogger migrationLogger)
    : IMigration
    where TMigrationInfo : IMigrationInfo
{
    protected readonly MigrationLogger _logger = migrationLogger;
    protected CancellationToken _cancellationToken;
    protected TMigrationInfo _migrationInfo;
    private double _lastProgressUpdate;
    private string _lastStatusUpdate;
    protected List<Guid> _importedUsers;
    public abstract MigratorMeta Meta { get; }

    public Func<double, string, Task> OnProgressUpdate { get; set; }

    protected async Task ReportProgress(double value, string status)
    {
        _lastProgressUpdate = value;
        _lastStatusUpdate = status;

        if (OnProgressUpdate != null)
        {
            await OnProgressUpdate(value, status);
        }

        _logger.Log($"{value:0.00} progress: {status}");
    }

    public double GetProgress() => _lastProgressUpdate;
    public string GetProgressStatus() => _lastStatusUpdate;

    public MigrationApiInfo ApiInfo { get => _migrationInfo?.ToApiInfo(); }

    public abstract Task InitAsync(string path, CancellationToken cancellationToken, OperationType operation);

    public abstract Task<MigrationApiInfo> ParseAsync(bool reportProgress = true);

    public abstract Task MigrateAsync(MigrationApiInfo migrationInfo);

    public void Log(string msg, Exception exception = null)
    {
        _logger.Log(msg, exception);
    }
    public virtual void Dispose()
    {
        _logger.Dispose();
    }

    public string GetLogName()
    {
        return _logger.GetLogName();
    }

    public virtual List<Guid> GetGuidImportedUsers()
    {
        return _importedUsers;
    }
}
