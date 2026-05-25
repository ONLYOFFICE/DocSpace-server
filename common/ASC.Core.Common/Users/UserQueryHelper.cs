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

namespace ASC.Core.Common.Users;

public static class UserQueryHelper
{
    public static T FilterByInviter<T>(T query, bool? invitedByMe, Guid? inviterId, Guid currentId) where T : IQueryable<User>, IEnumerable<User>
    {
        if (invitedByMe.HasValue && invitedByMe.Value)
        {
            return (T)query.Where(u => u.CreatedBy == currentId);
        }

        if (inviterId.HasValue)
        {
            return (T)query.Where(u => u.CreatedBy == inviterId);
        }

        return query;
    }

    public static T FilterByText<T>(T query, string text, string separator) where T : IQueryable<User>, IEnumerable<User>
    {
        if (string.IsNullOrEmpty(text))
        {
            return query;
        }

        var processedText = text.ToLower().Trim();

        if (string.IsNullOrEmpty(separator))
        {
            var split = processedText.Split(" ");
            return split.Aggregate(query, (current, t) => (T)current.Where(u =>
                u.FirstName.ToLower().Contains(t) ||
                u.LastName.ToLower().Contains(t) ||
                u.Email.ToLower().Contains(t)));
        }
        else
        {
            var split = processedText.Split(separator);
            var expression = split
                .Select(x =>
                    (Expression<Func<User, bool>>)(u =>
                        u.FirstName.ToLower().Contains(x) ||
                        u.LastName.ToLower().Contains(x) ||
                        u.Email.ToLower().Contains(x)))
                .Aggregate<Expression<Func<User, bool>>, Expression<Func<User, bool>>>(null, (current, combinedPartLambda) =>
                    current == null
                        ? combinedPartLambda
                        : current.Or(combinedPartLambda));

            if (expression != null)
            {
                return (T)query.Where(expression);
            }
        }

        return query;
    }
}