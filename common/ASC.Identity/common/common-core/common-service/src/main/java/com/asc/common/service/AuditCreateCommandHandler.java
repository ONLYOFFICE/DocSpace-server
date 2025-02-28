// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
