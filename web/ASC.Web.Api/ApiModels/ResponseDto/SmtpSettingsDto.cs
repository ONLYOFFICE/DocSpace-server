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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The SMTP settings parameters.
/// </summary>
public class SmtpSettingsDto : IMapFrom<SmtpSettings>
{
    /// <summary>
    /// The SMTP host.
    /// </summary>
    [SwaggerSchemaCustom(Example = "mail.example.com")]
    [StringLength(255)]
    public string Host { get; set; }

    /// <summary>
    /// The SMTP port.
    /// </summary>
    [SwaggerSchemaCustom(Example = 25)]
    [Range(1, 65535)]
    public int? Port { get; set; }

    /// <summary>
    /// The sender address.
    /// </summary>
    [SwaggerSchemaCustom(Example = "notify@example.com")]
    [StringLength(255)]
    public string SenderAddress { get; set; }

    /// <summary>
    /// The sender display name.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Postman")]
    [StringLength(255)]
    public string SenderDisplayName { get; set; }

    /// <summary>
    /// The credentials username.
    /// </summary>
    [SwaggerSchemaCustom(Example = "notify@example.com")]
    [StringLength(255)]
    public string CredentialsUserName { get; set; }

    /// <summary>
    /// The credentials user password.
    /// </summary>
    [SwaggerSchemaCustom(Example = "{password}")]
    public string CredentialsUserPassword { get; set; }

    /// <summary>
    /// Specifies whether the SSL is enabled or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool EnableSSL { get; set; }

    /// <summary>
    /// Specifies whether the authentication is enabled or not.
    /// </summary>
    public bool EnableAuth { get; set; }

    /// <summary>
    /// Specifies whether to use NTLM or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool UseNtlm { get; set; }

    /// <summary>
    /// Specifies if the current settings are default or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool IsDefaultSettings { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<SmtpSettings, SmtpSettingsDto>();
    }
}