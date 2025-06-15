// (c) Copyright Ascensio System SIA 2009-2025
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
/// Tenant wallet settings
/// </summary>
public class TenantWalletSettingsWrapper
{
    /// <summary>
    /// Tenant wallet settings
    /// </summary>
    public TenantWalletSettings Settings { get; set; }
}

[Scope]
[Serializable]
public class TenantWalletSettings : ISettings<TenantWalletSettings>
{
    /// <summary>
    /// Enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Minimun balance
    /// </summary>
    [Range(5, 1000)]
    public int MinBalance { get; set; }

    /// <summary>
    /// Up to balance
    /// </summary>
    [Range(6, 5000)]
    public int UpToBalance { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    public string Currency { get; set; }


    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{40069709-492A-4F41-988C-F1A053A8A560}"); }
    }

    public TenantWalletSettings GetDefault()
    {
        return new TenantWalletSettings();
    }

    public DateTime LastModified { get; set; }
}