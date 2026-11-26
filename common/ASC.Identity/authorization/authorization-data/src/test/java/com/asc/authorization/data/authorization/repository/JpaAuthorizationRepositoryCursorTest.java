// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
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

package com.asc.authorization.data.authorization.repository;

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.withSettings;

import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import org.junit.jupiter.api.Test;
import org.mockito.Answers;

/**
 * Which query {@link JpaAuthorizationRepository#findConsentedAuthorizationsByPrincipalId} sends.
 */
class JpaAuthorizationRepositoryCursorTest {
  private static final ZonedDateTime CURSOR =
      ZonedDateTime.of(2026, 11, 26, 10, 0, 0, 0, ZoneOffset.UTC);

  private final JpaAuthorizationRepository repository =
      mock(
          JpaAuthorizationRepository.class,
          withSettings().defaultAnswer(Answers.CALLS_REAL_METHODS));

  @Test
  void consented_NoCursor_ReadsTheFirstPage() {
    repository.findConsentedAuthorizationsByPrincipalId("user", null, 21);

    verify(repository).findFirstConsentedAuthorizationsByPrincipalId("user", 21);
    verify(repository, never()).findConsentedAuthorizationsByPrincipalIdBefore("user", null, 21);
  }

  @Test
  void consented_Cursor_ReadsThePageBeforeIt() {
    repository.findConsentedAuthorizationsByPrincipalId("user", CURSOR, 21);

    verify(repository).findConsentedAuthorizationsByPrincipalIdBefore("user", CURSOR, 21);
    verify(repository, never()).findFirstConsentedAuthorizationsByPrincipalId("user", 21);
  }
}
