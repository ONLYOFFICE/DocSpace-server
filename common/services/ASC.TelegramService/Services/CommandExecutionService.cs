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

using System.Globalization;

namespace ASC.TelegramService.Services;

[Singleton]
public partial class CommandExecutionService
{
    [GeneratedRegex(@"^\/([^\s]+)\s?(.*)")]
    private static partial Regex CmdRegex();
    private readonly Regex _cmdReg = CmdRegex();

    [GeneratedRegex(@"[^""\s]\S*|"".+?""")]
    private static partial Regex ArgsRegex();
    private readonly Regex _argsReg = ArgsRegex();

    private readonly Dictionary<string, MethodInfo> _commands = [];
    private readonly Dictionary<string, Type> _contexts = [];
    private readonly Dictionary<Type, ParamParser> _parsers = [];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandExecutionService> _log;

    public CommandExecutionService(ILogger<CommandExecutionService> logger, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _log = logger;

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var t in assembly.GetExportedTypes())
        {
            if (t.IsAbstract)
            {
                continue;
            }

            if (t.IsSubclassOf(typeof(CommandContext)))
            {
                foreach (var method in t.GetRuntimeMethods())
                {
                    if (method.IsPublic && Attribute.IsDefined(method, typeof(CommandAttribute)))
                    {
                        var attr = method.GetCustomAttribute<CommandAttribute>();
                        _commands.Add(attr.Name, method);
                        _contexts.Add(attr.Name, t);
                    }
                }
            }

            if (t.IsSubclassOf(typeof(ParamParser)) && Attribute.IsDefined(t, typeof(ParamParserAttribute)))
            {
                _parsers.Add(t.GetCustomAttribute<ParamParserAttribute>().Type, (ParamParser)Activator.CreateInstance(t));
            }
        }
    }

    private TelegramCommand ParseCommand(Message msg)
    {
        var reg = _cmdReg.Match(msg.Text);
        var args = _argsReg.Matches(reg.Groups[2].Value);

        return new TelegramCommand(msg, reg.Groups[1].Value.ToLowerInvariant(), args.Count > 0 ? [.. args.Select(a => a.Value)] : null);
    }

    private object[] ParseParams(MethodInfo cmd, string[] args)
    {
        var parsedParams = new List<object>();

        var cmdArgs = cmd.GetParameters();

        if (cmdArgs.Length > 0 && args == null || cmdArgs.Length != args.Length)
        {
            throw new Exception("Wrong parameters count");
        }

        for (var i = 0; i < cmdArgs.Length; i++)
        {
            var type = cmdArgs[i].ParameterType;
            var arg = args[i];
            if (type == typeof(string))
            {
                parsedParams.Add(arg);
                continue;
            }

            if (!_parsers.TryGetValue(type, out var value))
            {
                throw new Exception(string.Format("No parser found for type '{0}'", type));
            }

            parsedParams.Add(value.FromString(arg));
        }

        return [.. parsedParams];
    }

    public void HandleCommand(Message msg, ITelegramBotClient client, int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var cmd = ParseCommand(msg);

            if (!_commands.TryGetValue(cmd.CommandName, out var command))
            {
                throw new Exception($"No handler found for command '{cmd.CommandName}'");
            }

            var context = (CommandContext)_scopeFactory.CreateScope().ServiceProvider.GetService(_contexts[cmd.CommandName]);
            var param = ParseParams(command, cmd.Args);

            context.Context = cmd;
            context.Client = client;
            context.TenantId = tenantId;
            context.TelegramCulture = GetUserCulture(cmd.User.LanguageCode);

            cancellationToken.ThrowIfCancellationRequested();
            command.Invoke(context, param);
        }
        catch (Exception ex)
        {
            _log.DebugCouldntHandle(msg.Text, ex);
        }
    }

    private CultureInfo GetUserCulture(string langCode)
    {
        try
        {
            return CultureInfo.GetCultureInfo(langCode);
        }
        catch
        {
            return null;
        }
    }
}