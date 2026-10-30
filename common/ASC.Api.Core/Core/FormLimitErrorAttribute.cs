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

namespace ASC.Api.Core.Core;

/// <summary>
/// Gives a multipart upload that breaks the <see cref="RequestFormLimitsAttribute.MultipartBodyLengthLimit"/> of its
/// action a readable 400: "The maximum file size is exceeded (100 MB)." Without it the form is first read by model
/// binding, which reports the failure through model state in the framework's own words ("Failed to read the request
/// form. Multipart body length limit 104857600 exceeded.").
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FormLimitErrorAttribute : Attribute, IAsyncResourceFilter, IOrderedFilter
{
    /// <summary>
    /// After <see cref="RequestFormLimitsAttribute"/> (900), which installs the form reader with the limits of the action.
    /// </summary>
    public int Order => 1000;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var formLimits = context.ActionDescriptor.EndpointMetadata.OfType<RequestFormLimitsAttribute>().LastOrDefault();

        if (formLimits != null && request.HasFormContentType)
        {
            var limit = formLimits.MultipartBodyLengthLimit;

            try
            {
                await request.ReadFormAsync(context.HttpContext.RequestAborted);
            }
            // A part can outgrow the limit only in a body that does, so a smaller body failed for another reason.
            catch (InvalidDataException) when (request.ContentLength > limit)
            {
                throw new ArgumentException(FileSizeComment.GetFileSizeExceptionString(limit));
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                // Any other unreadable form is left to model binding: it gets the same failed read and reports it as before.
            }
        }

        await next();
    }
}
