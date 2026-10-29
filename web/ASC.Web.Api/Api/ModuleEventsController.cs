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

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// The entry point through which integration modules - the server side of a mail, chat or task application embedded
/// in DocSpace - push real-time events to the portal's browsers over the socket connection they already have.
/// A module authenticates with its own key, not with a user's credentials, and reaches only the portals its key is
/// configured for.
/// </remarks>
/// <name>portal</name>
[Scope]
[ApiEndpoint("portal", "moduleEvents")]
[AllowAnonymous]
public partial class ModuleEventsController(
    IConfiguration configuration,
    TenantManager tenantManager,
    UserManager userManager,
    PluginSocketManager pluginSocketManager,
    ILogger<ModuleEventsController> logger) : ControllerBase
{
    // Under the "plugins" section of the web plugins: one entry per module, {module}:keyHash (SHA-256 of the key,
    // hex) and {module}:tenants ("*" or comma-separated tenant ids). Read on every call, so a changed file applies
    // without a restart.
    private const string ModulesSection = "plugins:socketEvents:modules";

    // the socket service accepts bodies up to 100 KB and drops a larger one with no trace
    private const int MaxBodyBytes = 64 * 1024;

    /// <remarks>
    /// Delivers one event from an integration module to the browsers of the portal named by the request host, over
    /// the socket connection every open DocSpace page already keeps. No user session is needed: the caller
    /// identifies itself with the module key in the `X-Module-Key` header, which the installation administrator
    /// issues and allows for a list of portals. Set exactly one addressee - `user`, the e-mail of a portal user,
    /// reaches only that user's open sessions without any subscription, and `room` reaches everyone in the portal
    /// subscribed to `plugin-{module}-{room}`. Browsers receive `plugin:{module}:{event}` with `{ message }`, the
    /// module name taken from the key, never from the body. The call only hands the event over: nothing is stored,
    /// a user with no open session never sees it, and a repeated call delivers it again. Keep `message` a signal to
    /// re-read, such as an unread count. The body is limited to 64 KB. A missing or unknown key fails with 401, a
    /// portal the key is not allowed for with 403, an unknown or inactive user with 404, and invalid names or not
    /// exactly one addressee with 400.
    /// </remarks>
    /// <summary>
    /// Send a module event
    /// </summary>
    /// <path>api/2.0/portal/moduleEvents</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Portal / Module events")]
    [SwaggerResponse(200, "The event was handed to the socket service for delivery")]
    [SwaggerResponse(400, "The body is missing, the event or room name is invalid, or there is not exactly one addressee")]
    [SwaggerResponse(401, "The X-Module-Key header is missing or matches no configured module")]
    [SwaggerResponse(402, "The portal's payment has lapsed")]
    [SwaggerResponse(403, "The module key is not allowed for this portal")]
    [SwaggerResponse(404, "No active user of this portal has the given e-mail")]
    [SwaggerResponse(413, "The body is larger than 64 KB")]
    [HttpPost]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task SendModuleEvent(SendModuleEventRequestDto inDto)
    {
        var module = FindModule(inDto.ModuleKey);
        if (module is null)
        {
            logger.WarningUnknownModuleKey();
            throw new AuthenticationException("The module key is not valid");
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!IsAllowed(module, tenant.Id))
        {
            logger.WarningModuleNotAllowed(module, tenant.Id);
            throw new SecurityException("The module key is not allowed for this portal");
        }

        var @event = inDto.Event ?? throw new ArgumentException("The body is missing");

        if (!NameRegex().IsMatch(@event.Event ?? string.Empty))
        {
            throw new ArgumentException("event must be 1-128 letters, digits or the characters _ . : -");
        }

        var toUser = !string.IsNullOrEmpty(@event.User);
        if (toUser == !string.IsNullOrEmpty(@event.Room))
        {
            throw new ArgumentException("Set exactly one addressee: room or user");
        }

        if (!toUser && !NameRegex().IsMatch(@event.Room))
        {
            throw new ArgumentException("room must be 1-128 letters, digits or the characters _ . : -");
        }

        Guid? userId = null;
        if (toUser)
        {
            // the lookup runs in the request's portal: an address from another portal finds nobody here
            var user = await userManager.GetUserByEmailAsync(@event.User);
            if (user.Id == ASC.Core.Users.Constants.LostUser.Id || user.Status != EmployeeStatus.Active)
            {
                throw new ItemNotFoundException("No active user with this e-mail in the portal");
            }

            userId = user.Id;
        }

        await pluginSocketManager.SendAsync(tenant.Id, module, toUser ? null : @event.Room, userId, @event.Event, @event.Message);
    }

    /// <returns>The module the key is configured for, or null.</returns>
    private string FindModule(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        var presented = SHA256.HashData(Encoding.UTF8.GetBytes(key));

        foreach (var entry in configuration.GetSection(ModulesSection).GetChildren())
        {
            // the module names the events and the rooms, so it has to be usable inside both names
            if (!ModuleRegex().IsMatch(entry.Key) || !TryParseHash(entry["keyHash"], out var expected))
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(presented, expected))
            {
                return entry.Key;
            }
        }

        return null;
    }

    private static bool TryParseHash(string hex, out byte[] hash)
    {
        hash = null;

        if (string.IsNullOrEmpty(hex) || hex.Length != SHA256.HashSizeInBytes * 2)
        {
            return false;
        }

        try
        {
            hash = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool IsAllowed(string module, int tenantId)
    {
        var allowed = configuration[$"{ModulesSection}:{module}:tenants"];
        if (string.IsNullOrWhiteSpace(allowed))
        {
            return false;
        }

        if (allowed.Trim() == "*")
        {
            return true;
        }

        return allowed
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(id => int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var allowedId) && allowedId == tenantId);
    }

    [GeneratedRegex("^[A-Za-z0-9_.:-]{1,128}$")]
    private static partial Regex NameRegex();

    // no colon: it separates the module from the event in "plugin:{module}:{event}"
    [GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial Regex ModuleRegex();
}
