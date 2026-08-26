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

namespace ASC.MessagingSystem.Core.Sender;

[Singleton]
public class AuditLogSender(ILoggerFactory loggerFactory, ILogger<AuditLogSender> logger)
{
    public const string LoggerName = "ASC.Audit";

    private const int MaxIpLength = 50;
    private const int MaxInitiatorLength = 200;
    private const int MaxTargetLength = 256;
    private const int MaxUserAgentLength = 512;
    private const int MaxPageLength = 512;
    private const int MaxDescriptionLength = 2048;
    private const string TruncationMarker = "...";

    private readonly ILogger _auditLogger = loggerFactory.CreateLogger(LoggerName);

    public void Send(EventMessage message)
    {
        if (message == null || !_auditLogger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        try
        {
            _auditLogger.InfoAuditEvent(
                MessagesRepository.IsLoginEvent(message.Action) ? "login" : "audit",
                message.Action.ToStringFast(),
                (int)message.Action,
                message.TenantId,
                message.UserId,
                Sanitize(message.Initiator, MaxInitiatorLength),
                Sanitize(message.Ip, MaxIpLength),
                Sanitize(message.Page, MaxPageLength),
                Sanitize(message.UaHeader, MaxUserAgentLength),
                Sanitize(message.Target?.ToString(), MaxTargetLength),
                SerializeDescription(message.Description),
                message.Date);
        }
        catch (Exception ex)
        {
            logger.ErrorFailedSendToAuditLog(ex);
        }
    }

    private static string SerializeDescription(IList<string> description)
    {
        if (description is not { Count: > 0 })
        {
            return null;
        }

        var remaining = MaxDescriptionLength;
        var items = new List<string>(description.Count);

        foreach (var item in description)
        {
            if (remaining <= 0)
            {
                break;
            }

            var safe = Sanitize(item, remaining);
            if (safe == null)
            {
                continue;
            }

            remaining -= safe.Length;
            items.Add(safe);
        }

        return items.Count > 0 ? JsonSerializer.Serialize(items) : null;
    }

    private static string Sanitize(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var result = value;
        if (value.Length > maxLength)
        {
            // the marker counts towards maxLength so the result never exceeds it
            result = maxLength > TruncationMarker.Length
                ? string.Concat(value.AsSpan(0, maxLength - TruncationMarker.Length), TruncationMarker)
                : value[..maxLength];
        }

        return ContainsUnsafeCharacter(result) ? ReplaceUnsafeCharacters(result) : result;
    }

    // control characters forge extra log lines; '|' is the field delimiter of the InfoAuditEvent message template
    private static bool IsUnsafeCharacter(char c)
    {
        return char.IsControl(c) || c == '|';
    }

    private static bool ContainsUnsafeCharacter(string value)
    {
        return value.Any(IsUnsafeCharacter);
    }

    private static string ReplaceUnsafeCharacters(string value)
    {
        return string.Create(value.Length, value, static (buffer, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                buffer[i] = IsUnsafeCharacter(source[i]) ? ' ' : source[i];
            }
        });
    }
}
