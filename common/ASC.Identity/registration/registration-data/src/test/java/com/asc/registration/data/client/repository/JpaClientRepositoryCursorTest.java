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

package com.asc.registration.data.client.repository;

import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.withSettings;

import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import org.junit.jupiter.api.Test;
import org.mockito.Answers;

/** Which query the cursor methods of {@link JpaClientRepository} send for each page. */
class JpaClientRepositoryCursorTest {
  private static final ZonedDateTime CURSOR =
      ZonedDateTime.of(2026, 11, 26, 10, 0, 0, 0, ZoneOffset.UTC);

  private final JpaClientRepository repository =
      mock(JpaClientRepository.class, withSettings().defaultAnswer(Answers.CALLS_REAL_METHODS));

  @Test
  void tenantClients_NoCursor_ReadsTheFirstPage() {
    repository.findAllByTenantIdWithCursor(1, null, 31);

    verify(repository).findFirstPageByTenantId(1, 31);
    verify(repository, never()).findPageByTenantIdBefore(1, null, 31);
  }

  @Test
  void tenantClients_Cursor_ReadsThePageBeforeIt() {
    repository.findAllByTenantIdWithCursor(1, CURSOR, 31);

    verify(repository).findPageByTenantIdBefore(1, CURSOR, 31);
    verify(repository, never()).findFirstPageByTenantId(1, 31);
  }

  @Test
  void creatorClients_NoCursor_ReadsTheFirstPage() {
    repository.findAllByTenantIdAndCreatedByWithCursor(1, "user", null, 31);

    verify(repository).findFirstPageByTenantIdAndCreatedBy(1, "user", 31);
    verify(repository, never()).findPageByTenantIdAndCreatedByBefore(1, "user", null, 31);
  }

  @Test
  void creatorClients_Cursor_ReadsThePageBeforeIt() {
    repository.findAllByTenantIdAndCreatedByWithCursor(1, "user", CURSOR, 31);

    verify(repository).findPageByTenantIdAndCreatedByBefore(1, "user", CURSOR, 31);
    verify(repository, never()).findFirstPageByTenantIdAndCreatedBy(1, "user", 31);
  }
}
