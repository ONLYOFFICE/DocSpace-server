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

namespace ASC.Web.Api.ApiModel.ResponseDto;

public class SmtpSettingsDto : IMapFrom<SmtpSettings>
{
    [SwaggerSchemaCustom(Example = "mail.example.com", Description = "Host")]
    public string Host { get; set; }

    [SwaggerSchemaCustom(Example = "25", Description = "Port", Format = "int32", Nullable = true)]
    public int? Port { get; set; }

    [SwaggerSchemaCustom(Example = "notify@example.com", Description = "Sender address")]
    public string SenderAddress { get; set; }

    [SwaggerSchemaCustom(Example = "Postman", Description = "Sender display name")]
    public string SenderDisplayName { get; set; }

    [SwaggerSchemaCustom(Example = "notify@example.com", Description = "Credentials username")]
    public string CredentialsUserName { get; set; }

    [SwaggerSchemaCustom(Example = "{password}", Description = "Credentials user password")]
    public string CredentialsUserPassword { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Enables SSL or not")]
    public bool EnableSSL { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Enables authentication or not")]
    public bool EnableAuth { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Specifies whether to use NTLM or not")]
    public bool UseNtlm { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Specifies if the current settings are default or not")]
    public bool IsDefaultSettings { get; set; }

    public static SmtpSettingsDto GetSample()
    {
        return new SmtpSettingsDto
        {
            Host = "mail.example.com",
            Port = 25,
            CredentialsUserName = "notify@example.com",
            CredentialsUserPassword = "{password}",
            EnableAuth = true,
            EnableSSL = false,
            SenderAddress = "notify@example.com",
            SenderDisplayName = "Postman"
        };
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<SmtpSettings, SmtpSettingsDto>();
    }
}