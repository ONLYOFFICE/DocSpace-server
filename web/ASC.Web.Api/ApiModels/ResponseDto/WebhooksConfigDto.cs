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

namespace ASC.Web.Api.ApiModels.ResponseDto;

public class WebhooksConfigDto : IMapFrom<WebhooksConfig>
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// URI
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Secret key
    /// </summary>
    public string SecretKey { get; set; }

    /// <summary>
    /// Specifies if the webhooks are enabled or not
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// SSL
    /// </summary>
    public bool SSL { get; set; }
}

public class WebhooksConfigWithStatusDto : IMapFrom<WebhooksConfigWithStatus>
{
    /// <summary>
    /// Configs
    /// </summary>
    public WebhooksConfigDto Configs { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WebhooksConfigWithStatus, WebhooksConfigWithStatusDto>()
            .ForMember(src => src.Configs, ex => ex
                .MapFrom(map => map.WebhooksConfig));
    }
}
