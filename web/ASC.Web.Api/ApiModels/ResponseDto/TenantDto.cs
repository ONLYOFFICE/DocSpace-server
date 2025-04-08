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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The tenant parameters.
/// </summary>
public class TenantDto : IMapFrom<Tenant>
{
    /// <summary>
    /// The affiliate ID.
    /// </summary>
    public string AffiliateId { get; set; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Specifies if the calls are available for this tenant or not.
    /// </summary>
    public bool Calls { get; set; }

    /// <summary>
    /// The tenant campaign.
    /// </summary>
    public string Campaign { get; set; }

    /// <summary>
    /// The tenant creation date and time.
    /// </summary>
    public DateTime CreationDateTime { get; internal set; }

    /// <summary>
    /// The hosted region.
    /// </summary>
    public string HostedRegion { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; internal set; }

    /// <summary>
    /// The tenant industry.
    /// </summary>
    public TenantIndustry Industry { get; set; }

    /// <summary>
    /// The tenant language.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// The date and time when the tenant was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// The tenant mapped domain.
    /// </summary>
    public string MappedDomain { get; set; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The tenant owner ID.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The tenant payment ID.
    /// </summary>
    public string PaymentId { get; set; }

    /// <summary>
    /// Specifies if the ONLYOFFICE newsletter is allowed or not.
    /// </summary>
    public bool Spam { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    public TenantStatus Status { get; internal set; }

    /// <summary>
    /// The date and time when the tenant status was changed.
    /// </summary>
    public DateTime StatusChangeDate { get; internal set; }

    /// <summary>
    /// The tenant time zone.
    /// </summary>
    public string TimeZone { get; set; }

    /// <summary>
    /// The list of tenant trusted domains.
    /// </summary>
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// The tenant trusted domains in the string format.
    /// </summary>
    public string TrustedDomainsRaw { get; set; }

    /// <summary>
    /// The type of the tenant trusted domains.
    /// </summary>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The tenant version 
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The date and time when the tenant version was changed.
    /// </summary>
    public DateTime VersionChanged { get; set; }

    /// <summary>
    /// The tenant AWS region.
    /// </summary>
    public string Region { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Tenant, TenantDto>()
            .ForMember(r => r.TenantId, opt => opt.MapFrom(src => src.Id))
            .ForMember(r => r.TenantAlias, opt => opt.MapFrom(src => src.Alias));
    }
}
