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
/// The hosts whose service extensions register the document service clients do register them: both clients carry
/// their resilience pipeline, and the client behind the "SSL verification off" switch accepts any certificate.
/// </summary>
/// <remarks>
/// A host that does not register them gets the default handler for these names, with no retries and no way to turn
/// certificate validation off, and nothing fails until a request reaches a document server with a self-signed
/// certificate. People and AI.Worker register them in their service extensions, which can be called on a bare
/// service collection. Files.Server, Files.Worker and Web.Api did so before; ApiSystem registers them in
/// <c>ConfigureServices</c>, which needs Redis and the DI scan, and is not checked here. Files.Server registers the
/// client of the template gallery (AddTemplateGalleryPipelines) the same way, and the Monolith through it.
/// </remarks>
public sealed class HostHttpClientRegistrationTests
{
    [Fact]
    public void People_RegistersTheDocumentServiceClients()
    {
        var services = new ServiceCollection();

        services.AddPeopleServices(EmptyConfiguration());

        AssertDocumentServiceClients(services);
    }

    [Fact]
    public void AiWorker_RegistersTheDocumentServiceClients()
    {
        var services = new ServiceCollection();

        services.AddAiWorkerServices(EmptyConfiguration());

        AssertDocumentServiceClients(services);
    }

    [Fact]
    [Trait("Bug", "78009")]
    public void FilesServer_RegistersTheTemplateGalleryClient()
    {
        // Files.Server, and the Monolith through it, run the downloads from the template gallery. Without the
        // registration the factory hands out the default handler for the name: no retries and no circuit breaker, and
        // cookies shared by all tenants (bug 83399)
        var services = new ServiceCollection();

        services.AddFilesServerServices(EmptyConfiguration());

        using var provider = services.BuildServiceProvider();
        HttpHandlerProbe.ResilienceHandlers(provider, OFormRequestManager.HttpClientName).Should().Be(1);
        HttpHandlerProbe.Primary(provider, OFormRequestManager.HttpClientName).UseCookies.Should().BeFalse();
    }

    private static void AssertDocumentServiceClients(ServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        HttpHandlerProbe.ResilienceHandlers(provider, DocumentService.GetHttpClientName(sslVerification: true)).Should().Be(1);
        HttpHandlerProbe.ResilienceHandlers(provider, DocumentService.GetHttpClientName(sslVerification: false)).Should().Be(1);
        HttpHandlerProbe.AcceptsUntrustedCertificate(HttpHandlerProbe.Primary(provider, DocumentService.CustomSslVerificationClient)).Should().BeTrue();
    }

    private static IConfiguration EmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }
}
