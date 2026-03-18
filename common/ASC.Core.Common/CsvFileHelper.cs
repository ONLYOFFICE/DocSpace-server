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

using CsvHelper;
using CsvHelper.Configuration;

using DateTimeConverter = CsvHelper.TypeConversion.DateTimeConverter;

namespace ASC.Core.Common;

[Scope]
public class CsvFileHelper(ILogger<CsvFileHelper> logger)
{
    public Stream CreateFile<T>(IEnumerable<T> rows, ClassMap<T> mapper)
    {
        try
        {
            var stream = new MemoryStream();
            // CA2000: StreamWriter and CsvWriter ownership transferred to caller via returned stream
            // Caller is responsible for disposing the stream, which will dispose the writer and csv
#pragma warning disable CA2000
            var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);
            var csv = new CsvWriter(writer, CultureInfo.CurrentCulture);
#pragma warning restore CA2000

            if (mapper != null)
            {
                csv.Context.RegisterClassMap(mapper);
            }

            csv.WriteHeader<T>();
            csv.NextRecord();
            csv.WriteRecords(rows);
            writer.Flush();

            return stream;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileCreating(ex);
            throw;
        }
    }

    public async Task CreateLargeFileAsync<T>(
        Stream tempStream,
        IAsyncEnumerable<IEnumerable<T>> partialRecords,
        ClassMap<T> mapper,
        CsvConfiguration config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            config ??= new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true
            };

            // CA2000: StreamWriter and CsvWriter write to caller-provided tempStream
            // The tempStream is owned by caller and will be disposed by them
#pragma warning disable CA2000
            var writer = new StreamWriter(tempStream, leaveOpen: true);
            var csv = new CsvWriter(writer, config);
#pragma warning restore CA2000

            if (mapper != null)
            {
                csv.Context.RegisterClassMap(mapper);
            }

            csv.WriteHeader<T>();

            await csv.NextRecordAsync();

            if (partialRecords != null)
            {
                await foreach (var records in partialRecords.WithCancellation(cancellationToken))
                {
                    await csv.WriteRecordsAsync(records, cancellationToken);
                }
            }

            writer.Flush();
        }
        catch (Exception ex)
        {
            logger.ErrorWhileCreating(ex);
            throw;
        }
    }

    public class CsvDateTimeConverter : DateTimeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is not DateTime dateTime)
            {
                return base.ConvertToString(value, row, memberMapData);
            }

            var culture = memberMapData.TypeConverterOptions.CultureInfo;
            var format = $"{culture?.DateTimeFormat.ShortDatePattern} {culture?.DateTimeFormat.ShortTimePattern}";

            return $"=\"{dateTime.ToString(format)}\"";
        }
    }
}