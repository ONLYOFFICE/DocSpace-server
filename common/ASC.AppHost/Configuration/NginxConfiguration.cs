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

public static class NginxConfiguration
{
    public static IResourceBuilder<ContainerResource> ConfigureOpenResty(
        IDistributedApplicationBuilder builder,
        string basePath,
        string clientBasePath,
        IResourceBuilder<ExecutableResource>? startPackages,
        bool isDocker,
        bool isPreview = false)
    {
        var isArm64 = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;

        // Router telemetry (request logs + server spans) goes to the dashboard's
        // OTLP/HTTP receiver: the Lua exporter in buildtools/config/nginx/lua/otel
        // speaks no gRPC. Profiles that want it set the endpoint below; without it
        // the router runs exactly as it did before, on the plain image.
        var otel = Uri.TryCreate(builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"],
            UriKind.Absolute, out var otlpHttpUri);

        // Tracing needs lua-protobuf, a C module that has to be compiled against
        // the LuaJIT it will run on, so it is built from the -fat flavour of the
        // very same release (it ships luarocks, a compiler and envsubst; the plain
        // image ships none of them).
        var restyTag = Constants.OpenRestyVersion
            + (otel ? "-alpine-fat" : "-alpine")
            + (isArm64 ? "-arm64" : "");

        var certDir = DevCertificateGenerator.EnsureCertificate(basePath);
        var sslConfPath = Path.Combine(builder.AppHostDirectory, "nginx", "docspace-ssl.conf.template");
        var otelConfPath = Path.Combine(builder.AppHostDirectory, "nginx", "otel.conf");
        var otelEnvPath = Path.Combine(builder.AppHostDirectory, "nginx", "otel.main");

        var openResty = builder.AddContainer(Constants.OpenRestyContainer, "openresty/openresty", restyTag)
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx"), "/etc/nginx/conf.d/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx", "includes"), "/etc/nginx/includes/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "install", "docker", "config", "nginx", "templates"), "/etc/nginx/templates/")
            .WithBindMount(Path.Combine(clientBasePath, "public"), "/var/www/public")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "client"), "/var/www/client")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "login"), "/var/www/login")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "management"), "/var/www/management")
            .WithBindMount(certDir, "/etc/nginx/certs/", isReadOnly: true)
            .WithBindMount(sslConfPath, "/etc/nginx/dev-templates/docspace-ssl.conf.template", isReadOnly: true)
            .WithContainerRuntimeArgs(
                "-p", $"0.0.0.0:{Constants.AppHostPort}:{Constants.RestyPort}",
                "-p", $"0.0.0.0:{Constants.AppHostHttpsPort}:{Constants.RestyHttpsPort}");

        if (otel)
        {
            // The lua hooks are only mounted when telemetry is on. The modules
            // behind them bail out on their own when the OTEL_* flags are unset,
            // but not mounting them at all keeps the plain image free of any
            // dependency on the Lua tree.
            openResty
                .WithBindMount(otelConfPath, "/etc/nginx/conf.d/00-otel.conf", isReadOnly: true)
                .WithBindMount(otelEnvPath, "/etc/nginx/conf.d/00-otel.main", isReadOnly: true);
        }

        if (startPackages != null)
        {
            openResty.WaitFor(startPackages);
        }

        var serviceUrls = GetServiceUrls(isDocker, isPreview);

        foreach (var (key, value) in serviceUrls)
        {
            openResty.WithEnvironment(key, value);
        }

        // Variables consumed by the dev SSL vhost template. Keep them separate
        // from serviceUrls so the existing upstream-map envsubst is not affected.
        const string sslEnvVar = "RESTY_HTTP_PORT";
        openResty.WithEnvironment(sslEnvVar, Constants.RestyPort.ToString());

        if (otel)
        {
            // Hands the container the dashboard's OTLP api key as
            // OTEL_EXPORTER_OTLP_HEADERS, which otel/config.lua parses and both
            // exporters send - so the receiver can keep its authentication on.
            // The endpoint this picks is the gRPC one, which the Lua exporter
            // cannot speak; the override below has to stay after this call.
            openResty.WithOtlpExporter();

            openResty.WithEnvironment("OTEL_LOGS_ENABLED", "true");
            openResty.WithEnvironment("OTEL_TRACES_ENABLED", "true");
            openResty.WithEnvironment("OTEL_SERVICE_NAME", Constants.OpenRestyContainer);
            openResty.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT",
                "http://" + Constants.HostDockerInternal + ":" + otlpHttpUri!.Port.ToString());
            // config.lua reads none of this - nginx does not even declare the
            // variable in otel.main, so Lua cannot see it. It is corrected only
            // so it stops contradicting the endpoint next to it, which
            // WithOtlpExporter left pointing at gRPC.
            openResty.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        }

        // None of the three Lua dependencies ships with OpenResty. All three are
        // pinned to the versions the otel-build stage of
        // buildtools/install/docker/build/Dockerfile bakes into the production
        // router image; here they are installed at start (~12 s) so the dev router
        // can stay a stock image.
        //
        // Clearing the flags on failure is what keeps a failed install harmless:
        // with them still set, rewrite.lua requires opentelemetry on every request
        // and the router answers 500 to everything instead of merely losing its
        // telemetry. The exports reach nginx because it is started from this shell.
        var otelInstall = otel
            ? "{ /usr/local/openresty/luajit/bin/luarocks install lua-protobuf 0.3.3 && " +
              "/usr/local/openresty/bin/opm get ledgetech/lua-resty-http=0.16.1 && " +
              "curl -sSL https://github.com/yangxikun/opentelemetry-lua/archive/refs/tags/v0.2.6.tar.gz | tar -xz -C /tmp && " +
              "cp -r /tmp/opentelemetry-lua-0.2.6/lib/opentelemetry /usr/local/openresty/site/lualib/ ; } " +
              "|| { echo 'otel: dependency install failed, router telemetry is off'; " +
              "export OTEL_TRACES_ENABLED=false OTEL_LOGS_ENABLED=false; }; "
            : "";

        openResty.WithArgs("/bin/sh", "-c",
            // the -fat image the otel path runs on already carries envsubst
            (otel ? "" : "apk add --no-cache gettext && ") +
            otelInstall +
            $"envsubst '{string.Join(' ', serviceUrls.Select(r => $"${r.Key}"))}' < /etc/nginx/includes/onlyoffice-upstream-map.conf.template > /etc/nginx/includes/onlyoffice-upstream-map.conf && " +
            $"envsubst '${sslEnvVar}' < /etc/nginx/dev-templates/docspace-ssl.conf.template > /etc/nginx/conf.d/docspace-ssl.conf && " +
            $"/usr/local/openresty/bin/openresty -g 'daemon off;'");

        return openResty;
    }

    private static string BackendUrl<TProject>(int port, bool isDocker, bool isPreview) where TProject : IProjectMetadata, new()
    {
        if (isPreview)
        {
            return isDocker
                ? $"http://{ProjectConfigurator.GetProjectName<ASC_Monolith>()}:{Constants.MonolithPort}"
                : $"http://{Constants.HostDockerInternal}:{Constants.MonolithPort}";
        }

        return isDocker
            ? $"http://{ProjectConfigurator.GetProjectName<TProject>()}:{port}"
            : $"http://{Constants.HostDockerInternal}:{port}";
    }

    private static Dictionary<string, string> GetServiceUrls(bool isDocker, bool isPreview = false)
    {
        return new Dictionary<string, string>
        {
            { "SERVICE_CLIENT", $"http://{Constants.HostDockerInternal}:5001" },
            { "SERVICE_DOCEDITOR", $"http://{Constants.HostDockerInternal}:5013" },
            { "DOCUMENT_SERVER_URL_EXTERNAL", $"http://{Constants.EditorsContainer}" },
            { "DOCUMENT_CONTAINER_NAME", $"http://{Constants.EditorsContainer}" },
            { "SERVICE_MANAGEMENT", $"http://{Constants.HostDockerInternal}:5015" },
            { "SERVICE_PEOPLE_SERVER", BackendUrl<ASC_People>(Constants.PeoplePort, isDocker, isPreview) },
            { "SERVICE_FILES", BackendUrl<ASC_Files>(Constants.FilesPort, isDocker, isPreview) },
            { "SERVICE_API", BackendUrl<ASC_Web_Api>(Constants.WebApiPort, isDocker, isPreview) },
            { "SERVICE_API_SYSTEM", BackendUrl<ASC_ApiSystem>(Constants.ApiSystemPort, isDocker, isPreview) },
            { "SERVICE_BACKUP", BackendUrl<ASC_Data_Backup>(Constants.BackupPort, isDocker, isPreview) },
            { "SERVICE_STUDIO", BackendUrl<ASC_Web_Studio>(Constants.WebstudioPort, isDocker, isPreview) },
            { "SERVICE_AI", BackendUrl<ASC_AI>(Constants.AiPort, isDocker, isPreview) },
            { "SERVICE_API_CACHE", BackendUrl<ASC_Web_Api>(Constants.MonolithPort, isDocker, isPreview) },
            { "SERVICE_HELTHCHECKS", BackendUrl<ASC_Web_Api>(5033, isDocker, isPreview) },
            { "SERVICE_MIGRATION", BackendUrl<ASC_Web_Studio>(5034, isDocker, isPreview) },
            { "SERVICE_PLUGINS", $"http://{Constants.HostDockerInternal}:5014" },
            { "SERVICE_IDENTITY_API", $"http://{Constants.IdentityRegistrationContainer}:{Constants.IdentityRegistrationPort}" },
            { "SERVICE_IDENTITY", $"http://{Constants.IdentityAuthorizationContainer}:{Constants.IdentityAuthorizationPort}" },
            { "SERVICE_SSOAUTH", $"http://{Constants.HostDockerInternal}:9834" },
            { "SERVICE_AI_CHAT", $"http://{Constants.HostDockerInternal}:{Constants.AiChatPort}" },
            { "SERVICE_SOCKET", $"http://{Constants.HostDockerInternal}:9899" },
            { "SERVICE_LOGIN", $"http://{Constants.HostDockerInternal}:5011" },
            { "SERVICE_SDK", $"http://{Constants.HostDockerInternal}:5099" },
            { "DASHBOARDS_CONTAINER_NAME", $"http://{Constants.HostDockerInternal}:5601" },
            { "STORYBOOK_CONTAINER_NAME", $"http://{Constants.HostDockerInternal}:6006" },
            { "DNS_NAMESERVER", "127.0.0.11" }
        };
    }
}
