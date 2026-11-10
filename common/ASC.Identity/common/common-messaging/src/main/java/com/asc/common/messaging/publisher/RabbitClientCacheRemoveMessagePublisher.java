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

import com.asc.common.service.transfer.message.ClientCacheRemoveEvent;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * Publishes single client client cache evictions ({@link ClientCacheRemoveEvent}) to every region,
 * and at once to the authorization service of the local region; see {@link
 * ClientCacheEvictionPublisher}.
 *
 * <p>Only active in the SaaS profile, where multi-region cache invalidation is required, and only
 * loaded when RabbitMQ classes are available on the classpath.
 *
 * @see ClientCacheEvictionPublisher
 * @see ClientCacheRemoveEvent
 */
@Component
@Profile("saas")
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class RabbitClientCacheRemoveMessagePublisher
    extends ClientCacheEvictionPublisher<ClientCacheRemoveEvent> {
  public RabbitClientCacheRemoveMessagePublisher(
      AmqpTemplate amqpClient,
      @Value("${spring.application.region}") String region,
      RabbitBackgroundPublisher background) {
    super(amqpClient, region, background);
  }
}
