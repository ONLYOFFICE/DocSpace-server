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

using ASC.Notify.Cron;

namespace ASC.Core.Common.Tests
{
    public class CronTests
    {
        [MemberData(nameof(Data))]
        [Theory]
        public void Get_Time_After(string cronExpression, DateTime afterTime, DateTime expected)
        {
            //arrage
            var sut = new CronExpression(cronExpression);

            //act
            var result = sut.GetTimeAfter(afterTime);

            //Assert
            Assert.Equal(result, expected);
        }

        public static List<object[]> Data()
        {
            return new List<object[]>
            {
                 new object[] { "0 0 12 ? * 1", new DateTime(2025, 1, 1), new DateTime(2025, 1, 5, 12, 0, 0) },
                 new object[] { "0 0 12 ? * 1", new DateTime(2025, 1, 29), new DateTime(2025, 2, 2, 12, 0, 0) },

                 new object[] { "0 0 12 22 * ?", new DateTime(2025, 1, 1), new DateTime(2025, 1, 22, 12, 0, 0) },
                 new object[] { "0 0 12 22 * ?", new DateTime(2025, 1, 29), new DateTime(2025, 2, 22, 12, 0, 0) },

                 new object[] { "0 0 12 ? * *", new DateTime(2025, 1, 1), new DateTime(2025, 1, 1, 12, 0, 0) },
                 new object[] { "0 0 12 ? * *", new DateTime(2025, 1, 5, 9, 0, 0), new DateTime(2025, 1, 5, 12, 0, 0) },
                 new object[] { "0 0 12 ? * *", new DateTime(2025, 1, 1, 13, 0, 0), new DateTime(2025, 1, 2, 12, 0, 0) },
                 new object[] { "0 0 12 ? * *", new DateTime(2025, 1, 31, 13, 0, 0), new DateTime(2025, 2, 1, 12, 0, 0) }
            };
        }
    }
}
