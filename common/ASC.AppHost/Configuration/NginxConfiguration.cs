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

        var certDir = DevCertificateGenerator.EnsureCertificate(basePath);
        var sslConfPath = Path.Combine(builder.AppHostDirectory, "nginx", "docspace-ssl.conf.template");

        var openResty = builder.AddContainer(Constants.OpenRestyContainer, "openresty/openresty", "1.27.1.2-10-alpine" + (isArm64 ? "-arm64" : ""))
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

        openResty.WithArgs("/bin/sh", "-c",
            $"apk add --no-cache gettext && " +
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
            { "SERVICE_NEW_AI", $"http://{Constants.HostDockerInternal}:{Constants.NewAiPort}" },
            { "SERVICE_SOCKET", $"http://{Constants.HostDockerInternal}:9899" },
            { "SERVICE_LOGIN", $"http://{Constants.HostDockerInternal}:5011" },
            { "SERVICE_SDK", $"http://{Constants.HostDockerInternal}:5099" },
            { "DASHBOARDS_CONTAINER_NAME", $"http://{Constants.HostDockerInternal}:5601" },
            { "DNS_NAMESERVER", "127.0.0.11" }
        };
    }
}
