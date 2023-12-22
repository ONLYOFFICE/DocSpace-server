// (c) Copyright Ascensio System SIA 2010-2023
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Common.Logging;

[Singleton]
public class EFLoggerFactory(ILoggerProvider loggerProvider) : ILoggerFactory
{
    private readonly ILogger _logger = new EFLogger(loggerProvider.CreateLogger("ASC.SQL"));

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose() { }
}

public class EFLogger(ILogger logger) : ILogger
{
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
                var keyValuePairs = state as IEnumerable<KeyValuePair<string, object>>;
                var ev = new EFLogEvent("");

                foreach (var kv in keyValuePairs)
                {
                    ev.WithProperty(kv.Key, kv.Value);
                }

                logger.Log(LogLevel.Debug,
                        default,
                        ev,
                        exception,
                        EFLogEvent.Formatter);
                break;
        }
    }

    class EFLogEvent(string message) : IEnumerable<KeyValuePair<string, object>>
    {
        readonly List<KeyValuePair<string, object>> _properties = new();

        public string Message { get; } = message;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public void WithProperty(string name, object value)
        {
            _properties.Add(new KeyValuePair<string, object>(name, value));
        }

        public static Func<EFLogEvent, Exception, string> Formatter { get; } = (l, _) => l.Message;
    }

}
