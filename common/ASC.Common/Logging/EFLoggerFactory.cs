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

namespace ASC.Common.Logging;

[Singleton]
public class EFLoggerFactory(ILoggerFactory loggerFactory) : ILoggerFactory
{
    // Create the "ASC.SQL" logger from the application ILoggerFactory so EF command logs fan out to every
    // registered provider (NLog -> sql file/AWS, and OpenTelemetry -> OTLP/structured logging).
    private readonly ILogger _logger = new EFLogger(loggerFactory.CreateLogger("ASC.SQL"));

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose() { }
}

public partial class EFLogger(ILogger logger) : ILogger
{
    internal const string RedactedParameters = "[redacted]";

    // The key material of private rooms and of their external links: sealed private keys, the proof hash of a link,
    // the wraps of file keys and of link secrets, and the users' own key pairs (the e2e profiles of account_links, kept
    // with the other linked profiles there). A command that writes these tables is logged without its parameter values,
    // which the SQL log would otherwise keep (sensitive data logging is on), so they never reach a log file or the OTLP
    // exporter. Reads keep their parameters: they bind ids only, never the values.
    [GeneratedRegex(@"\b(INSERT\s+INTO|UPDATE)\s+[`""]?(files_share_link_keys|files_share_link_secret_wraps|files_file_keys|account_links)[`""]?(\s|\()",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WritesKeyMaterial();

    public IDisposable BeginScope<TState>(TState state)
    {
        return logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        switch (eventId.Id)
        {
            case 20101:
                if (TryRedact(state, out var redacted, out var parameters))
                {
                    logger.Log(LogLevel.Debug,
                        default,
                        redacted,
                        exception,
                        (_, e) => formatter(state, e).Replace(parameters, RedactedParameters));
                    break;
                }

                logger.Log(LogLevel.Debug,
                        default,
                        state,
                        exception,
                        formatter);
                break;
        }
    }

    /// <summary>
    /// Gives the properties of a logged command with the parameter values replaced, when the command writes key material.
    /// </summary>
    internal static bool TryRedact<TState>(TState state, out IReadOnlyList<KeyValuePair<string, object>> redacted, out string parameters)
    {
        redacted = null;
        parameters = null;

        if (state is not IReadOnlyList<KeyValuePair<string, object>> properties)
        {
            return false;
        }

        string commandText = null;
        foreach (var (key, value) in properties)
        {
            switch (key)
            {
                case "commandText":
                    commandText = value as string;
                    break;
                case "parameters":
                    parameters = value as string;
                    break;
            }
        }

        if (string.IsNullOrEmpty(parameters) || commandText == null || !WritesKeyMaterial().IsMatch(commandText))
        {
            parameters = null;
            return false;
        }

        redacted = properties
            .Select(p => p.Key == "parameters" ? new KeyValuePair<string, object>(p.Key, RedactedParameters) : p)
            .ToList();

        return true;
    }
}