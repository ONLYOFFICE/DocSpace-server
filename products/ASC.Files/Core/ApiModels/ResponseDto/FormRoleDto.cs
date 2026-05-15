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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The form role parameters.
/// </summary>
public class FormRoleDto
{

    /// <summary>
    /// The role name.
    /// </summary>
    /// <example>Approver</example>
    public required string RoleName { get; set; }

    /// <summary>
    /// The role color.
    /// </summary>
    /// <example>#FF5733</example>
    public string RoleColor { get; set; }

    /// <summary>
    /// The user of the role.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto User { get; set; }

    /// <summary>
    /// The role sequence.
    /// </summary>
    /// <example>1</example>
    public required int Sequence { get; set; }

    /// <summary>
    /// Specifies if the role is submitted.
    /// </summary>
    /// <example>false</example>
    public required bool Submitted { get; set; }

    /// <summary>
    /// The user who stopped the role.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto StopedBy { get; set; }

    /// <summary>
    /// The role history.
    /// </summary>
    /// <example>{"0": "2025-01-15T10:30:00Z"}</example>
    public Dictionary<int, DateTime> History { get; set; }

    /// <summary>
    /// The role status.
    /// </summary>
    /// <example>0</example>
    public FormFillingStatus RoleStatus { get; set; }
}
[Scope]
public class FormRoleDtoHelper(TenantUtil tenantUtil, EmployeeFullDtoHelper employeeFullDtoHelper, UserManager userManager)
{
    public async Task<FormRoleDto> Get<T>(EntryProperties<T> properties, FormRole role)
    {
        if (role == null)
        {
            return null;
        }
        var user = await employeeFullDtoHelper.GetFullAsync(await userManager.GetUsersAsync(role.UserId));
        var result = new FormRoleDto
        {
            RoleName = role.RoleName,
            RoleColor = role.RoleColor,
            User = user,
            Sequence = role.Sequence,
            Submitted = role.Submitted,
            History = new Dictionary<int, DateTime>()
        };

        if (!role.OpenedAt.Equals(DateTime.MinValue))
        {
            result.History.Add((int)FormRoleHistory.OpenedAtDate, tenantUtil.DateTimeFromUtc(role.OpenedAt));
        }
        if (!role.SubmissionDate.Equals(DateTime.MinValue))
        {
            result.History.Add((int)FormRoleHistory.SubmissionDate, tenantUtil.DateTimeFromUtc(role.SubmissionDate));
        }
        if (properties != null && !DateTime.MinValue.Equals(properties.FormFilling.FillingStopedDate) && role.RoleName.Equals(properties.FormFilling.FormFillingInterruption?.RoleName))
        {
            var stopedById = properties.FormFilling.FormFillingInterruption?.UserId ?? Guid.Empty;
            var stopedBy = await employeeFullDtoHelper.GetFullAsync(await userManager.GetUsersAsync(stopedById));
            result.StopedBy = Guid.Empty.Equals(stopedById) ? null : stopedBy;
            result.History.Add((int)FormRoleHistory.StopDate, tenantUtil.DateTimeFromUtc(properties.FormFilling.FillingStopedDate));
        }
        return result;
    }
}

/// <summary>
/// The form role history type.
/// </summary>
public enum FormRoleHistory
{
    [Description("Opened at date")]
    OpenedAtDate = 0,

    [Description("Submission date")]
    SubmissionDate = 1,

    [Description("Stop date")]
    StopDate = 2
}