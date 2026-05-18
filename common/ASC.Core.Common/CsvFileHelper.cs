// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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

            await writer.FlushAsync(cancellationToken);
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
