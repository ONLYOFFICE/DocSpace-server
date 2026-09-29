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

namespace ASC.Files.Core.ApiModels.WebhookDto;

/// <summary>
/// The pair of files carried by a form.submit webhook.
/// </summary>
/// <remarks>
/// A copy of SubmittedFormData. The only trigger whose payload is a wrapper rather than a bare entry, and the
/// only one whose webhook.target.id refers to a different entity - the original form - than the file that
/// changed. form.filled.out and form.stopped do not use this shape; they carry a plain FileWebhookDto.
/// </remarks>
[WebhookPayload(WebhookPayloadKind.FormSubmit)]
public class FormSubmitWebhookDto<T>
{
    /// <summary>
    /// The form the submission was filled from.
    /// </summary>
    public FileWebhookDto<T> OriginalForm { get; set; }

    /// <summary>
    /// The filled-in copy that was submitted.
    /// </summary>
    public FileWebhookDto<T> SubmittedForm { get; set; }
}
