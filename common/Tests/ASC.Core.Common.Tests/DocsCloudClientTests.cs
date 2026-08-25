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
/// Covers how a DocsCloud failure reaches the API caller: the Refit-backed <see cref="DocsCloudClient"/> is
/// driven with a fake primary handler (no network access), and the exception it raises is passed through the
/// real <see cref="CustomExceptionHandler"/> — the same object the API pipeline uses — so the assertions are
/// made on the status code and the error body the portal actually answers with.
/// </summary>
public class DocsCloudClientTests
{
    private const string BaseUrl = "https://docscloud.example.com/api/";
    private const string PortalId = "portal-1";
    private const string NotFoundMessage = "DocsCloud resource not found";

    private static readonly JsonSerializerOptions _responseOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// A portal whose DocsCloud tenant was never activated has no configuration to read: DocsCloud answers 404,
    /// which used to escape as an unhandled exception and turn into 500 Internal Server Error. It is a client
    /// error and is now reported as 400 Bad Request with the reason in the message.
    /// </summary>
    [Fact]
    [Trait("Bug", "83320")]
    public async Task GetTenantConfig_TenantNotActivated_IsReportedAsBadRequest()
    {
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.NotFound, "\"not found\""));

        var response = await CallAndHandleAsync(() => client.GetTenantConfigAsync(PortalId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error!.Message.Should().Be(NotFoundMessage);

        // A definitive client error is not worth retrying, and it is not a server fault, so no stack trace is exposed.
        handler.CallCount.Should().Be(1);
        response.Error.Stack.Should().BeNull();
    }

    /// <summary>
    /// Same as <see cref="GetTenantConfig_TenantNotActivated_IsReportedAsBadRequest"/>, for the tenant information
    /// endpoint. Note that the neighbouring "get tenant" endpoint is not affected: it swallows the 404 and answers
    /// 200 with no tenant.
    /// </summary>
    [Fact]
    [Trait("Bug", "83321")]
    public async Task GetTenantInfo_TenantNotActivated_IsReportedAsBadRequest()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.NotFound, "\"not found\""));

        var response = await CallAndHandleAsync(() => client.GetTenantInfoAsync(PortalId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error!.Message.Should().Be(NotFoundMessage);
    }

    /// <summary>
    /// Same as <see cref="GetTenantConfig_TenantNotActivated_IsReportedAsBadRequest"/>, for the usage endpoint.
    /// </summary>
    [Fact]
    [Trait("Bug", "83322")]
    public async Task GetTenantUsage_TenantNotActivated_IsReportedAsBadRequest()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.NotFound, "\"not found\""));

        var response = await CallAndHandleAsync(() => client.GetTenantUsageAsync(PortalId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error!.Message.Should().Be(NotFoundMessage);
    }

    /// <summary>
    /// Same as <see cref="GetTenantConfig_TenantNotActivated_IsReportedAsBadRequest"/>, for writing the
    /// configuration. The write is never retried, so the single 404 is what the caller sees.
    /// </summary>
    [Fact]
    [Trait("Bug", "83323")]
    public async Task UpdateTenantConfig_TenantNotActivated_IsReportedAsBadRequest()
    {
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.NotFound, "\"not found\""));

        var response = await CallAndHandleAsync(() => client.UpdateTenantConfigAsync(PortalId, new DocsCloudConfig { Wopi = new DocsCloudWopiConfig { Enable = true } }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error!.Message.Should().Be(NotFoundMessage);
        handler.CallCount.Should().Be(1);
    }

    /// <summary>
    /// Same as <see cref="GetTenantConfig_TenantNotActivated_IsReportedAsBadRequest"/>, for the quota endpoint.
    /// </summary>
    [Fact]
    [Trait("Bug", "83325")]
    public async Task GetTenantQuota_TenantNotActivated_IsReportedAsBadRequest()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.NotFound, "\"not found\""));

        var response = await CallAndHandleAsync(() => client.GetTenantQuotaAsync(PortalId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error!.Message.Should().Be(NotFoundMessage);
    }

    /// <summary>
    /// Only the "no such tenant" case is a client error. A DocsCloud service failure stays a 500, so the
    /// mapping cannot hide a broken dependency behind a 400.
    /// </summary>
    [Fact]
    public async Task GetTenantConfig_ServiceFailure_StaysInternalServerError()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.InternalServerError, "\"boom\""));

        var response = await CallAndHandleAsync(() => client.GetTenantConfigAsync(PortalId));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Runs the call the way the API pipeline does: the exception it throws is handed to
    /// <see cref="CustomExceptionHandler"/>, and the response it writes is read back.
    /// </summary>
    private static async Task<ErrorApiResponse> CallAndHandleAsync<T>(Func<Task<T>> call)
    {
        var exception = await Record.ExceptionAsync(call);

        exception.Should().NotBeNull();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await new CustomExceptionHandler(NullLogger<CustomExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        handled.Should().BeTrue();

        context.Response.Body.Position = 0;

        var response = await JsonSerializer.DeserializeAsync<ErrorApiResponse>(context.Response.Body, _responseOptions, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        // The handler writes the status on the response and repeats it in the body; they must agree.
        response!.StatusCode.Should().Be((HttpStatusCode)context.Response.StatusCode);

        return response;
    }

    private static (DocsCloudClient client, CapturingHandler handler) CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:docscloud:url"] = BaseUrl,
                ["core:docscloud:key"] = "test-key",
                ["core:docscloud:secret"] = "test-secret"
            })
            .Build();

        var handler = new CapturingHandler(responder);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<DocsCloudClient>();
        services.AddFusionCache();

        services.AddDocsCloudHttpClient(configuration);

        // Replace the real network handler with our capturing one.
        services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<DocsCloudClient>(), handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            var response = responder(request);

            // Mimic the real primary handler so the resilience pipeline can inspect the request.
            response.RequestMessage = request;

            return Task.FromResult(response);
        }
    }
}
