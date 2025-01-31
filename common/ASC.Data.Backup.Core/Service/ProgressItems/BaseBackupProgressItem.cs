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

using ASC.Common.Threading.Progress;

namespace ASC.Data.Backup.Services;

public abstract class BaseBackupProgressItem(IServiceScopeFactory serviceScopeFactory) : DistributedTaskProgress
{
    protected readonly IServiceScopeFactory _serviceScopeProvider = serviceScopeFactory;
    private int? _tenantId;
    private BackupProgressItemType? _backupProgressItemEnum;
    private string _link;
    private int? _newTenantId;

    public int NewTenantId
    {
        get => _newTenantId ?? this[nameof(_newTenantId)];
        set
        {
            _newTenantId = value;
            this[nameof(_newTenantId)] = value;
        }
    }


    public int TenantId
    {
        get => _tenantId ?? this[nameof(_tenantId)];
        set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }


    private bool? _dump;
    public bool Dump
    {
        get => _dump ?? this[nameof(_dump)];
        set
        {
            _dump = value;
            this[nameof(_dump)] = value;
        }
    }

    public string Link
    {
        get
        {
            return _link ?? this[nameof(_link)];
        }
        set
        {
            _link = value;
            this[nameof(_link)] = value;
        }
    }

    public BackupProgressItemType BackupProgressItemType
    {
        get
        {
            return _backupProgressItemEnum ?? (BackupProgressItemType)this[nameof(_backupProgressItemEnum)];
        }
        protected set
        {
            _backupProgressItemEnum = value;
            this[nameof(_backupProgressItemEnum)] = (int)value;
        }
    }

    protected void Init()
    {
        this[nameof(_tenantId)] = 0;
        this[nameof(_newTenantId)] = 0;
        this[nameof(_dump)] = false;
        this[nameof(_link)] = "";
    }

    public BackupProgress ToBackupProgress()
    {
        var progress = new BackupProgress
        {
            IsCompleted = IsCompleted,
            Progress = (int)Percentage,
            Error = Exception != null ? Exception.Message : "",
            TenantId = TenantId,
            BackupProgressEnum = BackupProgressItemType.Convert(),
            TaskId = Id
        };
        if (BackupProgressItemType is BackupProgressItemType.Backup or BackupProgressItemType.Transfer && Link != null)
        {
            progress.Link = Link;
        }

        return progress;
    }

    public abstract object Clone();
}
