// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Core;

public enum RecaptchaType
{
    [SwaggerEnum("Default")]
    Default = 0,

    [SwaggerEnum("AndroidV2")]
    AndroidV2 = 1,

    [SwaggerEnum("iOSV2")]
    iOSV2 = 2,

    [SwaggerEnum("hCaptcha")]
    hCaptcha = 3
}

public class RecaptchaException : InvalidCredentialException
{
    public RecaptchaException()
    {
    }

    public RecaptchaException(string message)
        : base(message)
    {
    }
}

[Scope]
public class Recaptcha(SetupInfo setupInfo, IHttpClientFactory clientFactory)
{
    public async Task<bool> ValidateRecaptchaAsync(RecaptchaType recaptchaType, string response, string ip)
    {
        try
        {
            var privateKey = recaptchaType is RecaptchaType.hCaptcha ? setupInfo.HcaptchaPrivateKey : setupInfo.RecaptchaPrivateKey;
            var verifyUrl = recaptchaType is RecaptchaType.hCaptcha ? setupInfo.HcaptchaVerifyUrl : setupInfo.RecaptchaVerifyUrl;

            var data = $"secret={privateKey}&remoteip={ip}&response={response}";

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(verifyUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var httpClient = clientFactory.CreateClient();
            using var httpClientResponse = await httpClient.SendAsync(request);
            var resp = await httpClientResponse.Content.ReadAsStringAsync();
            var recaptchData = JsonSerializer.Deserialize<RecaptchData>(resp, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (recaptchData.Success.GetValueOrDefault())
            {
                return true;
            }
            
            if (recaptchData.ErrorCodes is { Count: > 0 })
            {
                return false;
            }
        }
        catch (Exception)
        {
        }

        return false;
    }
}

public class RecaptchData
{
    public bool? Success { get; set; }
    
    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; set; }
}
