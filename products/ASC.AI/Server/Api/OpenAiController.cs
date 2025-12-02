// (c) Copyright Ascensio System SIA 2009-2025
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

using System.Net.Http.Headers;

using ASC.Api.Core.Middleware;

using Microsoft.AspNetCore.Authorization;

namespace ASC.AI.Api
{
    [Scope]
    [DefaultRoute("openai/{providerId}/v1/{*path}")]
    [ApiController]
    [ControllerName("ai")]
    [AllowAnonymous]
    [SuppressCustomResponseFilter]
    public class OpenAiController(
        IHttpClientFactory httpClientFactory,
        AiProviderService aiProviderService
        ) : ControllerBase
    {
        [HttpGet, HttpPost, HttpPut, HttpDelete]
        public async Task<Stream> ProxyRequest([FromRoute] int providerId, [FromRoute] string path)
        {
            var provider = await aiProviderService.GetProviderAsync(providerId);

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(provider.Url.EndsWith('/') ? provider.Url : provider.Url + '/');

            var uri = path ?? "";
            if (!string.IsNullOrEmpty(Request.QueryString.Value))
            {
                uri += Request.QueryString.Value;
            }

            var request = new HttpRequestMessage(new HttpMethod(Request.Method), uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.Key);

            if (Request.ContentLength.HasValue && Request.ContentLength > 0)
            {
                request.Content = new StreamContent(Request.Body);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Request.Headers.ContentType.FirstOrDefault() ?? "application/json");
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Response.StatusCode = (int)response.StatusCode;
            Response.Headers.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            Response.Headers.ContentLength = response.Content.Headers.ContentLength;
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
