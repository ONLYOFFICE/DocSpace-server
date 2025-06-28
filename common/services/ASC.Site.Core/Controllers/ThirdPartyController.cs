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

using System.Reflection;

namespace ASC.Site.Core.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class ThirdPartyController(IHttpClientFactory clientFactory, ApiSystemHelper apiSystemHelper) : ControllerBase
{
    /// <path>/thirdparty</path>
    [HttpGet("")]
    [AllowAnonymous]
    public ContentResult IndexHtml()
    {
        using var reader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("ASC.Site.Core.index.html"));

        var html = reader.ReadToEnd();

        return Content(html, "text/html");
    }

    /// <path>/thirdparty/{provider}/code</path>
    [SwaggerResponse(400, "Error")]
    [HttpGet("{provider}/code")]
    [AllowAnonymous]
    public ContentResult GetThirdPartyProviderCode()
    {
        using var reader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("ASC.Site.Core.thirdparty.html"));

        var html = reader.ReadToEnd();

        return Content(html, "text/html"); 
    }

    public record RegisterDto(string ThirdPartyProfile, string Email);

    /// <path>/thirdparty/register</path>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<string> RegisterAsync(RegisterDto inDto)
    {
        if (string.IsNullOrEmpty(inDto.ThirdPartyProfile) && string.IsNullOrEmpty(inDto.Email))
        {
            throw new ArgumentException();
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri("http://localhost:8092/apisystem/portal/registerbyemail"),
            Method = HttpMethod.Post
        };

        var auth = apiSystemHelper.CreateAuthToken(Guid.NewGuid().ToString());
        auth = auth.Substring(0, auth.Length - 1);

        request.Headers.Add("Authorization", auth);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var data = System.Text.Json.JsonSerializer.Serialize(inDto);
        request.Content = new StringContent(data, Encoding.UTF8, "application/json");

        var httpClient = clientFactory.CreateClient();

        using var response = await httpClient.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }
}
