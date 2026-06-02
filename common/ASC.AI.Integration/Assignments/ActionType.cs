// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Integration.Assignments;

/// <summary>
/// Identifies the AI action a profile can be assigned to. Each member is a distinct
/// assignment slot within a tenant/entry scope (see <see cref="AssignmentsStorage"/>).
/// </summary>
[EnumExtensions]
public enum ActionType
{
    /// <summary>
    /// The global fallback slot used during profile resolution when no profile is bound
    /// to the requested action. This is a valid, persisted assignment value — <b>not</b> a
    /// sentinel or "unset" marker. Note that, as the first member, its underlying value is
    /// <c>0</c> (i.e. <c>default(ActionType)</c>), so reads such as
    /// <see cref="Database.AiIntegrationContext.GetAllAssignmentsAsync"/> intentionally
    /// include it; do not add a filter or constraint that excludes it.
    /// </summary>
    Default,
    Chat,
    Code,
    Summarization,
    Translation,
    TextAnalyze,
    ImageGeneration,
    OCR,
    Vision
}
