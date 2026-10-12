using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade98 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "AddRelationshipEvent", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "CreateNewContact", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ExportCompleted", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ResponsibleForOpportunity", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ResponsibleForTask", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "SetAccess", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new bookmark created", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "28b10049-dd20-4f54-b986-873bc14ccfc7", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "BirthdayReminder", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "37620ae5-c40b-45ce-855a-39dd7d76a1fa", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "calendar_sharing", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "event_alert", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "invitetoproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "milestonedeadline", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentformessage", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentformilestone", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentfortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "projectcreaterequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "projecteditrequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "removefromproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "responsibleforproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "responsiblefortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "taskclosed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new feed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6504977c-75af-4691-9099-084d3ddeea04", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new post", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6a598c74-91ae-437d-a5f4-ad339bd11bb2", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharefolder", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "updatedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new wiki page", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "742cf945-cbbc-4a57-82d6-1600a12cf8ca", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new topic in forum", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "853b6eb9-73ee-438d-9b09-8ffeedf36234", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new photo uploaded", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "9d51954f-db9b-4aed-94e3-ed70b914e101", -1 },
                column: "sender",
                value: "email.sender");

            // the jabber/messenger channel has no patterns any more: drop it from the tenant and user rows too,
            // otherwise NotifyEngine keeps logging "no one patterns getted" for every notification
            migrationBuilder.Sql("DELETE FROM core_subscriptionmethod WHERE sender = 'messanger.sender'");

            migrationBuilder.Sql(
                """
                UPDATE core_subscriptionmethod
                SET sender = TRIM(BOTH '|' FROM REPLACE(CONCAT('|', sender, '|'), '|messanger.sender|', '|'))
                WHERE sender LIKE '%messanger.sender%'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "AddRelationshipEvent", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "CreateNewContact", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ExportCompleted", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ResponsibleForOpportunity", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "ResponsibleForTask", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "SetAccess", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "13ff36fb-0272-4887-b416-74f52b0d0b02", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new bookmark created", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "28b10049-dd20-4f54-b986-873bc14ccfc7", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "BirthdayReminder", "abef62db-11a8-4673-9d32-ef1d8af19dc0", "37620ae5-c40b-45ce-855a-39dd7d76a1fa", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "calendar_sharing", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "event_alert", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "40650da3-f7c1-424c-8c89-b9c115472e08", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "invitetoproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "milestonedeadline", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentformessage", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentformilestone", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "newcommentfortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "projectcreaterequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "projecteditrequest", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "removefromproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "responsibleforproject", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "responsiblefortask", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "taskclosed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6045b68c-2c2e-42db-9e53-c272e814c4ad", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new feed", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6504977c-75af-4691-9099-084d3ddeea04", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new post", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6a598c74-91ae-437d-a5f4-ad339bd11bb2", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharefolder", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "updatedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new wiki page", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "742cf945-cbbc-4a57-82d6-1600a12cf8ca", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new topic in forum", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "853b6eb9-73ee-438d-9b09-8ffeedf36234", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "new photo uploaded", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "9d51954f-db9b-4aed-94e3-ed70b914e101", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");
        }
    }
}
