// (c) Copyright Ascensio System SIA 2009-2026
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

package com.asc.identity.minified.messaging;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Primary;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Direct implementation of authorization cleanup publishers for minified deployment.
 *
 * <p>Instead of sending events to RabbitMQ, these publishers directly perform the cleanup
 * operations on authorizations and consents. This eliminates the need for RabbitMQ infrastructure
 * in minified deployments.
 */
@Slf4j
@Component
@Profile("minified")
@RequiredArgsConstructor
public class DirectAuthorizationCleanupPublisher {
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final JpaConsentRepository jpaConsentRepository;

  /**
   * Minified-specific publisher that performs immediate cleanup when a single client is removed.
   *
   * <p>Instead of sending an event over RabbitMQ, this implementation directly deletes all
   * authorizations and consents for the given client using JPA repositories. Executed
   * asynchronously and wrapped in a transaction.
   */
  @Primary
  @Component
  @Profile("minified")
  @RequiredArgsConstructor
  public static class ClientRemovedPublisher
      implements AuthorizationMessagePublisher<ClientRemovedEvent> {
    private final JpaAuthorizationRepository authorizationRepository;
    private final JpaConsentRepository consentRepository;

    /**
     * Deletes all authorizations and consents associated with the removed client.
     *
     * @param message event describing the removed client
     */
    @Override
    @Transactional
    public void publish(ClientRemovedEvent message) {
      log.info("Directly cleaning up authorizations for client: {}", message.getClientId());
      try {
        authorizationRepository.deleteAllAuthorizationsByClientId(message.getClientId());
        consentRepository.deleteAllConsentsByClientId(message.getClientId());
        log.info(
            "Authorizations and consents for client {} have been removed", message.getClientId());
      } catch (Exception e) {
        log.error(
            "Failed to cleanup authorizations for client {}: {}",
            message.getClientId(),
            e.getMessage(),
            e);
      }
    }
  }

  /**
   * Minified-specific publisher that cleans up authorizations/consents for a user whose clients
   * have been removed.
   *
   * <p>This bypasses the messaging layer and interacts with the database directly.
   */
  @Primary
  @Component
  @Profile("minified")
  @RequiredArgsConstructor
  public static class UserClientsRemovedPublisher
      implements AuthorizationMessagePublisher<UserClientsRemovedEvent> {
    private final JpaAuthorizationRepository authorizationRepository;
    private final JpaConsentRepository consentRepository;

    /**
     * Deletes all authorizations and consents associated with the given user.
     *
     * @param message event describing the user whose clients were removed
     */
    @Override
    @Transactional
    public void publish(UserClientsRemovedEvent message) {
      log.info("Directly cleaning up authorizations for user: {}", message.getUserId());
      try {
        authorizationRepository.deleteAllAuthorizationsByPrincipalId(message.getUserId());
        consentRepository.deleteAllConsentsByPrincipalId(message.getUserId());
        log.info("Authorizations and consents for user {} have been removed", message.getUserId());
      } catch (Exception e) {
        log.error(
            "Failed to cleanup authorizations for user {}: {}",
            message.getUserId(),
            e.getMessage(),
            e);
      }
    }
  }

  /**
   * Minified-specific publisher that cleans up tenant-wide authorizations and consents when all
   * clients for a tenant are removed.
   */
  @Primary
  @Component
  @Profile("minified")
  @RequiredArgsConstructor
  public static class TenantClientsRemovedPublisher
      implements AuthorizationMessagePublisher<TenantClientsRemovedEvent> {
    private final JpaAuthorizationRepository authorizationRepository;
    private final JpaConsentRepository consentRepository;

    /**
     * Deletes all consents and authorizations for the specified tenant.
     *
     * @param message event describing the tenant whose clients were removed
     */
    @Override
    @Transactional
    public void publish(TenantClientsRemovedEvent message) {
      log.info("Directly cleaning up authorizations for tenant: {}", message.getTenantId());
      try {
        consentRepository.deleteAllConsentsByTenantId(message.getTenantId());
        authorizationRepository.deleteAllAuthorizationsByTenantId(message.getTenantId());
        log.info(
            "Authorizations and consents for tenant {} have been removed", message.getTenantId());
      } catch (Exception e) {
        log.error(
            "Failed to cleanup authorizations for tenant {}: {}",
            message.getTenantId(),
            e.getMessage(),
            e);
      }
    }
  }
}
