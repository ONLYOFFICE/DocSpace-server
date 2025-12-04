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

package com.asc.authorization.messaging.listener;

import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.common.service.transfer.message.RetrieveAuthorizationMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * RabbitMQ RPC listener for handling authorization retrieval requests.
 *
 * <p>This component listens on a region-specific RPC queue and processes incoming requests to
 * retrieve authorization entities by token. It is only active when the "saas" profile is enabled,
 * allowing cross-instance authorization lookups in a clustered environment.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@Profile("saas")
@RabbitListener(
    queues = "asc_identity_authorization_rpc_${spring.application.region}_queue",
    containerFactory = "rabbitRpcContainerFactory")
public class AuthorizationMessagingRPCListener {
  private final JpaAuthorizationRepository jpaAuthorizationRepository;

  /**
   * Handles authorization retrieval RPC requests.
   *
   * <p>Searches for an authorization entity matching the provided token across multiple token
   * types: state, authorization code, access token, or refresh token.
   *
   * @param event the message containing the token to search for
   * @return the matching {@link AuthorizationEntity}, or {@code null} if not found
   */
  @RabbitHandler
  public AuthorizationEntity receiveAuthorizationRetrieval(RetrieveAuthorizationMessage event) {
    return jpaAuthorizationRepository
        .findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(event.getToken())
        .orElse(null);
  }
}
