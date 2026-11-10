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

namespace ASC.AppHost.Configuration;

public static class Constants
{
    public const int AppHostPort = 8092;
    public const int AppHostHttpsPort = 443;
    public const string AppHostHttpsHost = "docspace.dev.localhost";

    public const int RestyPort = 8092;
    public const int RestyHttpsPort = 443;
    public const int SocketIoPort = 9899;
    public const int SsoAuthPort = 9834;
    public const int AiChatPort = 9837;
    public const int DocumentAgentPort = 9838;
    /// <summary>The port the agent image listens on inside its container (its Dockerfile's default).</summary>
    public const int AgentContainerPort = 8081;
    public const int WebDavPort = 1900;
    public const int IdentityRegistrationPort = 9090;
    public const int IdentityAuthorizationPort = 8080;
    public const int PeoplePort = 5004;
    public const int FilesPort = 5007;
    public const int WebApiPort = 5000;
    public const int ApiSystemPort = 5010;
    public const int BackupPort = 5012;
    public const int WebstudioPort = 5003;
    public const int AiPort = 5157;
    public const int AiWorkerPort = 5154;
    public const int ClearEventsPort = 5027;
    public const int BackupWorkerPort = 5032;
    public const int FilesWorkerPort = 5009;
    public const int NotifyPort = 5005;
    public const int StudioNotifyPort = 5006;
    public const int OpensearchPort = 9200;
    public const int DocSpaceMcpPort = 8000;
    public const int PlaywrightUiPort = 3333;
    public const int E2ETestsUiPort = 3334;

    public const int TelegramPort = 5050;
    public const int MonolithPort = 5027;

    // Keep in sync with OPENRESTY_VERSION in
    // buildtools/install/docker/build/Dockerfile, so the dev router runs the
    // release the production router image is built from.
    public const string OpenRestyVersion = "1.31.1.1-2";

    public const int OtelCollectorGrpcPort = 4317;
    public const int OtelCollectorHttpPort = 4318;

    public const string HostDockerInternal = "host.docker.internal";
    public const string OpenRestyContainer = "onlyoffice-openresty";
    public const string EditorsContainer = "onlyoffice-editors";
    public const string OpensearchContainer = "opensearch";
    public const string OpensearchVersion = "3.5.0";

    // The built-in forms database (ConnectionStrings:formsAdmin): PostgreSQL with pg_cron, set up as a server shared
    // by portals of different owners needs it. The port keeps clear of a local PostgreSQL on 5432.
    public const string FormsPostgresContainer = "forms-postgres";
    public const string PostgresVersion = "17";
    public const int FormsPostgresPort = 35432;
    public const string FormsDatabase = "forms";
    public const string FormsAdminUser = "forms_admin";
    public const string SocketIoContainer = "onlyoffice-socketIO";
    public const string DocSpaceMcpContainer = "onlyoffice-docspace-mcp";
    public const string DocumentAgentContainer = "onlyoffice-document-agent";
    public const string OtelCollectorContainer = "onlyoffice-otel-collector";

    public const string IdentityRegistrationContainer = "onlyoffice-identity-registration";
    public const string IdentityAuthorizationContainer = "onlyoffice-identity-authorization";

    // The Nextcloud the integration tests connect as third-party storage. The account is created by
    // the image on first start (SQLite, no volume), so every run begins with an empty one.
    public const string NextcloudContainer = "nextcloud";
    public const string NextcloudVersion = "33-apache";
    public const string NextcloudUser = "docspace";
    public const string NextcloudPassword = "docspace-tests";

    // Where the webhook tests run the WireMock they subscribe to, in the test process: on a loopback alias the
    // integration-test blacklist leaves open (ConnectionStringManager.AddBaseConfig) while 127.0.0.1 stays blocked.
    public const int WebhookReceiverPort = 18089;
    public const string WebhookReceiverHost = "127.0.0.2";

    // Where the integration-test-billing profile points the billing, accounting and docscloud services
    // (ConnectionStringManager.UsePaymentServices): one WireMock the billing tests run in the test process, started
    // before the stack, since the services ask billing as soon as a portal is registered. Each path is the base URL
    // that service's client joins its routes to, so the three stay apart on the one server.
    public const string PaymentStubHost = "127.0.0.1";
    public const int PaymentStubPort = 18090;
    public const string PaymentStubBillingPath = "/billing/api";
    public const string PaymentStubAccountingPath = "/accounting/api";
    public const string PaymentStubDocsCloudPath = "/docscloud/api/";

    // What the services of that profile are told about the payment services in place of what buildtools/config says:
    // the addresses, key, secret, region, default quota and the first accounting currency. A portal's payment key is
    // the region followed by the tenant id, which is how the stub tells the portals apart; a portal billing knows
    // nothing about falls back to the free startup quota rather than becoming unpaid. The other payment settings
    // (core:payment:test and :delay, core:accounting:topupperiod, the limits) still come from buildtools/config.
    public const string PaymentStubRegion = "stub-";
    public const string PaymentStubKey = "stub-key";
    public const string PaymentStubSecret = "stub-secret";
    public const int PaymentStubDefaultQuota = -3;

    // The delivery schedule of the integration-test profile (webhooks:retryIntervals and webhooks:requestTimeout,
    // set in ConnectionStringManager.AddBaseConfig): WebhookTestRetryCount retries WebhookTestRetryInterval apart,
    // four attempts in all, instead of the production schedule of minutes to hours, and an attempt that gives up on a
    // silent target after WebhookTestRequestTimeout instead of 30 seconds. The delivery tests in ASC.Web.Api.Tests
    // (WebhookDeliveryTestBase) read the same values.
    public const int WebhookTestRetryCount = 3;
    public static TimeSpan WebhookTestRetryInterval { get; } = TimeSpan.FromMilliseconds(500);
    public static TimeSpan WebhookTestRequestTimeout { get; } = TimeSpan.FromSeconds(12);

    // UrlValidator.DefaultBlacklist with 127.0.0.0/8 narrowed to 127.0.0.1/32: the receiver is reached as
    // WebhookReceiverHost, while localhost stays refused (bug 80980). A non-empty list replaces the default as a
    // whole, so a test in ASC.Web.Api.Tests (WebhookTestProfileTests) keeps the two in step.
    public static IReadOnlyList<string> WebhookTestBlacklist { get; } =
    [
        "0.0.0.0/8", "10.0.0.0/8", "100.64.0.0/10", "127.0.0.1/32", "169.254.0.0/16", "172.16.0.0/12",
        "192.168.0.0/16", "::/128", "::1/128", "fc00::/8", "fd00::/8", "fe80::/10"
    ];

    // The services that get WebhookTestBlacklist: Web.Api checks the target of a subscription when it is saved, and
    // Web.Studio sends the deliveries. Every other UrlValidator caller of the profile keeps the default list, so the
    // SSRF tests of the other suites run against what production refuses.
    public static IReadOnlyList<string> WebhookTestBlacklistServices { get; } =
        [ProjectConfigurator.GetProjectName<ASC_Web_Api>(), ProjectConfigurator.GetProjectName<ASC_Web_Studio>()];
}
