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
/// The standard HTTP clients that shared code asks for by name, and the handler settings each name promises
/// (<c>.claude/rules/http-clients.md</c>).
/// </summary>
/// <remarks>
/// <para>
/// The test reads the primary handler that the factory really builds for each name. A name that
/// <c>AddStandardHttpClients</c> leaves out fails here as well, because <see cref="IHttpClientFactory"/> does not
/// refuse an unregistered name: it serves the default handler, which keeps cookies and follows redirects.
/// </para>
/// <para>
/// The test checks the registration itself, not the hosts that call it. That <c>BaseStartup</c>,
/// <c>BaseWorkerStartup</c>, ApiSystem and Site.Core call <c>AddStandardHttpClients</c> is not checked here: their
/// registrations run inside <c>ConfigureServices</c>, which needs Redis and the DI scan. In the workers, only
/// <c>customHttpClientNoCookie</c> is covered, for Files.Worker, by <c>ThirdPartyWorkerSessionIsolationTests</c> in
/// the Files suite; no test notices another name missing from a host.
/// </para>
/// </remarks>
public sealed class StandardHttpClientsTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStandardHttpClients().BuildServiceProvider();

    [Fact]
    [Trait("Bug", "83399")]
    public void NoCookieClient_KeepsNoCookies()
    {
        var handler = PrimaryHandler("customHttpClientNoCookie");

        handler.UseCookies.Should().BeFalse();
    }

    [Fact]
    public void CustomHttpClient_DoesNotFollowRedirects()
    {
        var handler = PrimaryHandler("customHttpClient");

        handler.AllowAutoRedirect.Should().BeFalse();
        handler.SslOptions.RemoteCertificateValidationCallback.Should().BeNull();
    }

    [Fact]
    public void SslIgnoreClient_DoesNotFollowRedirectsAndAcceptsAnyCertificate()
    {
        var handler = PrimaryHandler("customHttpClientSslIgnore");

        handler.AllowAutoRedirect.Should().BeFalse();
        AcceptsUntrustedCertificate(handler).Should().BeTrue();
    }

    [Theory]
    [InlineData(UrlValidator.PinnedHttpClient, false)]
    [InlineData(UrlValidator.PinnedHttpClientSslIgnore, true)]
    public void PinnedClients_ConnectThroughThePinnedCallback(string name, bool ignoresCertificates)
    {
        var handler = PrimaryHandler(name);
        using var client = _provider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
        Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> pinned = UrlValidator.PinnedConnectCallback;

        handler.ConnectCallback.Should().Be(pinned);
        handler.AllowAutoRedirect.Should().BeFalse();
        AcceptsUntrustedCertificate(handler).Should().Be(ignoresCertificates);
        client.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    [Trait("Bug", "78009")]
    public async Task TemplateGallery_GetsItsDeadlinesWithoutARegistrationOfItsOwn()
    {
        // The request manager of the template gallery takes its deadlines from the registry of resilience pipelines in
        // every host that creates FileStorageService, also in one that registers neither its pipelines nor any other
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["files:oform:domain"] = "http://127.0.0.1:1",
                ["files:oform:path"] = "/api/oforms"
            })
            .Build();
        using var cache = new FusionCache(new FusionCacheOptions());
        var manager = ActivatorUtilities.CreateInstance<OFormRequestManager>(_provider,
            NullLogger<OFormRequestManager>.Instance, configuration, cache);

        var exception = await Assert.ThrowsAsync<CustomHttpException>(() => manager.Get(1, ".pdf"));

        exception.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private SocketsHttpHandler PrimaryHandler(string name)
    {
        return HttpHandlerProbe.Primary(_provider, name);
    }

    private static bool AcceptsUntrustedCertificate(SocketsHttpHandler handler)
    {
        return HttpHandlerProbe.AcceptsUntrustedCertificate(handler);
    }
}
