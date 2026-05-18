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

package com.asc.common.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.ports.output.repository.AuditCommandRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Handles the creation of audit records by interacting with the {@link AuditCommandRepository}.
 *
 * <p>This class is responsible for saving single or multiple audit records in the repository. It
 * uses Spring's {@link Transactional} annotation to manage transactions and ensures that the
 * operations are completed within specified timeouts.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuditCreateCommandHandler {

  private final AuditCommandRepository auditCommandRepository;

  /**
   * Creates a single audit record.
   *
   * <p>This method saves a single audit record in the repository. The transaction timeout for this
   * operation is set to 2 seconds.
   *
   * @param audit the audit record to be saved
   */
  @Transactional(timeout = 2)
  public void createAudit(Audit audit) {
    auditCommandRepository.saveAudit(audit);
  }

  /**
   * Creates multiple audit records.
   *
   * <p>This method saves multiple audit records in the repository. The transaction timeout for this
   * operation is set to 4 seconds.
   *
   * @param audits the iterable collection of audit records to be saved
   */
  @Transactional(timeout = 4)
  public void createAudits(Iterable<Audit> audits) {
    for (var audit : audits) auditCommandRepository.saveAudit(audit);
  }
}
