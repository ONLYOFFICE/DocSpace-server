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

namespace ASC.Core.Common.Tests;

/// <summary>
/// Reads what <see cref="IHttpMessageHandlerFactory"/> really builds for a client name: the handler chain, the
/// primary handler at its end, and whether that handler accepts a certificate whose chain does not validate.
/// </summary>
internal static class HttpHandlerProbe
{
    /// <summary>
    /// The handlers built for <paramref name="name"/>, the outermost first and the primary handler last. The factory
    /// wraps the primary handler in its own delegating handlers (lifetime tracking, logging) and in the ones the
    /// registration adds (resilience).
    /// </summary>
    public static List<HttpMessageHandler> Chain(IServiceProvider provider, string name)
    {
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
        var chain = new List<HttpMessageHandler> { handler };

        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
            chain.Add(handler);
        }

        return chain;
    }

    /// <summary>
    /// The primary handler built for <paramref name="name"/>; the assertion fails if it is not a
    /// <see cref="SocketsHttpHandler"/>.
    /// </summary>
    public static SocketsHttpHandler Primary(IServiceProvider provider, string name)
    {
        return Chain(provider, name).Last().Should().BeOfType<SocketsHttpHandler>().Subject;
    }

    /// <summary>
    /// How many resilience handlers the chain built for <paramref name="name"/> holds.
    /// </summary>
    public static int ResilienceHandlers(IServiceProvider provider, string name)
    {
        return Chain(provider, name).Count(handler => handler.GetType().Name == "ResilienceHandler");
    }

    /// <summary>
    /// Whether the handler would go on with a self-signed certificate whose chain does not validate. Without a
    /// callback of its own, the handler applies the default validation, which refuses it.
    /// </summary>
    public static bool AcceptsUntrustedCertificate(SocketsHttpHandler handler)
    {
        var callback = handler.SslOptions.RemoteCertificateValidationCallback;

        if (callback is null)
        {
            return false;
        }

        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=untrusted.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var chain = new X509Chain();

        return callback(handler, certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors);
    }
}
