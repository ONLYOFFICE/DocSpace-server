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
/// A stand-in for an interface that records every call and passes it on to a real implementation, or fails it when
/// there is none. A test reads what the code under test asked of a dependency it does not own (the options a value
/// was cached with), or that it asked nothing at all.
/// </summary>
public class RecordingProxy<T> : DispatchProxy where T : class
{
    private T? _inner;
    private Action<MethodInfo, object?[]>? _onCall;

    /// <summary>The calls in the order they were made: the member and its arguments.</summary>
    public List<(MethodInfo Method, object?[] Args)> Calls { get; } = [];

    /// <param name="inner">The implementation every call is passed to; null fails each call after recording it.</param>
    /// <param name="onCall">Runs after a call is recorded and before it is passed on: what another request does meanwhile.</param>
    public static (T Proxy, RecordingProxy<T> Recorder) Create(T? inner, Action<MethodInfo, object?[]>? onCall = null)
    {
        var proxy = Create<T, RecordingProxy<T>>();
        var recorder = (RecordingProxy<T>)(object)proxy;
        recorder._inner = inner;
        recorder._onCall = onCall;

        return (proxy, recorder);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        Calls.Add((targetMethod, args ?? []));
        _onCall?.Invoke(targetMethod, args ?? []);

        if (_inner is null)
        {
            throw new NotSupportedException($"{typeof(T).Name}.{targetMethod.Name} was not expected by the test");
        }

        try
        {
            return targetMethod.Invoke(_inner, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
