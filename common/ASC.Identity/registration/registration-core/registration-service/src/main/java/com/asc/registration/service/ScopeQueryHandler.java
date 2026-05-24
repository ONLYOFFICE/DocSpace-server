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

package com.asc.registration.service;

import com.asc.registration.core.domain.exception.ScopeNotFoundException;
import com.asc.registration.service.mapper.ScopeDataMapper;
import com.asc.registration.service.ports.output.repository.ScopeQueryRepository;
import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

/**
 * ScopeQueryHandler handles query operations related to scopes. It retrieves scope information from
 * the repository and maps it to response objects.
 */
@Slf4j
@RequiredArgsConstructor
public class ScopeQueryHandler {
  private final ScopeQueryRepository queryRepository;

  private final ScopeDataMapper dataMapper;

  /**
   * Retrieves a specific scope by its name.
   *
   * @param name the name of the scope to retrieve.
   * @return a {@link ScopeResponse} representing the requested scope.
   * @throws ScopeNotFoundException if the scope with the specified name is not found.
   */
  public ScopeResponse getScope(String name) {
    log.info("Trying to get scope by name: {}", name);

    var scope =
        queryRepository
            .findByName(name)
            .orElseThrow(
                () ->
                    new ScopeNotFoundException(
                        String.format("Scope with name %s was not found", name)));

    return dataMapper.toScopeResponse(scope);
  }

  /**
   * Retrieves all available scopes.
   *
   * @return a set of {@link ScopeResponse} representing all scopes.
   */
  public Set<ScopeResponse> getScopes() {
    log.info("Trying to get scopes");

    return StreamSupport.stream(queryRepository.findAll().spliterator(), false)
        .map(dataMapper::toScopeResponse)
        .collect(Collectors.toSet());
  }
}
