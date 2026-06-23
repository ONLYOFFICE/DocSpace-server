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

package com.asc.authorization.application.mapper;

import com.asc.common.data.audit.entity.AuditEntity;
import com.asc.common.service.transfer.message.AuditMessage;
import org.springframework.stereotype.Component;

/**
 * Mapper component responsible for converting audit-related data between different representations.
 * This mapper specifically handles the conversion from {@link AuditMessage} to {@link AuditEntity}.
 *
 * <p>The mapper follows the standard mapping pattern used throughout the application to transform
 * data transfer objects (DTOs) into domain entities for persistence.
 */
@Component
public class AuditMapper {

  /**
   * Converts an {@link AuditMessage} to an {@link AuditEntity}.
   *
   * <p>This method performs a one-to-one mapping of all fields from the audit message to the
   * corresponding audit entity. All fields are copied directly without any transformation or
   * validation logic.
   *
   * <p>The mapping includes the following fields:
   *
   * <ul>
   *   <li>Initiator - The user or system that initiated the action
   *   <li>Target - The target of the action being audited
   *   <li>IP - The IP address from which the action was performed
   *   <li>Browser - The browser information
   *   <li>Platform - The platform information
   *   <li>Date - The timestamp when the action occurred
   *   <li>Tenant ID - The tenant identifier
   *   <li>User ID - The user identifier
   *   <li>Page - The page where the action occurred
   *   <li>Action - The type of action performed
   *   <li>Description - Additional description of the action
   * </ul>
   *
   * @param auditMessage the audit message to convert, must not be null
   * @return a new {@link AuditEntity} instance with all fields mapped from the input message
   * @throws NullPointerException if the auditMessage parameter is null
   */
  public AuditEntity toEntity(AuditMessage auditMessage) {
    return AuditEntity.builder()
        .initiator(auditMessage.getInitiator())
        .target(auditMessage.getTarget())
        .ip(auditMessage.getIp())
        .browser(auditMessage.getBrowser())
        .platform(auditMessage.getPlatform())
        .date(auditMessage.getDate())
        .tenantId(auditMessage.getTenantId())
        .userId(auditMessage.getUserId())
        .page(auditMessage.getPage())
        .action(auditMessage.getAction())
        .description(auditMessage.getDescription())
        .build();
  }
}
