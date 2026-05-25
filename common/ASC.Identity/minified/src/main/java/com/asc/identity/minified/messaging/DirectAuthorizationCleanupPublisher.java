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
@Profile("minified")
public class DirectAuthorizationCleanupPublisher {
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
