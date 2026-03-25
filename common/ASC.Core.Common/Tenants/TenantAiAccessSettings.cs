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

namespace ASC.Core.Tenants;

/// <summary>
/// The tenant-level settings for enabling or disabling all AI functionality in DocSpace.
/// </summary>
/// <remarks>
/// When AI is disabled for a tenant, all AI-related features are turned off:
/// the AI Agents folder is hidden from root folder listings, AI status checks
/// return disabled immediately, and AI chat endpoints become inaccessible.
/// AI is enabled by default for backward compatibility.
/// Only users with the DocSpaceAdmin role (EditPortalSettings permission) can change this setting.
/// </remarks>
[Scope]
[Serializable]
public class TenantAiAccessSettings : ISettings<TenantAiAccessSettings>
{
    /// <summary>
    /// Specifies whether AI functionality is enabled for the tenant.
    /// When set to <c>false</c>, all AI features (chat, agents, vectorization) are disabled tenant-wide.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The settings ID.
    /// </summary>
    public static Guid ID => new("{C74A278F-0936-47FF-915E-619622770504}");

    public TenantAiAccessSettings GetDefault()
    {
        return new TenantAiAccessSettings
        {
            Enabled = true
        };
    }

    /// <summary>
    /// The timestamp indicating when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }
}
