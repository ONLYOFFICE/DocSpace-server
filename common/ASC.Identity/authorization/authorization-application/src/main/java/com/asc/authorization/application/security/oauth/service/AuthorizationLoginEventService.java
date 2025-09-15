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

package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.application.exception.authorization.AuthorizationPersistenceException;
import com.asc.authorization.application.mapper.AuditMapper;
import com.asc.authorization.application.mapper.LoginEventMapper;
import com.asc.authorization.data.login.repository.JpaLoginEventRepository;
import com.asc.common.data.audit.repository.JpaAuditRepository;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.common.service.transfer.message.LoginRegisteredEvent;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.TransactionDefinition;
import org.springframework.transaction.support.TransactionTemplate;
import org.springframework.validation.annotation.Validated;

/**
 * Service implementation for registering login events in the authorization system. This service
 * handles the persistence of login events and their associated audit information using
 * transactional operations to ensure data consistency.
 *
 * <p>The service implements the {@link AuthorizationLoginEventRegistrationService} interface and
 * provides concrete functionality for storing login events and audit messages in the database. It
 * uses Spring's transaction management to ensure atomicity of the registration process.
 *
 * <p>Key features of this service:
 *
 * <ul>
 *   <li>Transactional registration of both login events and audit messages
 *   <li>Automatic rollback on any failure during the registration process
 *   <li>Proper exception handling with custom business exceptions
 *   <li>Read-committed isolation level for optimal performance
 *   <li>Configurable transaction timeout for preventing long-running operations
 * </ul>
 */
@Service
@Validated
@RequiredArgsConstructor
public class AuthorizationLoginEventService implements AuthorizationLoginEventRegistrationService {

  private final PlatformTransactionManager transactionManager;
  private final AuditMapper auditMapper;
  private final LoginEventMapper loginEventMapper;
  private final JpaAuditRepository jpaAuditRepository;
  private final JpaLoginEventRepository jpaLoginEventRepository;

  /**
   * Registers a login event along with its associated audit information using a transactional
   * operation to ensure data consistency.
   *
   * <p>This method performs the following operations within a single transaction:
   *
   * <ol>
   *   <li>Converts the {@link LoginRegisteredEvent} to a {@link
   *       com.asc.authorization.data.login.entity.LoginEventEntity} using the {@link
   *       LoginEventMapper}
   *   <li>Converts the {@link AuditMessage} to a {@link
   *       com.asc.common.data.audit.entity.AuditEntity} using the {@link AuditMapper}
   *   <li>Persists both entities to their respective repositories
   *   <li>Ensures atomicity - if any operation fails, the entire transaction is rolled back
   * </ol>
   *
   * <p>The transaction is configured with:
   *
   * <ul>
   *   <li>Read-committed isolation level for optimal performance and consistency
   *   <li>2-second timeout to prevent long-running operations
   *   <li>Automatic rollback on any exception during execution
   * </ul>
   *
   * <p>If any exception occurs during the registration process, it is wrapped in an {@link
   * AuthorizationPersistenceException} to provide a consistent error handling experience for the
   * calling code.
   *
   * @param loginEvent the login event to register, must not be null
   * @param auditMessage the associated audit message containing audit trail information, must not
   *     be null
   * @throws IllegalArgumentException if either parameter is null or contains invalid data
   * @throws AuthorizationPersistenceException if the registration process fails due to persistence
   *     errors
   */
  @Override
  public void registerLogin(LoginRegisteredEvent loginEvent, AuditMessage auditMessage) {
    try {
      var template = new TransactionTemplate(transactionManager);
      template.setIsolationLevel(TransactionDefinition.ISOLATION_READ_COMMITTED);
      template.setTimeout(2);
      template.executeWithoutResult(
          status -> {
            try {
              jpaLoginEventRepository.save(loginEventMapper.toEntity(loginEvent));
              jpaAuditRepository.save(auditMapper.toEntity(auditMessage));
            } catch (Exception ex) {
              status.setRollbackOnly();
              throw ex;
            }
          });
    } catch (Exception e) {
      throw new AuthorizationPersistenceException(e);
    }
  }
}
