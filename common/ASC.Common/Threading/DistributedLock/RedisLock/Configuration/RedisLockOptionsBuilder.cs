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

namespace ASC.Common.Threading.DistributedLock.RedisLock.Configuration;

public sealed class RedisLockOptionsBuilder
{
    private static readonly TimeSpan _defaultExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _minimumExpiry = TimeSpan.FromSeconds(.1);

    private static readonly TimeSpan _defaultMinimumTimeout = TimeSpan.FromSeconds(30);

    private TimeSpan _expiry = _defaultExpiry;
    private TimeSpan _timeout = _defaultMinimumTimeout;
    private TimeSpan _extendInterval;

    private RedisLockOptionsBuilder() { }

    public RedisLockOptionsBuilder Expiry(TimeSpan expiry)
    {
        if (expiry < _minimumExpiry || expiry == Timeout.InfiniteTimeSpan || expiry == TimeSpan.MaxValue)
        {
            return this;
        }

        _expiry = expiry;
        return this;
    }

    public RedisLockOptionsBuilder ExtendInterval(TimeSpan extendInterval)
    {
        _extendInterval = extendInterval;
        return this;
    }

    public RedisLockOptionsBuilder MinTimeout(TimeSpan minTimeout)
    {
        if (minTimeout < _defaultMinimumTimeout || minTimeout == Timeout.InfiniteTimeSpan || minTimeout == TimeSpan.MaxValue)
        {
            return this;
        }

        _timeout = minTimeout;
        return this;
    }

    internal static RedisLockOptions GetOptions(Action<RedisLockOptionsBuilder> optionsBuilder)
    {
        RedisLockOptionsBuilder options;
        if (optionsBuilder != null)
        {
            options = new RedisLockOptionsBuilder();
            optionsBuilder(options);
        }
        else
        {
            options = null;
        }

        var expiry = options?._expiry ?? _defaultExpiry;
        var timeout = options?._timeout ?? _defaultMinimumTimeout;

        var extendInterval = expiry / 3;

        if (options?._extendInterval is { } optExtendInterval
            && optExtendInterval > TimeSpan.Zero && optExtendInterval < expiry)
        {
            extendInterval = optExtendInterval;
        }

        return new RedisLockOptions
        {
            Expiry = expiry,
            ExtendInterval = extendInterval,
            MinTimeout = timeout
        };
    }
}