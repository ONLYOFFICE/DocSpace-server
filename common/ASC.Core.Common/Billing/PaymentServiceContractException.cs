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


namespace ASC.Core.Billing;

/// <summary>
/// A payment service - billing, accounting or docscloud - answered, but not with what the call expects: an HTML page of
/// a proxy in front of it, a contract changed on its side. This is no refusal of the service, and the request may well
/// have been applied, so the type stays outside <see cref="BillingException"/>, <see cref="AccountingException"/> and
/// <see cref="DocsCloudException"/>: a caller that handles a refusal - a 402 for the client, a backup schedule dropped
/// for an unpaid portal - does not handle this one, and the API reports it as a server error.
/// </summary>
public class PaymentServiceContractException(string service, string operation, HttpStatusCode? statusCode, Exception inner)
    : Exception(statusCode is { } status
        ? $"{service} answer to {operation} ({(int)status}) could not be read"
        : $"{service} answer to {operation} could not be read", inner)
{
    /// <summary>The service that answered: <c>Billing</c>, <c>Accounting</c> or <c>DocsCloud</c>.</summary>
    public string Service { get; } = service;

    /// <summary>The method and the route template of the call, without the values the route was filled with.</summary>
    public string Operation { get; } = operation;

    /// <summary>The status of the unreadable answer, when the client still had it.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;

    /// <summary>The exception for an answer Refit could not deserialize.</summary>
    internal static ValueTask<Exception> FromResponse(string service, HttpResponseMessage response, Exception exception)
    {
        return ValueTask.FromResult<Exception>(
            new PaymentServiceContractException(service, PaymentServiceOperation.Of(response.RequestMessage), response.StatusCode, exception));
    }
}

/// <summary>
/// How a request to a payment service is named in an exception: the method and the Refit route template. The filled
/// address carries the internal host of the service and the portal key, which an exception passes on to the logs
/// and, unhandled, to the client of the API.
/// </summary>
internal static class PaymentServiceOperation
{
    public static string Of(HttpRequestMessage request)
    {
        if (request is null)
        {
            return "a request";
        }

        IDictionary<string, object> options = request.Options;

        if (options.TryGetValue(HttpRequestMessageOptions.RelativePathTemplate, out var template) && template is string path)
        {
            return $"{request.Method} {path}";
        }

        return options.TryGetValue(HttpRequestMessageOptions.MethodName, out var method) && method is string methodName
            ? $"{request.Method} {methodName}"
            : $"{request.Method} request";
    }
}
