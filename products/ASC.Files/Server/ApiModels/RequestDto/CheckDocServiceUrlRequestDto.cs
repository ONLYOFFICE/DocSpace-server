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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The ONLYOFFICE Docs connection settings to store and verify.
/// </summary>
public class CheckDocServiceUrlRequestDto
{
    /// <summary>
    /// The public address of the Document Server, the one a browser loads the editor from. An empty value drops the
    /// portal's own setting, so the address configured for the deployment takes over again. A value with no scheme is
    /// stored with `http://` prepended, and an absolute address may not carry a query string.
    /// </summary>
    /// <example>https://documentserver.example.com</example>
    public required string DocServiceUrl { get; set; }

    /// <summary>
    /// The address the portal itself uses for its server-to-server calls to the Document Server, for deployments
    /// where that traffic stays inside the private network. Left empty, those calls go to the public address instead.
    /// </summary>
    /// <example>https://documentserver-internal.example.com</example>
    public string DocServiceUrlInternal { get; set; }

    /// <summary>
    /// The address of this portal as the Document Server has to call it back on in order to fetch and save a
    /// document. Set it when the Document Server cannot resolve the portal by its public name; left empty, the
    /// portal's own resolved address is used.
    /// </summary>
    /// <example>https://portal.example.com</example>
    public string DocServiceUrlPortal { get; set; }

    /// <summary>
    /// The shared secret that requests between the portal and the Document Server are signed with; it has to be the
    /// same value the Document Server itself is configured with, otherwise the verification of the new settings
    /// fails. It is write-only: the document service location is reported without it.
    /// </summary>
    /// <example>secret-key-123</example>
    public string DocServiceSignatureSecret { get; set; }

    /// <summary>
    /// The name of the HTTP header the signature travels in, which has to match the header the Document Server
    /// expects. A secret without a header is not a usable pair and is rejected.
    /// </summary>
    /// <example>Authorization</example>
    public string DocServiceSignatureHeader { get; set; }

    /// <summary>
    /// Whether the portal validates the TLS certificate of the Document Server. With verification on, a self-signed
    /// certificate breaks the connection; with it off, any certificate is accepted, which is meant for test
    /// deployments only. Omitting the field turns verification on.
    /// </summary>
    /// <example>true</example>
    public bool? DocServiceSslVerification { get; set; }
}