﻿// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Collections.Concurrent;

namespace ASC.Api.Core.Middleware;

[Scope]
public class WebhooksGlobalFilterAttribute(
    IWebhookPublisher webhookPublisher,
    ILogger<WebhooksGlobalFilterAttribute> logger,
    SettingsManager settingsManager,
    DbWorker dbWorker)
    : ResultFilterAttribute, IDisposable
{
    private static readonly ConcurrentDictionary<string, Webhook> _webhooks = new();
    private MemoryStream _stream;
    private Stream _bodyStream;

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var (webhook, webhookData) = await GetWebhookAsync(context.HttpContext);
        var skip = webhook == null;

        if (!skip)
        {
            _stream = new MemoryStream();
            _bodyStream = context.HttpContext.Response.Body;
            context.HttpContext.Response.Body = _stream;
        }

        await base.OnResultExecutionAsync(context, next);

        if (context.Cancel || skip)
        {
            return;
        }

        if (_stream is { CanRead: true })
        {
            _stream.Position = 0;
            await _stream.CopyToAsync(_bodyStream);
            context.HttpContext.Response.Body = _bodyStream;

            try
            {
                var requestPayload = Encoding.UTF8.GetString(_stream.ToArray());

                await webhookPublisher.PublishAsync(webhook, requestPayload, webhookData);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }


    public void Dispose()
    {
        _stream?.Dispose();
    }

    private async Task<(Webhook webhook, WebhookData webhookData)> GetWebhookAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var endpoint = (RouteEndpoint)context.GetEndpoint();
        var routePattern = endpoint?.RoutePattern.RawText;
        var disabled = endpoint?.Metadata.OfType<WebhookDisableAttribute>().FirstOrDefault();

        if (routePattern == null)
        {
            return (null, null);
        }

        if (disabled != null)
        {
            return (null, null);
        }

        if (!DbWorker.MethodList.Contains(method))
        {
            return (null, null);
        }

        var key = $"{method}{routePattern}";
        if (!_webhooks.TryGetValue(key, out var webhook))
        {
            webhook = await dbWorker.GetWebhookAsync(method, routePattern);
            if (webhook != null)
            {
                _webhooks.TryAdd(key, webhook);
            }
        }

        if (webhook == null || (await settingsManager.LoadAsync<WebHooksSettings>()).Ids.Contains(webhook.Id))
        {
            return (null, null);
        }

        var webhookData = GetWebhookDataAsync(context, endpoint);

        return (webhook, webhookData);
    }

    private static WebhookData GetWebhookDataAsync(HttpContext context, RouteEndpoint endpoint)
    {
        var accessCheckerAttribute = endpoint?.Metadata.GetMetadata<WebhookAccessCheckerAttribute>();

        if (accessCheckerAttribute == null)
        {
            return null;
        }

        var returnType = GetReturnTypeFromRouteEndpoint(endpoint);

        var routeData = context.GetRouteData().Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        return new WebhookData
        {
            AccessCheckerType = accessCheckerAttribute.WebhookAccessCheckerType,
            ResponseType = returnType,
            RouteData = routeData
        };
    }

    private static Type GetReturnTypeFromRouteEndpoint(RouteEndpoint endpoint)
    {
        var returnType = endpoint?.Metadata?.GetMetadata<ControllerActionDescriptor>()?.MethodInfo?.ReturnType;

        if (returnType == null)
        {
             return null;
        }

        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? returnType.GetGenericArguments().First()
            : returnType;
    }
}