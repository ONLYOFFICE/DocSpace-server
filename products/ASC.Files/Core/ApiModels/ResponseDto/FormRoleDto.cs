// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class FormRoleDto
{
    public string RoleName { get; set; }
    public string RoleColor { get; set; }
    public Guid UserId { get; set; }
    public int Sequence { get; set; }
    public bool Submitted { get; set; }
    public Guid? StopedBy { get; set; }
    public Dictionary<int, DateTime> History { get; set; }
}
[Scope]
public class FormRoleDtoHelper(TenantUtil tenantUtil)
{
    public FormRoleDto Get<T>(EntryProperties<T> properties, FormRole role)
    {
        if (role == null)
        {
            return null;
        }
        var history = new Dictionary<FormRoleHistory, DateTime>();
        var result = new FormRoleDto
        {
            RoleName = role.RoleName,
            RoleColor = role.RoleColor,
            UserId = role.UserId,
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
            result.StopedBy = Guid.Empty.Equals(properties.FormFilling.FormFillingInterruption?.UserId) ? null : properties.FormFilling.FormFillingInterruption?.UserId;
            result.History.Add((int)FormRoleHistory.StopDate, tenantUtil.DateTimeFromUtc(properties.FormFilling.FillingStopedDate));
        }
        return result;

    }
}

public enum FormRoleHistory
{
    OpenedAtDate = 0 ,
    SubmissionDate = 1,
    StopDate = 2
}
