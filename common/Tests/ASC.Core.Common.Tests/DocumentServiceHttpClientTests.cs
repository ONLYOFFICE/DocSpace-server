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
/// The named clients of the document service: each one carries a single resilience pipeline, however many of a
/// process's service extensions register them.
/// </summary>
/// <remarks>
/// The Monolith runs the services of every host in one process, so it calls <c>AddDocumentServiceHttpClient</c> once
/// for each host extension that registers the clients. Every call used to add another resilience handler to the same
/// named client, and the handlers nest: the outer one retries the inner one, so the retries multiply and the timeouts
/// stack.
/// </remarks>
public sealed class DocumentServiceHttpClientTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public DocumentServiceHttpClientTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();

        services.AddDocumentServiceHttpClient(configuration);
        services.AddDocumentServiceHttpClient(configuration);
        services.AddDocumentServiceHttpClient(configuration);

        _provider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RegisteredThreeTimes_ClientHasOneResiliencePipeline(bool sslVerification)
    {
        var handlers = HandlerChain(DocumentService.GetHttpClientName(sslVerification));

        handlers.Count(h => h.GetType().Name == "ResilienceHandler").Should().Be(1);
    }

    [Fact]
    public void RegisteredThreeTimes_SslIgnoringClientStillIgnoresCertificates()
    {
        var primary = HandlerChain(DocumentService.GetHttpClientName(sslVerification: false)).Last();

        primary.Should().BeOfType<SocketsHttpHandler>()
            .Which.SslOptions.RemoteCertificateValidationCallback.Should().NotBeNull();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private List<HttpMessageHandler> HandlerChain(string name)
    {
        var handler = _provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
        var chain = new List<HttpMessageHandler> { handler };

        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
            chain.Add(handler);
        }

        return chain;
    }
}
