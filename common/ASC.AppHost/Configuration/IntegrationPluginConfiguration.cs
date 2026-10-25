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

/// <summary>
/// Starts the server-side integration plugins found in <c>{basePath}/plugins</c>: services that talk to DocSpace
/// only through its public surface - the REST API, webhooks and <c>POST api/2.0/portal/moduleEvents</c>.
/// The same folder also holds client-side DocSpace plugins, so a folder is started only when it says it is
/// an integration: a .NET folder by a <c>*.csproj</c>, a Node folder by <c>"docspaceIntegration": true</c>
/// in its <c>package.json</c>. Everything else is skipped.
/// A plugin may bring its own engines as <c>engines/{engine}/Dockerfile</c>: each is built and started as a
/// container listening on port 9000 with a persistent volume at <c>/data</c>, and the plugin gets its address as
/// <c>Engines__{engine}__Url</c>.
/// </summary>
public static partial class IntegrationPluginConfiguration
{
    private const string PackageJsonMarker = "docspaceIntegration";
    private const string ModulesSection = "plugins:socketEvents:modules";
    private const int EnginePort = 9000;

    public static void AddIntegrationPlugins(IDistributedApplicationBuilder builder, string basePath)
    {
        var pluginsBasePath = Path.Combine(basePath, "plugins");

        if (!Directory.Exists(pluginsBasePath))
        {
            return;
        }

        var modules = new Dictionary<string, string>();

        foreach (var pluginDirectory in Directory.GetDirectories(pluginsBasePath))
        {
            var module = ResourceName(Path.GetFileName(pluginDirectory));
            if (module.Length == 0 || modules.ContainsKey(module))
            {
                Console.WriteLine($"Integration plugin '{pluginDirectory}' skipped: its folder name gives no unique resource name");
                continue;
            }

            var resource = AddPlugin(builder, pluginDirectory, $"plugin-{module}");
            if (resource == null)
            {
                continue;
            }

            // A fresh key on every AppHost start: both sides get it from here, so nothing has to be stored.
            var moduleKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            modules[module] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(moduleKey)));

            resource
                .WithEnvironment("DocSpace__Url", $"http://localhost:{Constants.AppHostPort.ToString()}")
                .WithEnvironment("DocSpace__Module", module)
                .WithEnvironment("DocSpace__ModuleKey", moduleKey);

            AddEngines(builder, pluginDirectory, module, resource);
        }

        if (modules.Count > 0)
        {
            ConfigureDocSpace(builder, modules);
        }
    }

    private static IResourceBuilder<IResourceWithEnvironment>? AddPlugin(IDistributedApplicationBuilder builder, string pluginDirectory, string name)
    {
        var project = Directory.GetFiles(pluginDirectory, "*.csproj");
        if (project.Length > 1)
        {
            Console.WriteLine($"Integration plugin '{pluginDirectory}' skipped: it holds {project.Length} projects, expected one");
            return null;
        }

        if (project.Length == 1)
        {
            // a project resource builds the plugin first and wires OpenTelemetry, so a fresh checkout needs no build step
            var dotnetPlugin = builder.AddProject(name, project[0]);

            // a worker-style plugin has no HTTP endpoint in its launch settings, and then nothing to probe
            if (dotnetPlugin.Resource.Annotations.OfType<EndpointAnnotation>().Any(e => e.Name == "http"))
            {
                dotnetPlugin.WithHttpHealthCheck("/health");
            }

            return builder.CreateResourceBuilder<IResourceWithEnvironment>(dotnetPlugin.Resource);
        }

        if (IsNodeIntegration(pluginDirectory))
        {
            var nodePlugin = builder.AddJavaScriptApp(name, pluginDirectory, "start").WithNpm();
            return builder.CreateResourceBuilder<IResourceWithEnvironment>(nodePlugin.Resource);
        }

        return null;
    }

    private static void AddEngines(IDistributedApplicationBuilder builder, string pluginDirectory, string module, IResourceBuilder<IResourceWithEnvironment> plugin)
    {
        var enginesPath = Path.Combine(pluginDirectory, "engines");
        if (!Directory.Exists(enginesPath))
        {
            return;
        }

        foreach (var engineDirectory in Directory.GetDirectories(enginesPath))
        {
            if (!File.Exists(Path.Combine(engineDirectory, "Dockerfile")))
            {
                continue;
            }

            var engine = ResourceName(Path.GetFileName(engineDirectory));
            // /data outlives the container, so model downloads and caches survive an AppHost restart
            var container = builder.AddDockerfile($"plugin-{module}-{engine}", engineDirectory)
                .WithHttpEndpoint(targetPort: EnginePort, name: "http")
                .WithVolume($"docspace-plugin-{module}-{engine}", "/data");

            plugin.WithEnvironment($"Engines__{engine}__Url", container.GetEndpoint("http"));
        }
    }

    /// <summary>
    /// Tells Web.Api (or the monolith) the hash of each plugin's module key, so the plugin may push events to every
    /// portal. A local plugin also listens on loopback, which the webhook URL check refuses as an SSRF target:
    /// for these services only, the two loopback entries of the configured blacklist are replaced by a duplicate
    /// of another entry. Everything else on the list stays blocked.
    /// </summary>
    private static void ConfigureDocSpace(IDistributedApplicationBuilder builder, Dictionary<string, string> modules)
    {
        string[] consumers =
        [
            ProjectConfigurator.GetProjectName<ASC_Web_Api>(),
            ProjectConfigurator.GetProjectName<ASC_Web_Studio>(),
            ProjectConfigurator.GetProjectName<ASC_Monolith>()
        ];

        foreach (var resource in builder.Resources.OfType<IResourceWithEnvironment>().Where(r => consumers.Contains(r.Name)))
        {
            var resourceBuilder = builder.CreateResourceBuilder(resource);

            foreach (var (module, keyHash) in modules)
            {
                resourceBuilder
                    .WithEnvironment($"{ModulesSection}:{module}:keyHash", keyHash)
                    .WithEnvironment($"{ModulesSection}:{module}:tenants", "*");
            }

            // indexes of "127.0.0.0/8" and "::1/128" in webhooks:blacklist of buildtools/config/appsettings.json
            resourceBuilder
                .WithEnvironment("webhooks:blacklist:3", "0.0.0.0/8")
                .WithEnvironment("webhooks:blacklist:8", "::/128");
        }
    }

    private static bool IsNodeIntegration(string pluginDirectory)
    {
        var packageJson = Path.Combine(pluginDirectory, "package.json");

        if (!File.Exists(packageJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJson));

            return document.RootElement.TryGetProperty(PackageJsonMarker, out var marker) && marker.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            // a broken package.json of some client plugin must not stop the whole AppHost
            return false;
        }
    }

    // Aspire resource names and module names allow only ASCII letters, digits and single hyphens
    private static string ResourceName(string directoryName)
    {
        return NotAllowedChars().Replace(directoryName.Trim().ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotAllowedChars();
}
