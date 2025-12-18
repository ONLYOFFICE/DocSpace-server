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
