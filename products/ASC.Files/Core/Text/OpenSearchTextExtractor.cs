// (c) Copyright Ascensio System SIA 2009-2026
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
using HttpMethod = OpenSearch.Net.HttpMethod;
using PostData = OpenSearch.Net.PostData;

namespace ASC.Files.Core.Text;

[Singleton(typeof(ITextExtractor))]
public class OpenSearchTextExtractor(Client client) : ITextExtractor
{
    private const string PipelineId = "attachments";

    private static readonly byte[] _jsonPrefix =
        "{\"docs\":[{\"_index\":\"extract\",\"_source\":{\"document\":{\"data\":\""u8.ToArray();

    private static readonly byte[] _jsonSuffix =
        "\"}}}]}"u8.ToArray();

    public async Task<string?> ExtractAsync(Stream content, long contentLength)
    {
        var postData = PostData.StreamHandler(
            content,
            static (_, _) => throw new NotSupportedException(),
            static async (source, stream, ct) =>
            {
                await stream.WriteAsync(_jsonPrefix, ct);
                await WriteToBase64StreamAsync(source, stream, ct);
                await stream.WriteAsync(_jsonSuffix, ct);
            });

        var response = await ((IOpenSearchClient)client.Instance).LowLevel
            .DoRequestAsync<SimulatePipelineResponse>(
                HttpMethod.POST,
                $"/_ingest/pipeline/{Uri.EscapeDataString(PipelineId)}/_simulate",
                CancellationToken.None,
                postData);

        if (!response.IsValid)
        {
            return null;
        }

        var simulation = response.Documents.FirstOrDefault();
        if (simulation?.Document == null)
        {
            return null;
        }

        var documentSource = await simulation.Document.Source.AsAsync<Source>();
        return documentSource.Document?.Attachment?.Content;
    }

    public class Source
    {
        public Document? Document { get; set; }
    }

    public class Document
    {
        public Attachment? Attachment { get; set; }
    }

    private static async Task WriteToBase64StreamAsync(
        Stream source, Stream destination, CancellationToken ct = default)
    {
        const int bufferSize = 3 * 1024;

        var input = System.Buffers.ArrayPool<byte>.Shared.Rent(bufferSize);
        var output = System.Buffers.ArrayPool<byte>.Shared.Rent(
            System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length(bufferSize));

        var leftover = 0;

        try
        {
            while (true)
            {
                var read = await source.ReadAsync(input.AsMemory(leftover, bufferSize - leftover), ct);

                var count = leftover + read;
                if (count == 0)
                {
                    break;
                }

                var final = read == 0;

                var status = System.Buffers.Text.Base64.EncodeToUtf8(
                    input.AsSpan(0, count),
                    output,
                    out var consumed,
                    out var written,
                    isFinalBlock: final);

                if (status != System.Buffers.OperationStatus.Done
                    && !(status == System.Buffers.OperationStatus.NeedMoreData && !final))
                {
                    throw new InvalidOperationException($"Base64 encoding failed: {status}");
                }

                await destination.WriteAsync(output.AsMemory(0, written), ct);

                if (final)
                {
                    break;
                }

                leftover = count - consumed;

                if (leftover > 0)
                {
                    input.AsSpan(consumed, leftover).CopyTo(input);
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(input);
            System.Buffers.ArrayPool<byte>.Shared.Return(output);
        }
    }
}
