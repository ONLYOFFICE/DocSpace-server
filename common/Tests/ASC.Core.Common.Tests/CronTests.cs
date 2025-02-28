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
