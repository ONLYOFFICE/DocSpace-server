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

namespace ASC.Core.Common.Tests;

/// <summary>
/// A service behind the portal that fails is reported with a <see cref="CustomHttpException"/> of 502. It was logged
/// only as information, as a refusal of the caller is, while the same failures were critical as plain exceptions; it
/// is critical again. A 4xx stays information. The answer carries the fixed text of the exception, not the message of
/// the cause, which can name an internal host.
/// </summary>
public class CustomExceptionHandlerUpstreamTests
{
    private const string InternalHost = "10.0.0.5:8080";

    private static readonly JsonSerializerOptions _responseOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, LogLevel.Critical)]
    [InlineData(HttpStatusCode.BadRequest, LogLevel.Information)]
    public async Task CustomHttpException_IsLoggedByItsStatus(HttpStatusCode status, LogLevel expected)
    {
        var logger = new RecordingLogger();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new CustomHttpException(status, "The service could not be reached", new HttpRequestException($"Connection refused ({InternalHost})"));

        var handled = await new CustomExceptionHandler(logger).TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)status);
        logger.Levels.Should().Equal(expected);

        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ErrorApiResponse>(context.Response.Body, _responseOptions, TestContext.Current.CancellationToken);

        response!.Error.Message.Should().Be("The service could not be reached");
        response.Error.Stack.Should().BeNull();
    }

    private sealed class RecordingLogger : ILogger<CustomExceptionHandler>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
        }
    }
}
