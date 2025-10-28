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

#nullable enable

using Attachment = OpenSearch.Client.Attachment;

namespace ASC.Files.Core.Text;

[Singleton(typeof(ITextExtractor))]
public class OpenSearchTextExtractor(Client client) : ITextExtractor
{
    private const string PipelineId = "attachments";
    
    public async Task<string?> ExtractAsync(Memory<byte> content)
    {
        var document = new SimulatePipelineDocument
        {
            Index = "extract", //not used, needed to avoid error
            Source = new Source
            {
                Document = new Document { Data = Convert.ToBase64String(content.Span) }
            }
        };
        
        var response = await client.Instance.Ingest
            .SimulatePipelineAsync(s => 
                s.Id(PipelineId)
                    .Documents([document]));

        if (!response.IsValid)
        {
            return null;
        }
        
        var simulation = response.Documents.FirstOrDefault();
        if (simulation == null)
        {
            return null;
        }

        var source = await simulation.Document.Source.AsAsync<Source>();
        return source.Document?.Attachment?.Content;
    }

    private class Source
    {
        public Document? Document { get; set; }
    }

    private class Document
    {
        public string? Data { get; set; }
        public Attachment? Attachment { get; set; }
    }
}