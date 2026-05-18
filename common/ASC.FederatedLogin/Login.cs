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

namespace ASC.FederatedLogin;

[Scope]
public class Login(
    IWebHostEnvironment webHostEnvironment,
    ProviderManager providerManager,
    LoginProfileTransport loginProfileTransport)
{
    private string Callback => _params.Get("callback") ?? "loginCallback";
    private string Auth => _params.Get("auth");
    private string ReturnUrl => _params.Get("returnurl"); //TODO?? FormsAuthentication.LoginUrl;
    private string Pure => _params.Get("pure");

    private LoginMode Mode
    {
        get
        {
            if (!string.IsNullOrEmpty(_params.Get("mode")))
            {
                return Enum.Parse<LoginMode>(_params.Get("mode"), true);
            }

            return LoginMode.Popup;
        }
    }

    protected bool Minimal
    {
        get
        {
            if (_params.ContainsKey("min"))
            {
                bool.TryParse(_params.Get("min"), out var result);

                return result;
            }

            return false;
        }
    }

    private Dictionary<string, string> _params;

    public async Task InvokeAsync(HttpContext context)
    {
        _ = context.PushRewritenUri();

        if (string.IsNullOrEmpty(context.Request.Query["p"]))
        {
            _params = new Dictionary<string, string>(context.Request.Query.Count);
            //Form params and redirect
            foreach (var key in context.Request.Query.Keys)
            {
                _params.Add(key, context.Request.Query[key]);
            }

            //Pack and redirect
            var uriBuilder = new UriBuilder(context.Request.Url());
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_params)));
            uriBuilder.Query = "p=" + token;
            context.Response.Redirect(uriBuilder.Uri.ToString(), true);
            await context.Response.CompleteAsync();

            return;
        }

        try
        {
            _params = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(context.Request.Query["p"])));
        }
        catch (Exception e)
        {
            throw new ArgumentException(e.Message);
        }

        if (!string.IsNullOrEmpty(Auth))
        {
            try
            {
                var desktop = _params.ContainsKey("desktop") && _params["desktop"] == "true";
                Dictionary<string, string> additionalStateArgs = null;

                if (desktop)
                {
                    additionalStateArgs = context.Request.Query.ToDictionary(r => r.Key, r => r.Value.FirstOrDefault());
                    additionalStateArgs.TryAdd("desktop", "true");
                }

                var profile = providerManager.Process(Auth, context, null, additionalStateArgs);
                if (profile != null)
                {
                    await SendJsCallbackAsync(context, profile);
                }
            }
            catch (ThreadAbortException)
            {
                //Thats is responce ending
            }
            catch (Exception ex)
            {
                await SendJsCallbackAsync(context, new LoginProfile(ex));
            }
        }
        else
        {
            //Render xrds
            await RenderXrdsAsync(context);
        }
        context.PopRewritenUri();
    }

    private async Task RenderXrdsAsync(HttpContext context)
    {
        var xrdsloginuri = new Uri(context.Request.Url(), new Uri(context.Request.Url().AbsolutePath, UriKind.Relative)) + "?auth=openid&returnurl=" + ReturnUrl;
        var xrdsimageuri = new Uri(context.Request.Url(), new Uri(webHostEnvironment.WebRootPath, UriKind.Relative)) + "openid.gif";
        await XrdsHelper.RenderXrdsAsync(context.Response, xrdsloginuri, xrdsimageuri);
    }

    private async Task SendJsCallbackAsync(HttpContext context, LoginProfile profile)
    {
        bool.TryParse(Pure, out var pureTransport);

        var desktop = Mode == LoginMode.Redirect;
        var returnUrl = desktop && !string.IsNullOrWhiteSpace(ReturnUrl) ? ReturnUrl : "/";

        //Render a page
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(
            JsCallbackHelper.GetCallbackPage()
            .Replace("%PROFILE%", $"\"{await loginProfileTransport.ToString(profile, pureTransport)}\"")
            .Replace("%CALLBACK%", Callback)
            .Replace("%RETURNURL%", $"\"{returnUrl}\"")
            .Replace("%DESKTOP%", desktop.ToString().ToLowerInvariant())
            );
    }
}

public class LoginHandler
{
    public LoginHandler(RequestDelegate _)
    {
    }

    public async Task InvokeAsync(HttpContext context, Login login)
    {
        await login.InvokeAsync(context);
    }
}

public static class LoginHandlerExtensions
{
    public static IApplicationBuilder UseLoginHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoginHandler>();
    }
}