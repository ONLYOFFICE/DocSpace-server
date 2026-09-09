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

package com.asc.authorization.application.security.oauth.service;

import java.util.Optional;

/**
 * Resolves who owns a registered client, so that grants can record the client's owner alongside the
 * user who granted them and be removed when that owner's clients are removed.
 */
public interface RegisteredClientOwnerService {
  /**
   * The tenant a client belongs to and the user who created it.
   *
   * @param tenantId the tenant owning the client
   * @param userId the user who created the client, null when unknown
   */
  record Owner(long tenantId, String userId) {}

  /**
   * Loads the owner of a client. Unlike {@link
   * RegisteredClientAccessibilityService#findAccessibleClient(String)} this ignores whether the
   * client is public and enabled, because a grant to a private or disabled client still has to
   * record its owner.
   *
   * @param clientId the client ID whose owner to resolve
   * @return the owner, or empty if the client could not be resolved
   */
  Optional<Owner> findClientOwner(String clientId);
}
