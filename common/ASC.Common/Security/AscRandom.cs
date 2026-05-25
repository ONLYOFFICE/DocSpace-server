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

namespace ASC.Common.Security;

public class AscRandom : Random
{
    private int _inext;
    private int _inextp;
    private readonly int[] _seeds;

    public AscRandom() : this(Environment.TickCount) { }

    public AscRandom(int seed)
    {
        _seeds = new int[56];
        var num4 = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
        var num2 = 161803398 - num4;
        _seeds[^1] = num2;
        var num3 = 1;

        for (var i = 1; i < _seeds.Length - 1; i++)
        {
            var index = 21 * i % (_seeds.Length - 1);
            _seeds[index] = num3;
            num3 = num2 - num3;

            if (num3 < 0)
            {
                num3 += int.MaxValue;
            }

            num2 = _seeds[index];
        }

        for (var j = 1; j < 5; j++)
        {
            for (var k = 1; k < _seeds.Length; k++)
            {
                _seeds[k] -= _seeds[1 + (k + 30) % (_seeds.Length - 1)];

                if (_seeds[k] < 0)
                {
                    _seeds[k] += int.MaxValue;
                }
            }
        }

        _inext = 0;
        _inextp = 21;
    }

    public override int Next(int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

        return (int)(InternalSample() * 4.6566128752457969E-10 * maxValue);
    }

    public override void NextBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(InternalSample() % (byte.MaxValue + 1));
        }
    }

    private int InternalSample()
    {
        var inext = _inext;
        var inextp = _inextp;

        if (++inext >= _seeds.Length - 1)
        {
            inext = 1;
        }

        if (++inextp >= _seeds.Length - 1)
        {
            inextp = 1;
        }

        var num = _seeds[inext] - _seeds[inextp];

        if (num == int.MaxValue)
        {
            num--;
        }

        if (num < 0)
        {
            num += int.MaxValue;
        }

        _seeds[inext] = num;
        _inext = inext;
        _inextp = inextp;

        return num;
    }
}