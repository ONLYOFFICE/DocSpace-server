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


namespace ASC.Notify.Engine;

/// <summary>
/// The answer to whether a letter may be built, and, when it was accepted, the acceptance itself. The sending code
/// marks it <see cref="Complete"/> once the letter is handed over for sending; disposed before that - the link, the
/// audit record or the publishing failed - it takes the acceptance back, so a repeat right after the failure builds
/// the letter instead of being refused for one that was never sent.
/// </summary>
public sealed class NotifyAcceptance : IAsyncDisposable
{
    /// <summary>The letter is a repeat: build nothing.</summary>
    public static readonly NotifyAcceptance Refused = new(false, null, null);

    /// <summary>The letter is not checked for repeats, or no acceptance was recorded: build it.</summary>
    public static readonly NotifyAcceptance Unchecked = new(true, null, null);

    private readonly NotifyDeduplicationService _service;
    private readonly string _acceptedKey;
    private bool _completed;

    private NotifyAcceptance(bool accepted, NotifyDeduplicationService service, string acceptedKey)
    {
        Accepted = accepted;
        _service = service;
        _acceptedKey = acceptedKey;
    }

    /// <summary>True when the letter may be built and sent.</summary>
    public bool Accepted { get; }

    internal static NotifyAcceptance Recorded(NotifyDeduplicationService service, string acceptedKey)
    {
        return new NotifyAcceptance(true, service, acceptedKey);
    }

    /// <summary>The letter is on its way: keep the acceptance until it runs out or the engine marks the letter.</summary>
    public void Complete()
    {
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_acceptedKey is null || _completed)
        {
            return;
        }

        _completed = true;

        await _service.ReleaseAcceptanceAsync(_acceptedKey);
    }
}
