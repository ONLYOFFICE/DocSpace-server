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

namespace ASC.Common.Threading;

/// <summary>
/// </summary>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(DistributedTaskProgress))]
public class DistributedTask
{
    [JsonInclude]
    protected string _exeption = string.Empty;

    [JsonIgnore]
    public Func<DistributedTask, Task> Publication { get; set; }

    /// <summary>Instance ID</summary>
    /// <type>System.Int32, System</type>
    public int InstanceId { get; set; }

    /// <summary>ID</summary>
    /// <type>System.String, System</type>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Status</summary>
    /// <type>ASC.Common.Threading.DistributedTaskStatus, ASC.Common</type>
    public DistributedTaskStatus Status { get; set; }

    /// <summary>Last modified date</summary>
    /// <type>System.DateTime, System</type>
    public DateTime LastModifiedOn { get; set; }

    /// <summary>Exception</summary>
    /// <type>System.Object, System</type>
    [JsonIgnore]
    public Exception Exception
    {
        get => new(_exeption);
        set => _exeption = value?.Message ?? "";
    }

    protected CancellationToken CancellationToken { get; set; }

    public virtual async Task RunJob(CancellationToken cancellationToken)
    {
        Status = DistributedTaskStatus.Running;
        CancellationToken = cancellationToken;

        await DoJob();
    }

    protected virtual Task DoJob() { return Task.CompletedTask; }

    public async Task PublishChanges()
    {
        if (Publication == null)
        {
            throw new InvalidOperationException("Publication not found.");
        }

        await Publication(this);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}