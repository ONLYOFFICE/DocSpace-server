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
/// process's service extensions register them, the clients behind the "SSL verification off" switch accept any
/// certificate, and a timeout or retry count that does not parse or is out of bounds neither stops the registration
/// nor breaks the clients.
/// </summary>
/// <remarks>
/// The Monolith runs the services of every host in one process, so it calls <c>AddDocumentServiceHttpClient</c> once
/// for each host extension that registers the clients. Every call used to add another resilience handler to the same
/// named client, and the handlers nest: the outer one retries the inner one, so a request that fails fast was tried
/// many more times than configured, within the client's 100-second timeout.
/// </remarks>
public sealed class DocumentServiceHttpClientTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public DocumentServiceHttpClientTests()
    {
        var configuration = Configuration(timeout: null, @try: null);
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
        HttpHandlerProbe.ResilienceHandlers(_provider, DocumentService.GetHttpClientName(sslVerification)).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SslIgnoringClients_AcceptAnUntrustedCertificate(bool customClient)
    {
        var name = customClient ? DocumentService.CustomSslVerificationClient : DocumentService.GetHttpClientName(sslVerification: false);

        HttpHandlerProbe.AcceptsUntrustedCertificate(HttpHandlerProbe.Primary(_provider, name)).Should().BeTrue();
    }

    [Fact]
    public void VerifyingClient_KeepsTheDefaultCertificateValidation()
    {
        var primary = HttpHandlerProbe.Primary(_provider, DocumentService.GetHttpClientName(sslVerification: true));

        HttpHandlerProbe.AcceptsUntrustedCertificate(primary).Should().BeFalse();
    }

    /// <summary>
    /// The values used to be read with <c>Convert.ToInt32</c> during the registration, so one that did not parse
    /// stopped the host from starting; a timeout under a second became a zero timeout and a retry count of 0 or less
    /// an invalid strategy, which failed every request once the host ran.
    /// </summary>
    [Theory]
    [InlineData("x", null)]
    [InlineData("500", null)]
    [InlineData("0", null)]
    [InlineData(null, "x")]
    [InlineData(null, "0")]
    [InlineData(null, "-1")]
    public void BrokenSettings_ClientsStillRegisterAndBuild(string? timeout, string? @try)
    {
        var services = new ServiceCollection();

        services.AddDocumentServiceHttpClient(Configuration(timeout, @try));

        using var provider = services.BuildServiceProvider();

        HttpHandlerProbe.ResilienceHandlers(provider, DocumentService.GetHttpClientName(sslVerification: true)).Should().Be(1);
        HttpHandlerProbe.ResilienceHandlers(provider, DocumentService.GetHttpClientName(sslVerification: false)).Should().Be(1);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private static IConfiguration Configuration(string? timeout, string? @try)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["files:docservice:timeout"] = timeout,
                ["files:docservice:try"] = @try
            })
            .Build();
    }
}
