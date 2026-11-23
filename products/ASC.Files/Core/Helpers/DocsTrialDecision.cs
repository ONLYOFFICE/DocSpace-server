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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// The decisions the setup wizard and its page take on the editing service's trial, as functions of what was read,
/// so that a unit test reaches them: the paths that take them need an Enterprise installation next to an editing
/// service on its trial.
/// </summary>
public static class DocsTrialDecision
{
    /// <summary>
    /// Whether the wizard page has to offer the upload of a license file: unless the editing service runs an active
    /// trial and the installation's trial would still be in force once that trial is applied.
    /// </summary>
    /// <param name="answer">The editing service's answer about its trial, possibly the cached one.</param>
    /// <param name="trialWouldBeInForce">
    /// Whether the installation's trial would be in force if <paramref name="answer"/>'s trial were applied now; only
    /// asked for an active trial, so <c>false</c> for any other answer.
    /// </param>
    public static bool IsLicenseFileRequired(DocsTrialCheck answer, bool trialWouldBeInForce)
    {
        return answer.State != DocsTrialState.Active || !trialWouldBeInForce;
    }

    /// <summary>
    /// Whether the wizard completes on the installation's own trial while the editing service does not answer.
    /// </summary>
    /// <param name="ownTrialEnd">The stored end of the installation's own trial; <c>null</c> when none is in the tariff.</param>
    /// <param name="lastKnown">The last answer of the editing service the installation knows; <c>null</c> when none is.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <remarks>
    /// The own trial has to be in force through today. The last answer decides as the wizard page decided on it: an
    /// answer of no trial or of an ended trial asked for a file, and so does an active trial that would already be
    /// over once applied. With no answer known the page asked for a file too, but the wizard still completes on the
    /// own trial by decision: an editing service that never answered is not read as one without a trial, and the
    /// background refresh brings the end of the trial of the first tariff read closer once it answers (a trial
    /// activated on request keeps its end).
    /// </remarks>
    public static bool CompletesOnOwnTrial(DateTime? ownTrialEnd, DocsTrialCheck lastKnown, DateTime utcNow)
    {
        if (ownTrialEnd is not { } end || end.Date < utcNow.Date)
        {
            return false;
        }

        return lastKnown?.State switch
        {
            null or DocsTrialState.Unknown => true,
            DocsTrialState.Active => LicenseReader.WillTrialBeInForce(end, lastKnown.Trial.DueDate, utcNow),
            _ => false
        };
    }
}
