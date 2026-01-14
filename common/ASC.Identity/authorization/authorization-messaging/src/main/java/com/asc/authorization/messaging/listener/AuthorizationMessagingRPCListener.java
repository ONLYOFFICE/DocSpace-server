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
import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.authorization.data.key.repository.JpaKeyPairRepository;
import com.asc.authorization.messaging.configuration.KeyPairConfigurationProperties;
import com.asc.common.service.transfer.message.KeyPairRetrievedEvent;
import com.asc.common.service.transfer.message.RetrieveAuthorizationMessage;
import com.asc.common.service.transfer.message.RetrieveKeyPairMessage;
import com.asc.common.service.transfer.message.SaveAuthorizationMessage;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.Comparator;
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
  private final KeyPairConfigurationProperties keyPairConfigurationProperties;

  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final JpaKeyPairRepository jpaKeyPairRepository;

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
    log.info("Received retrieve authorization request {}", event.getToken());

    var response =
        jpaAuthorizationRepository
            .findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(
                event.getToken())
            .orElse(null);

    if (response != null) log.info("Found a valid non-null authorization for {}", event.getToken());

    return response;
  }

  /**
   * Handles authorization save RPC requests from remote regions.
   *
   * @param event the message containing the authorization data to save
   * @return {@code true} if the save was successful, {@code false} otherwise
   */
  @RabbitHandler
  public Boolean receiveAuthorizationSave(SaveAuthorizationMessage event) {
    try {
      log.info(
          "Received authorization save request for client: {}, principal: {}",
          event.getRegisteredClientId(),
          event.getPrincipalId());

      var result =
          jpaAuthorizationRepository
              .findByRegisteredClientIdAndPrincipalIdAndAuthorizationGrantType(
                  event.getRegisteredClientId(),
                  event.getPrincipalId(),
                  event.getAuthorizationGrantType());

      var toPersist =
          AuthorizationEntity.builder()
              .registeredClientId(event.getRegisteredClientId())
              .principalId(event.getPrincipalId())
              .authorizationGrantType(event.getAuthorizationGrantType())
              .id(event.getId())
              .state(event.getState())
              .attributes(event.getAttributes())
              .authorizedScopes(event.getAuthorizedScopes())
              .authorizationCodeValue(event.getAuthorizationCodeValue())
              .authorizationCodeMetadata(event.getAuthorizationCodeMetadata())
              .authorizationCodeIssuedAt(event.getAuthorizationCodeIssuedAt())
              .authorizationCodeExpiresAt(event.getAuthorizationCodeExpiresAt())
              .accessTokenValue(event.getAccessTokenValue())
              .accessTokenHash(event.getAccessTokenHash())
              .accessTokenMetadata(event.getAccessTokenMetadata())
              .accessTokenType(event.getAccessTokenType())
              .accessTokenScopes(event.getAccessTokenScopes())
              .accessTokenIssuedAt(event.getAccessTokenIssuedAt())
              .accessTokenExpiresAt(event.getAccessTokenExpiresAt())
              .refreshTokenValue(event.getRefreshTokenValue())
              .refreshTokenHash(event.getRefreshTokenHash())
              .refreshTokenMetadata(event.getRefreshTokenMetadata())
              .refreshTokenIssuedAt(event.getRefreshTokenIssuedAt())
              .refreshTokenExpiresAt(event.getRefreshTokenExpiresAt())
              .idTokenValue(event.getIdTokenValue())
              .idTokenClaims(event.getIdTokenClaims())
              .idTokenMetadata(event.getIdTokenMetadata())
              .idTokenIssuedAt(event.getIdTokenIssuedAt())
              .idTokenExpiresAt(event.getIdTokenExpiresAt())
              .build();

      if (result.isPresent()) {
        var entity = result.get();
        if (event.getTenantId() == null || event.getTenantId() < 1)
          toPersist.setTenantId(entity.getTenantId());
        else toPersist.setTenantId(event.getTenantId());
      } else if (event.getTenantId() != null) {
        toPersist.setTenantId(event.getTenantId());
      }

      jpaAuthorizationRepository.save(toPersist);
      log.info("Authorization saved successfully in remote region");
      return true;
    } catch (Exception e) {
      log.error("Failed to save authorization in remote region", e);
      return false;
    }
  }

  /**
   * Handles key pair retrieval RPC requests from remote regions.
   *
   * <p>Returns the latest active key pair for signing tokens in cross-region scenarios. The private
   * key is returned encrypted for security during transit.
   *
   * @param request the key pair request
   * @return the key pair response containing the signing key
   */
  @RabbitHandler
  public KeyPairRetrievedEvent receiveKeyPairRetrieval(RetrieveKeyPairMessage request) {
    log.debug("Received cross-region key pair request");

    try {
      var cutoffTime =
          ZonedDateTime.now(ZoneOffset.UTC)
              .minus(keyPairConfigurationProperties.getRotationPeriod())
              .minus(keyPairConfigurationProperties.getDeprecationPeriod());

      var latestKeyPair =
          jpaKeyPairRepository.findActiveKeyPairs(cutoffTime).stream()
              .max(Comparator.comparing(KeyPair::getCreatedAt))
              .orElse(null);

      if (latestKeyPair == null) {
        log.warn("No active key pair found");
        return KeyPairRetrievedEvent.builder().success(false).build();
      }

      log.debug("Returning key pair with id: {}", latestKeyPair.getId());

      return KeyPairRetrievedEvent.builder()
          .success(true)
          .id(latestKeyPair.getId())
          .publicKey(latestKeyPair.getPublicKey())
          .privateKey(latestKeyPair.getPrivateKey())
          .pairType(latestKeyPair.getPairType().name())
          .build();
    } catch (Exception e) {
      log.error("Error retrieving key pair for remote region", e);
      return KeyPairRetrievedEvent.builder().success(false).build();
    }
  }
}
