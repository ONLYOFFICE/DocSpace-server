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
