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
