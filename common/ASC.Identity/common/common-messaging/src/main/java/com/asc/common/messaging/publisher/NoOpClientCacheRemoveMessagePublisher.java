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

package com.asc.common.messaging.publisher;

import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * No-op implementation of {@link AuthorizationMessagePublisher} for {@link ClientCacheRemoveEvent}.
 *
 * <p>This publisher is active in non-SaaS profiles where multi-region cache invalidation via
 * RabbitMQ is not required. It simply logs the event without attempting to publish to any message
 * broker, preventing errors when RabbitMQ infrastructure is not available.
 *
 * @see AuthorizationMessagePublisher
 * @see ClientCacheRemoveEvent
 */
@Slf4j
@Component
@Profile("!saas")
public class NoOpClientCacheRemoveMessagePublisher
    implements AuthorizationMessagePublisher<ClientCacheRemoveEvent> {

  /**
   * Logs the client cache removal event without publishing.
   *
   * <p>In non-SaaS deployments, cache invalidation is handled locally within the single instance,
   * so broadcasting to other regions is not necessary.
   *
   * @param message the {@link ClientCacheRemoveEvent} to log
   */
  @Override
  public void publish(ClientCacheRemoveEvent message) {
    log.debug("Skipping client cache removal event broadcast: clientId={}", message.getClientId());
  }
}
