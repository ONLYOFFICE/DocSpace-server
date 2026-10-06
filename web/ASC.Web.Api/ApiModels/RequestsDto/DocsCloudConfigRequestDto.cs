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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// Represents the configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudConfigRequestDto
{
    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Portal</example>
    [StringLength(255)]
    public string TenantName { get; set; }

    /// <summary>
    /// The security configuration.
    /// </summary>
    public DocsCloudSecurityConfigRequest Security { get; set; }

    /// <summary>
    /// The server configuration.
    /// </summary>
    public DocsCloudServerConfigRequest Server { get; set; }

    /// <summary>
    /// The WOPI configuration.
    /// </summary>
    public DocsCloudWopiConfigRequest Wopi { get; set; }

    /// <summary>
    /// The IP filter configuration.
    /// </summary>
    public DocsCloudIpFilterConfigRequest IpFilter { get; set; }
}

/// <summary>
/// Represents the security configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudSecurityConfigRequest
{
    /// <summary>
    /// The security secret.
    /// </summary>
    /// <example>abc123</example>
    [StringLength(255)]
    public string Secret { get; set; }

    /// <summary>
    /// The security header name.
    /// </summary>
    /// <example>Authorization</example>
    [StringLength(255)]
    public string Header { get; set; }
}

/// <summary>
/// Represents the server configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudServerConfigRequest
{
    /// <summary>
    /// Whether anonymous access is supported.
    /// </summary>
    /// <example>false</example>
    public bool IsAnonymousSupport { get; set; }

    /// <summary>
    /// The maximum file size in bytes.
    /// </summary>
    /// <example>104857600</example>
    // The operand type must match the property: the int overload of Range converts the value with Convert.ToInt32,
    // which overflows (rather than reporting a validation error) on anything above int.MaxValue.
    [Range(typeof(long), "0", "209715200")]
    public long FileSizeLimit { get; set; }
}

/// <summary>
/// Represents the WOPI configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudWopiConfigRequest
{
    /// <summary>
    /// Whether WOPI is enabled.
    /// </summary>
    /// <example>false</example>
    public bool Enable { get; set; }
}

/// <summary>
/// Represents the IP filter configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudIpFilterConfigRequest
{
    /// <summary>
    /// The IP filter rules.
    /// </summary>
    /// <example>[{"address": "127.0.0.1", "allowed": true}]</example>
    public List<DocsCloudIpFilterRuleRequest> Rules { get; set; }
}

/// <summary>
/// Represents the IP filter rule of a Docs Connect tenant.
/// </summary>
public class DocsCloudIpFilterRuleRequest
{
    /// <summary>
    /// The IP address.
    /// </summary>
    /// <example>127.0.0.1</example>
    // A length cap only: the field also carries ranges and CIDR notation, so the format is DocsCloud's to judge.
    [StringLength(255)]
    public string Address { get; set; }

    /// <summary>
    /// Whether the IP address is allowed.
    /// </summary>
    /// <example>true</example>
    public bool Allowed { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class DocsCloudConfigRequestDtoMapper
{
    public static partial DocsCloudConfig Map(this DocsCloudConfigRequestDto source);
}
