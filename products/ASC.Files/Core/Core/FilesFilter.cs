// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Core;

public record FileFilter { }

public record BaseFilter : FileFilter
{
    public int From { get; set; }
    public int Count { get; set; } 
    public OrderBy OrderBy { get; set; }
    public Guid SubjectId { get; set; }
    public FilterType FilterType { get; set; }
    public bool SubjectGroup { get; set; }
    public string SearchText { get; set; }
    public string[] Extension { get; set; }
    public bool SearchInContent { get; set; }
    public bool WithSubfolders { get; set; }
    public SearchArea SearchArea { get; set; }
    public bool WithoutTags { get; set; }
    public IEnumerable<string> TagNames { get; set; }
    public bool ExcludeSubject { get; set; }
    public ProviderFilter Provider { get; set; }
    public SubjectFilter SubjectFilter { get; set; }
    public ApplyFilterOption ApplyFilterOption { get; set; }

    public void Deconstruct(
        out int from, out int count, out OrderBy orderBy, out Guid subjectId, out FilterType filterType, 
        out bool subjectGroup, out string searchText, out string[] extension, out bool searchInContent, out bool withSubfolders,
        out SearchArea searchArea, out bool withoutTags, out IEnumerable<string> tagNames, out bool excludeSubject,
        out ProviderFilter provider, out SubjectFilter subjectFilter, out ApplyFilterOption applyFilterOption
    ) => (from, count, orderBy, subjectId, filterType, subjectGroup, searchText, extension, searchInContent, withSubfolders,
            searchArea, withoutTags, tagNames, excludeSubject, provider, subjectFilter, applyFilterOption) = 
        (From, Count, OrderBy, SubjectId, FilterType, SubjectGroup, SearchText, Extension, SearchInContent, WithSubfolders, 
            SearchArea, WithoutTags, TagNames, ExcludeSubject, Provider, SubjectFilter, ApplyFilterOption);
}

public record FolderFilter : BaseFilter
{
    public IEnumerable<string> SubjectEntriesIds { get; set; }
}

