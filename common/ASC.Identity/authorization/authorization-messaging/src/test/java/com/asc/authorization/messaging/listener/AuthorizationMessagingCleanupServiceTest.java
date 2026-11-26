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

package com.asc.authorization.messaging.listener;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import java.time.Instant;
import org.junit.jupiter.api.Test;
import org.springframework.transaction.PlatformTransactionManager;

/** How long {@link AuthorizationMessagingCleanupService} keeps removing chunks. */
class AuthorizationMessagingCleanupServiceTest {
  private final JpaAuthorizationRepository authorizations = mock(JpaAuthorizationRepository.class);
  private final JpaConsentRepository consents = mock(JpaConsentRepository.class);
  private final AuthorizationMessagingCleanupService service =
      new AuthorizationMessagingCleanupService(
          mock(PlatformTransactionManager.class), authorizations, consents);

  /**
   * A chunk that deletes fewer rows than its size, as one does on PostgreSQL when another instance
   * removed some of the rows it picked, does not end the removal: the next chunk deletes the rest.
   */
  @Test
  void onClientRemoved_ShortChunk_KeepsRemovingUntilAChunkDeletesNothing() {
    when(authorizations.deleteAuthorizationsByClientId(eq("client"), anyInt()))
        .thenReturn(1000, 400, 300, 0);
    when(consents.deleteConsentsByClientId(eq("client"), anyInt())).thenReturn(0);

    service.onClientRemoved("client");

    verify(authorizations, times(4)).deleteAuthorizationsByClientId("client", 1000);
    verify(consents, times(1)).deleteConsentsByClientId("client", 500);
  }

  @Test
  void onUserRemoved_NothingToRemove_AsksOncePerKind() {
    when(authorizations.deleteAuthorizationsByPrincipalId(eq("user"), any(), anyInt()))
        .thenReturn(0);
    when(consents.deleteConsentsByPrincipalId(eq("user"), any(), anyInt())).thenReturn(0);
    when(authorizations.deleteAuthorizationsByOwner(eq(1L), eq("user"), any(), anyInt()))
        .thenReturn(0);
    when(consents.deleteConsentsByOwner(eq(1L), eq("user"), any(), anyInt())).thenReturn(0);

    service.onUserRemoved(1, "user", Instant.parse("2026-11-26T00:00:00Z"));

    verify(authorizations, times(1)).deleteAuthorizationsByPrincipalId(eq("user"), any(), eq(1000));
    verify(consents, times(1)).deleteConsentsByPrincipalId(eq("user"), any(), eq(500));
    verify(authorizations, times(1))
        .deleteAuthorizationsByOwner(eq(1L), eq("user"), any(), eq(1000));
    verify(consents, times(1)).deleteConsentsByOwner(eq(1L), eq("user"), any(), eq(500));
  }
}
