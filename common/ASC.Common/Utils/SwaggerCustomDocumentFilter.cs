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
using Amazon.Runtime.Internal.Transform;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class HideRouteDocumentFilter : IDocumentFilter
{
    private readonly string _routeToHide;

    public HideRouteDocumentFilter(string RouteToHide)
    {
        _routeToHide = RouteToHide;
    }

    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        if (document.Paths.ContainsKey(_routeToHide))
        {
            document.Paths.Remove(_routeToHide);
        }
    }
}

public class TagDescriptionsDocumentFilter : IDocumentFilter
{
    private readonly Dictionary<string, string> _tagDescriptions = new Dictionary<string, string>
    {
        { "Backup", "" },
        { "Files / Files", "" },
        { "Files / Folders", "" },
        { "Files / Operations", "" },
        { "Files / Quota", "" },
        { "Files / Rooms", "" },
        { "Files / Settings", "" },
        { "Files / Third-party integration", "" },
        { "Group", "" },
        { "Group / Rooms", "" },
        { "People / Contacts", "" },
        { "People / Password", "" },
        { "People / Photos", "" },
        { "People / Profiles", "" },
        { "People / Quota", "" },
        { "People / Search", "" },
        { "People / Theme", "" },
        { "People / Third-party accounts", "" },
        { "People / User data", "" },
        { "People / User status", "" },
        { "People / User type", "" },
        { "Authentication", "" },
        { "Capabilities", "" },
        { "Migration", "" },
        { "Modules", "" },
        { "Portal / Quota", "" },
        { "Portal / Settings", "" },
        { "Portal / Users", "" },
        { "Security / Active connections", "" },
        { "Security / Audit trail data", "" },
        { "Security / CSP", "" },
        { "Security / Firebase", "" },
        { "Security / Login history", "" },
        { "Security / SMTP settings", "" },
        { "Settings / Authorization", "" },
        { "Settings / Common settings", "" },
        { "Settings / Cookies", "" },
        { "Settings / Custom Navigation", "" },
        { "Settings / Encryption", "" },
        { "Settings / Greeting settings", "" },
        { "Settings / IP restrictions", "" },
        { "Settings / License", "" },
        { "Settings / Login settings", "" },
        { "Settings / Messages", "" },
        { "Settings / Notifications", "" },
        { "Settings / Owner", "" },
        { "Settings / Quota", "" },
        { "Settings / Rebranding", "" },
        { "Settings / Security", "" },
        { "Settings / SSO", "" },
        { "Settings / Statistics", "" },
        { "Settings / Storage", "" },
        { "Settings / Team templates", "" },
        { "Settings / TFA settings", "" },
        { "Settings / Tips", "" },
        { "Settings / Webhooks", "" },
        { "Settings / Webplugins", "" }
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var customTags = new HashSet<string>();

        foreach (var path in swaggerDoc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var tag in operation.Value.Tags)
                {
                    customTags.Add(tag.Name);
                }
            }
        }

        swaggerDoc.Tags = customTags.Select(tag => new OpenApiTag
        {
            Name = tag,
            Description = _tagDescriptions[tag]
        }).ToList();
        
    }
}