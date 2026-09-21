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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// How far the portal has got with the directory operation that is running, and what it ran into.
/// </summary>
/// <example>
/// {
///   "completed": false,
///   "id": "00000000-0000-0000-0000-000000000001",
///   "status": "Getting users from LDAP",
///   "error": "Connection timeout",
///   "warning": "Certificate not verified",
///   "percents": 40,
///   "certificateConfirmRequest": "{\"approved\":false}",
///   "source": "ldap.example.com",
///   "operationType": "Sync"
/// }
/// </example>
public class LdapStatusDto
{
    /// <summary>
    /// Whether the operation has finished, successfully or not. Poll the same operation until it is `true`, then
    /// read `error` to learn which of the two it was.
    /// </summary>
    /// <example>false</example>
    public bool Completed { get; set; }

    /// <summary>
    /// The identifier of the running operation, so a client can tell a fresh operation from the one it was
    /// already following. It is empty when no operation is running at all.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string Id { get; set; }

    /// <summary>
    /// What the operation is doing at the moment, as a sentence in the portal language rather than a code - do
    /// not branch on it; `completed`, `error` and `percents` are the fields to read.
    /// </summary>
    /// <example>Getting users from LDAP</example>
    public string Status { get; set; }

    /// <summary>
    /// Why the operation failed, in the portal language. It is empty while the operation is still running and on
    /// one that finished cleanly, so it is what distinguishes success from failure once `completed` is `true`.
    /// </summary>
    /// <example>Connection timeout</example>
    public string Error { get; set; }

    /// <summary>
    /// Something the operation went past but wants reported - a skipped account, a certificate it did not
    /// verify. Unlike `error` it does not mean the operation failed.
    /// </summary>
    /// <example>Certificate not verified</example>
    public string Warning { get; set; }

    /// <summary>
    /// How far along the operation is, from 0 to 100. It does not advance smoothly, since the stages differ
    /// wildly in length, so use `completed` and not this number to decide when to stop polling.
    /// </summary>
    /// <example>40</example>
    public int Percents { get; set; }

    /// <summary>
    /// The certificate the server offered, serialised, present only when the operation stopped to ask whether to
    /// trust it. Accept it by saving the settings again with the accept-certificate flag set.
    /// </summary>
    /// <example>{"approved":false}</example>
    public string CertificateConfirmRequest { get; set; }

    /// <summary>
    /// The directory server the operation is talking to, which is the stored server setting.
    /// </summary>
    /// <example>ldap.example.com</example>
    public string Source { get; set; }

    /// <summary>
    /// Which operation this is - a synchronisation, a dry run of one, a save with import, or a dry run of that -
    /// so a client polling one endpoint can tell whether the operation it is watching is its own.
    /// </summary>
    /// <example>Sync</example>
    public string OperationType { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class LdapStatusDtoMapper
{
    public static partial LdapStatusDto MapToDto(this LdapOperationStatus source);
}