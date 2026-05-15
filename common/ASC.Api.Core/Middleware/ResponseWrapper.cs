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

using Microsoft.AspNetCore.Diagnostics;

namespace ASC.Api.Core.Middleware;

public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = (HttpStatusCode)context.Response.StatusCode;
        string message = null;

        if (status == HttpStatusCode.OK)
        {
            status = HttpStatusCode.InternalServerError;
        }

        var withStackTrace = true;
        var criticalException = false;

        switch (exception)
        {
            case ItemNotFoundException e:
                status = HttpStatusCode.NotFound;
                message = e.Message;
                break;
            case FileNotFoundException e:
                status = HttpStatusCode.NotFound;
                message = e.Message;
                break;
            case DirectoryNotFoundException e:
                status = HttpStatusCode.NotFound;
                message = e.Message;
                break;
            case ArgumentException e:
                status = HttpStatusCode.BadRequest;
                message = e.Message;
                break;
            case SecurityException:
            case AuthorizingException:
                status = HttpStatusCode.Forbidden;
                message = "Access denied";
                break;
            case BruteForceCredentialException:
            case RecaptchaException:
                status = HttpStatusCode.Forbidden;
                withStackTrace = false;
                break;
            case AuthenticationException:
                status = HttpStatusCode.Unauthorized;
                withStackTrace = false;
                break;
            case InvalidOperationException:
                status = HttpStatusCode.Forbidden;
                break;
            case TenantQuotaException:
            case BillingException:
            case AccountingPaymentRequiredException:
                status = HttpStatusCode.PaymentRequired;
                break;
            case CustomHttpException httpException:
                status = (HttpStatusCode)httpException.StatusCode;
                withStackTrace = false;
                break;
            case NotSupportedException:
                status = HttpStatusCode.UnsupportedMediaType;
                withStackTrace = false;
                break;
            default:
                criticalException = true;
                break;
        }

        if (criticalException)
        {
            logger.CriticalError(context.Request.Method, context.Request.Path.Value, exception);
        }
        else
        {
            logger.InformationError(context.Request.Method, context.Request.Path.Value, exception.Message, exception.InnerException?.Message);
        }

        var result = new ErrorApiResponse(status, exception, message, withStackTrace);

        context.Response.StatusCode = (int)status;

        await context.Response.WriteAsJsonAsync(result, cancellationToken);

        return true;
    }
}

public class CustomResponseFilterAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult result)
        {
            result.DeclaredType = typeof(SuccessApiResponse);
            result.Value = new SuccessApiResponse(context.HttpContext, result.Value);
        }
        if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new SuccessApiResponse(context.HttpContext, null));
        }

        base.OnResultExecuting(context);
    }
}
