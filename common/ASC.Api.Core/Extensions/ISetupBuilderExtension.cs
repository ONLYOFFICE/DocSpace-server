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

using Amazon.Runtime;

namespace ASC.Api.Core.Extensions;

public static class ISetupBuilderExtension
{
    public static ISetupBuilder LoadConfiguration(this ISetupBuilder loggingBuilder, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var conf = new XmlLoggingConfiguration(CrossPlatform.PathCombine(configuration["pathToConf"], "nlog.config"));

        // Resolve service name for the NLog OTLP target. AppHost sets OTEL_SERVICE_NAME in container env;
        // standalone runs fall back to appsettings or the host application name. We never mutate the
        // process environment — passing through conf.Variables keeps the resolution local to this NLog setup.
        conf.Variables["serviceName"] = new[]
        {
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"),
            configuration["openTelemetry:ServiceName"],
            hostEnvironment.ApplicationName,
            Assembly.GetEntryAssembly()?.GetName().Name
        }.FirstOrDefault(static s => !string.IsNullOrWhiteSpace(s));

        var settings = configuration.GetSection("log").Get<NLogSettings>();

        if (settings == null)
        {
            return loggingBuilder;
        }
        
        if (!string.IsNullOrEmpty(settings.Name))
        {
            conf.Variables["name"] = settings.Name;
        }

        if (!string.IsNullOrEmpty(settings.Dir))
        {
            var dir = Path.IsPathRooted(settings.Dir) ? settings.Dir : CrossPlatform.PathCombine(hostEnvironment.ContentRootPath, settings.Dir);
            conf.Variables["dir"] = dir.TrimEnd('/').TrimEnd('\\') + Path.DirectorySeparatorChar;
        }

        foreach (var targetName in new[] { "aws", "aws_sql" })
        {
            var awsTarget = conf.FindTargetByName<AWSTarget>(targetName);

            if (awsTarget == null)
            {
                continue;
            }

            //hack
            if (!string.IsNullOrEmpty(settings.Name))
            {
                awsTarget.LogGroup = awsTarget.LogGroup.Replace("${var:name}", settings.Name);
            }


            var awsAccessKeyId = string.IsNullOrEmpty(settings.AWSAccessKeyId) ? configuration["aws:cloudWatch:accessKeyId"] : settings.AWSAccessKeyId;
            var awsSecretAccessKey = string.IsNullOrEmpty(settings.AWSSecretAccessKey) ? configuration["aws:cloudWatch:secretAccessKey"] : settings.AWSSecretAccessKey;

            if (!string.IsNullOrEmpty(awsAccessKeyId))
            {
                awsTarget.Credentials = new BasicAWSCredentials(awsAccessKeyId, awsSecretAccessKey);
            }
            else
            {
                conf.RemoveTarget(targetName);
            }
        }

        return loggingBuilder.LoadConfiguration(conf);
    }
}