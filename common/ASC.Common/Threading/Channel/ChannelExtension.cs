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

namespace ASC.Common.Threading;
public static class ChannelExtension
{
    public static IList<ChannelReader<T>> Split<T>(this ChannelReader<T> ch, int n, Func<int, int, T, int> selector = null, CancellationToken cancellationToken = default)
    {
        var outputs = new Channel<T>[n];

        for (var i = 0; i < n; i++)
        {
            outputs[i] = Channel.CreateUnbounded<T>();
        }

        Task.Run(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var index = 0;

                await foreach (var item in ch.ReadAllAsync(cancellationToken))
                {
                    if (selector == null)
                    {
                        index = (index + 1) % n;
                    }
                    else
                    {
                        index = selector(n, index, item);
                    }

                    await outputs[index].Writer.WriteAsync(item, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: catch error via additional channel
                //  var error = Channel.CreateUnbounded<T>();
                //  await error.Writer.WriteAsync(ex);
            }
            finally
            {
                foreach (var output in outputs)
                {
                    output.Writer.Complete();
                }
            }
        }, cancellationToken);

        return outputs.Select(output => output.Reader).ToArray();
    }

    public static ChannelReader<T> Merge<T>(this IEnumerable<ChannelReader<T>> inputs, CancellationToken cancellationToken = default)
    {
        var output = Channel.CreateUnbounded<T>();

        Task.Run(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                async Task Redirect(ChannelReader<T> input)
                {
                    await foreach (var item in input.ReadAllAsync(cancellationToken))
                    {
                        await output.Writer.WriteAsync(item, cancellationToken);
                    }
                }

                await Task.WhenAll(inputs.Select(Redirect).ToArray());
            }
            catch (OperationCanceledException)
            {
                // TODO: catch error via additional channel
                //  var error = Channel.CreateUnbounded<T>();
                //  await error.Writer.WriteAsync(ex);
            }
            finally
            {
                output.Writer.Complete();
            }
        }, cancellationToken);

        return output;
    }

}