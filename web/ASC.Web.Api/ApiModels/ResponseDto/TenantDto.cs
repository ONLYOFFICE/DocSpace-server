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

public class TenantDto : IMapFrom<Tenant>
{
    /// <summary>
    /// Affiliate ID
    /// </summary>
    [OpenApiDescription("Affiliate ID")]
    public string AffiliateId { get; set; }

    /// <summary>
    /// Tenant alias
    /// </summary>
    [OpenApiDescription("Tenant alias")]
    public string TenantAlias { get; set; }

    /// <summary>
    /// Specifies if the calls are available for this tenant or not
    /// </summary>
    [OpenApiDescription("Specifies if the calls are available for this tenant or not")]
    public bool Calls { get; set; }

    /// <summary>
    /// Campaign
    /// </summary>
    [OpenApiDescription("Campaign")]
    public string Campaign { get; set; }

    /// <summary>
    /// Creation date and time
    /// </summary>
    [OpenApiDescription("Creation date and time")]
    public DateTime CreationDateTime { get; internal set; }

    /// <summary>
    /// Hosted region
    /// </summary>
    [OpenApiDescription("Hosted region")]
    public string HostedRegion { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    [OpenApiDescription("Tenant ID")]
    public int TenantId { get; internal set; }

    /// <summary>
    /// Tenant industry
    /// </summary>
    [OpenApiDescription("Tenant industry")]
    public TenantIndustry Industry { get; set; }

    /// <summary>
    /// Language
    /// </summary>
    [OpenApiDescription("Language")]
    public string Language { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    [OpenApiDescription("Last modified date")]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Mapped domain
    /// </summary>
    [OpenApiDescription("Mapped domain")]
    public string MappedDomain { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [OpenApiDescription("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    [OpenApiDescription("Owner ID")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Payment ID
    /// </summary>
    [OpenApiDescription("Payment ID")]
    public string PaymentId { get; set; }

    /// <summary>
    /// Specifies if the ONLYOFFICE newsletter is allowed or not
    /// </summary>
    [OpenApiDescription("Specifies if the ONLYOFFICE newsletter is allowed or not")]
    public bool Spam { get; set; }

    /// <summary>
    /// Tenant status
    /// </summary>
    [OpenApiDescription("Tenant status")]
    public TenantStatus Status { get; internal set; }

    /// <summary>
    /// The date and time when the tenant status was changed
    /// </summary>
    [OpenApiDescription("The date and time when the tenant status was changed")]
    public DateTime StatusChangeDate { get; internal set; }

    /// <summary>
    /// Time zone
    /// </summary>
    [OpenApiDescription("Time zone")]
    public string TimeZone { get; set; }

    /// <summary>
    /// List of trusted domains
    /// </summary>
    [OpenApiDescription("List of trusted domains")]
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// Trusted domains in the string format
    /// </summary>
    [OpenApiDescription("Trusted domains in the string format")]
    public string TrustedDomainsRaw { get; set; }

    /// <summary>
    /// Trusted domains type
    /// </summary>
    [OpenApiDescription("Trusted domains type")]
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    [OpenApiDescription("Version")]
    public int Version { get; set; }

    /// <summary>
    /// The date and time when the tenant version was changed
    /// </summary>
    [OpenApiDescription("The date and time when the tenant version was changed")]
    public DateTime VersionChanged { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Tenant, TenantDto>()
            .ForMember(r => r.TenantId, opt => opt.MapFrom(src => src.Id))
            .ForMember(r => r.TenantAlias, opt => opt.MapFrom(src => src.Alias));
    }
}
