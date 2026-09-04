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

using System.Collections.Concurrent;

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
            case UnauthorizedAccessException:
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
            // Asking for the configuration, quota or usage of a DocsCloud tenant the portal never activated is a
            // client error, not a server fault: the caller is told what is missing, without a stack trace.
            // The other DocsCloud failures (service not configured, authorization rejected) are ours to fix,
            // so they stay 500 and keep being logged as critical.
            case DocsCloudNotFoundException e:
                status = HttpStatusCode.BadRequest;
                message = e.Message;
                withStackTrace = false;
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

/// <summary>
/// Marks a controller or action whose responses must not be wrapped into <see cref="SuccessApiResponse"/>
/// by <see cref="CustomResponseFilterAttribute"/>. Used by services (e.g. ASC.ApiSystem) that expose a flat
/// response shape when their controllers are hosted inside the common (monolith) pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableResponseWrapperAttribute : Attribute;

public class CustomResponseFilterAttribute : ResultFilterAttribute
{
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is DisableResponseWrapperAttribute))
        {
            await next();

            return;
        }

        if (context.Result is ObjectResult result)
        {
            // An IAsyncEnumerable<T> result is buffered here, on the request's own async path, so the response
            // serializer (DynamicIgnoreConverter) can stay synchronous. Nothing is lost: the previous converter already
            // buffered every property into a MemoryStream before writing it, it just did so by blocking a thread-pool
            // thread on the enumeration.
            result.DeclaredType = typeof(SuccessApiResponse);
            result.Value = new SuccessApiResponse(context.HttpContext, await AsyncEnumerableBuffer.MaterializeAsync(result.Value, context.HttpContext.RequestAborted));
        }
        if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new SuccessApiResponse(context.HttpContext, null));
        }

        await next();
    }
}

/// <summary>
/// Turns an <see cref="IAsyncEnumerable{T}"/> of unknown <c>T</c> into a <see cref="List{T}"/>; anything else is returned as is.
/// </summary>
internal static class AsyncEnumerableBuffer
{
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _readers = new();
    private static readonly MethodInfo _readMethod = typeof(AsyncEnumerableBuffer).GetMethod(nameof(ReadAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static Task<object> MaterializeAsync(object value, CancellationToken token)
    {
        if (value == null)
        {
            return Task.FromResult<object>(null);
        }

        var reader = _readers.GetOrAdd(value.GetType(), static type =>
        {
            var asyncEnumerable = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
            if (asyncEnumerable == null)
            {
                return null;
            }

            return _readMethod.MakeGenericMethod(asyncEnumerable.GetGenericArguments()[0]).CreateDelegate<Func<object, CancellationToken, Task<object>>>();
        });

        return reader == null ? Task.FromResult(value) : reader(value, token);
    }

    private static async Task<object> ReadAsync<T>(object source, CancellationToken token)
    {
        var list = new List<T>();
        await foreach (var item in ((IAsyncEnumerable<T>)source).WithCancellation(token))
        {
            list.Add(item);
        }

        return list;
    }
}
