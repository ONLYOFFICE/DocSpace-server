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

namespace ASC.Tests.Common.ApiFactories;

/// <summary>
/// Everything a portal (tenant) is created with: its alias, its tenant id, its owner, the base address
/// of every started service, and the factory the fixture uses to hand out clients on its shared
/// connection pool.
/// </summary>
/// <remarks>
/// <paramref name="TenantId"/> is for a suite that reaches the platform in-process rather than over
/// HTTP — the alias is enough to scope a request through the <c>Origin</c> header, but
/// <c>TenantManager.SetCurrentTenantAsync</c> wants the id.
/// </remarks>
public sealed record PortalContext(
    string PortalName,
    int TenantId,
    User Owner,
    IReadOnlyDictionary<string, Uri> BaseAddresses,
    Func<Uri, string?, HttpClient> CreateClient);

/// <summary>
/// A self-contained set of HTTP clients bound to a single portal via the <c>Origin</c> header.
/// Each test owns one instance, which makes tests fully independent and safe to run in parallel.
/// </summary>
/// <remarks>
/// The base owns the WebApi client, because that is where authentication lives: every client it
/// hands out through <see cref="CreateClient"/> is wired to it, so <c>HttpClient.Authenticate(user)</c>
/// works on all of them. A suite derives from this and adds only the typed API clients it uses.
/// </remarks>
public abstract class PortalClientsBase : IDisposable
{
    private readonly PortalContext _context;
    private readonly List<HttpClient> _httpClients = [];

    /// <summary>The portal (tenant) alias these clients are bound to.</summary>
    public string PortalName => _context.PortalName;

    /// <summary>The tenant id of this portal, for anything that addresses the platform in-process.</summary>
    public int TenantId => _context.TenantId;

    /// <summary>The owner of this portal. Its Id is unique per portal.</summary>
    public User Owner => _context.Owner;

    public HttpClient WebApiHttpClient { get; }

    /// <summary>Raw access to ASC.Web.Api — also the endpoint every client signs in through.</summary>
    public RawApiClient WebApi { get; }

    protected PortalClientsBase(PortalContext context)
    {
        _context = context;

        // First, and without registration: it is itself the authentication endpoint the others use.
        WebApiHttpClient = NewHttpClient(ResourceNames.WebApi);
        WebApi = new RawApiClient(WebApiHttpClient);

        Initializer.RegisterAuthApi(WebApiHttpClient, WebApi);
    }

    /// <summary>
    /// Creates a client for the given Aspire resource, bound to this portal and able to sign in.
    /// The client is disposed together with the bundle.
    /// </summary>
    protected HttpClient CreateClient(string resourceName)
    {
        var client = NewHttpClient(resourceName);

        Initializer.RegisterAuthApi(client, WebApi);

        return client;
    }

    /// <summary>The base address of a service, in the form the generated SDK's <c>BasePath</c> expects.</summary>
    protected string BasePathOf(string resourceName)
    {
        return _context.BaseAddresses[resourceName].ToString().TrimEnd('/');
    }

    private HttpClient NewHttpClient(string resourceName)
    {
        if (!_context.BaseAddresses.TryGetValue(resourceName, out var baseAddress))
        {
            throw new InvalidOperationException(
                $"The '{resourceName}' resource was not started. Add it to the fixture's Resources.");
        }

        // The clients are per-test (own Origin/Auth headers) but share the fixture's connection pool.
        var client = _context.CreateClient(baseAddress, $"http://{PortalName}");

        _httpClients.Add(client);

        return client;
    }

    public void Dispose()
    {
        foreach (var client in _httpClients)
        {
            Initializer.UnregisterAuthApi(client);
            client.Dispose();
        }

        _httpClients.Clear();

        GC.SuppressFinalize(this);
    }
}
