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

package com.asc.authorization.application.security.oauth.provider;

import static com.asc.authorization.application.security.RegionUtils.JWT_REGION_EXTRACTOR;

import com.asc.authorization.application.security.RegionUtils;
import com.asc.common.messaging.configuration.ClientRegistrationMessagingConfiguration;
import com.asc.common.service.transfer.message.ClientRetrievedEvent;
import com.asc.common.service.transfer.message.RetrieveClientMessage;
import com.asc.common.utilities.crypto.EncryptionService;
import java.net.URL;
import java.util.*;
import lombok.extern.slf4j.Slf4j;
import org.jspecify.annotations.Nullable;
import org.slf4j.MDC;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.convert.TypeDescriptor;
import org.springframework.core.env.Environment;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.oauth2.core.*;
import org.springframework.security.oauth2.core.converter.ClaimConversionService;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenIntrospection;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2TokenIntrospectionAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Component;
import org.springframework.util.CollectionUtils;

/**
 * Authentication provider for OAuth2 token introspection requests.
 *
 * <p>This provider handles authentication of token introspection requests by validating the token
 * against stored authorizations and registered clients. It builds and returns introspection details
 * for active tokens, including claims and metadata.
 */
@Slf4j
@Component
public class TokenIntrospectionAuthenticationProvider implements AuthenticationProvider {
  private static final TypeDescriptor OBJECT_TYPE_DESCRIPTOR = TypeDescriptor.valueOf(Object.class);
  private static final TypeDescriptor LIST_STRING_TYPE_DESCRIPTOR =
      TypeDescriptor.collection(List.class, TypeDescriptor.valueOf(String.class));

  @Value("${spring.application.region}")
  private String region;

  private final Environment environment;
  @Nullable private final RabbitTemplate rpcRabbitTemplate;
  @Nullable private final MessageConverter messageConverter;

  private final EncryptionService encryptionService;
  private final OAuth2AuthorizationService authorizationService;
  private final RegisteredClientRepository registeredClientRepository;

  @Autowired
  public TokenIntrospectionAuthenticationProvider(
      Environment environment,
      @Autowired(required = false) @Qualifier("rpcRabbitTemplate") RabbitTemplate rpcRabbitTemplate,
      @Autowired(required = false) MessageConverter messageConverter,
      EncryptionService encryptionService,
      OAuth2AuthorizationService authorizationService,
      RegisteredClientRepository registeredClientRepository) {
    this.environment = environment;
    this.rpcRabbitTemplate = rpcRabbitTemplate;
    this.messageConverter = messageConverter;
    this.encryptionService = encryptionService;
    this.authorizationService = authorizationService;
    this.registeredClientRepository = registeredClientRepository;
  }

  /**
   * Maps a {@link ClientRetrievedEvent} to a {@link RegisteredClient}.
   *
   * <p>Decrypts the client secret which is transmitted in encrypted form over RabbitMQ.
   *
   * @param clientEvent the client event containing client information from the remote region
   * @return the mapped {@link RegisteredClient}
   */
  private RegisteredClient toRegisteredClient(ClientRetrievedEvent clientEvent) {
    return RegisteredClient.withId(clientEvent.getClientId())
        .clientId(clientEvent.getClientId())
        .clientSecret(encryptionService.decrypt(clientEvent.getClientSecret()))
        .clientName(clientEvent.getName())
        .clientAuthenticationMethods(
            authMethods ->
                clientEvent.getAuthenticationMethods().stream()
                    .map(
                        method ->
                            switch (method) {
                              case "client_secret_post" ->
                                  ClientAuthenticationMethod.CLIENT_SECRET_POST;
                              case "none" -> ClientAuthenticationMethod.NONE;
                              default -> ClientAuthenticationMethod.CLIENT_SECRET_BASIC;
                            })
                    .forEach(authMethods::add))
        .authorizationGrantTypes(
            grantTypes -> {
              grantTypes.add(AuthorizationGrantType.AUTHORIZATION_CODE);
              grantTypes.add(AuthorizationGrantType.REFRESH_TOKEN);
            })
        .redirectUris(uris -> uris.addAll(clientEvent.getRedirectUris()))
        .scopes(scopes -> scopes.addAll(clientEvent.getScopes()))
        .build();
  }

  /**
   * Fetches a registered client from a remote region via RPC messaging.
   *
   * <p>This method sends an RPC request to the specified remote region to retrieve the client
   * information. It's used during token introspection when a token from a different region needs to
   * be validated.
   *
   * @param clientId the ID of the client to retrieve
   * @param targetRegion the region to fetch the client from
   * @return an {@link Optional} containing the {@link RegisteredClient} if found, or empty if the
   *     remote region returns no result or the request fails
   */
  private Optional<RegisteredClient> fetchRemoteClient(String clientId, String targetRegion) {
    if (rpcRabbitTemplate == null || messageConverter == null) {
      log.warn("RabbitMQ not available, cannot fetch client from remote region: {}", targetRegion);
      return Optional.empty();
    }

    try {
      MDC.put("client_id", clientId);
      MDC.put("region", targetRegion);
      log.info("Retrieving client from remote region");

      var routingKey =
          ClientRegistrationMessagingConfiguration.CLIENT_RPC_ROUTING_KEY_PREFIX + targetRegion;
      var message =
          messageConverter.toMessage(
              RetrieveClientMessage.builder().clientId(clientId).build(), new MessageProperties());

      var response =
          rpcRabbitTemplate.sendAndReceive(
              ClientRegistrationMessagingConfiguration.CLIENT_RPC_EXCHANGE, routingKey, message);

      if (response == null) {
        log.warn("Received an empty response from remote region");
        return Optional.empty();
      }

      var data = messageConverter.fromMessage(response);
      if (data instanceof ClientRetrievedEvent clientEvent) {
        var registeredClient = toRegisteredClient(clientEvent);
        return Optional.of(registeredClient);
      }

      return Optional.empty();
    } catch (Exception e) {
      log.error("Error fetching client from remote region: {}", targetRegion, e);
      return Optional.empty();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Authenticates the provided token introspection request.
   *
   * <p>This method validates the token against stored authorizations and builds an introspection
   * response for active tokens. If the token is not found or inactive, a default inactive response
   * is returned.
   *
   * @param authentication the {@link Authentication} request object, specifically an instance of
   *     {@link OAuth2TokenIntrospectionAuthenticationToken}.
   * @return a fully authenticated {@link OAuth2TokenIntrospectionAuthenticationToken} containing
   *     token claims or the original request if authentication fails.
   * @throws AuthenticationException if an authentication error occurs.
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    var tokenIntrospectionAuthentication =
        (OAuth2TokenIntrospectionAuthenticationToken) authentication;

    var token = tokenIntrospectionAuthentication.getToken();
    // TODO: Refactor for SaaS config
    var isSaaS =
        Arrays.stream(environment.getActiveProfiles())
            .anyMatch(profile -> profile.equalsIgnoreCase("saas"));

    String tokenRgn = null;
    if (isSaaS) {
      var tokenRegion = RegionUtils.extract(token, JWT_REGION_EXTRACTOR);
      if (tokenRegion.isPresent() && !tokenRegion.get().equalsIgnoreCase(region)) {
        tokenRgn = tokenRegion.get();
        log.debug(
            "Token region '{}' does not match current region '{}'. Will attempt cross-region client lookup.",
            tokenRgn,
            region);
      }
    }

    var authorization = authorizationService.findByToken(token, null);
    if (authorization == null) {
      log.debug("Did not authenticate token introspection request since token was not found");
      // Return the authentication request when token not found
      return tokenIntrospectionAuthentication;
    }

    log.trace("Retrieved authorization with token");

    var authorizedToken = authorization.getToken(token);
    if (authorizedToken == null || !authorizedToken.isActive()) {
      log.trace("Did not introspect token since not active");
      return new OAuth2TokenIntrospectionAuthenticationToken(
          token, authentication, OAuth2TokenIntrospection.builder().build());
    }

    RegisteredClient authorizedClient;
    if (isSaaS && tokenRgn != null && !tokenRgn.equalsIgnoreCase(region)) {
      log.debug(
          "Fetching client {} from remote region: {}",
          authorization.getRegisteredClientId(),
          tokenRgn);
      var remoteClient = fetchRemoteClient(authorization.getRegisteredClientId(), tokenRgn);
      if (remoteClient.isPresent()) {
        authorizedClient = remoteClient.get();
        log.debug("Successfully retrieved client from remote region: {}", tokenRgn);
      } else {
        log.warn(
            "Failed to retrieve client {} from remote region {}. Token introspection failed.",
            authorization.getRegisteredClientId(),
            tokenRgn);
        return new OAuth2TokenIntrospectionAuthenticationToken(
            token, authentication, OAuth2TokenIntrospection.builder().build());
      }
    } else {
      authorizedClient = registeredClientRepository.findById(authorization.getRegisteredClientId());
    }

    var tokenClaims = withActiveTokenClaims(authorizedToken, authorizedClient);

    log.trace("Authenticated token introspection request");

    return new OAuth2TokenIntrospectionAuthenticationToken(
        authorizedToken.getToken().getTokenValue(), authentication, tokenClaims);
  }

  /**
   * Indicates whether this {@code AuthenticationProvider} supports the specified authentication
   * type.
   *
   * @param authentication the class of the authentication request object.
   * @return {@code true} if the authentication type is supported, otherwise {@code false}.
   */
  public boolean supports(Class<?> authentication) {
    return OAuth2TokenIntrospectionAuthenticationToken.class.isAssignableFrom(authentication);
  }

  /**
   * Constructs an {@link OAuth2TokenIntrospection} object with active token claims.
   *
   * @param authorizedToken the authorized token containing claims and metadata.
   * @param authorizedClient the associated registered client.
   * @return the constructed {@link OAuth2TokenIntrospection} object.
   */
  private static OAuth2TokenIntrospection withActiveTokenClaims(
      OAuth2Authorization.Token<OAuth2Token> authorizedToken, RegisteredClient authorizedClient) {

    OAuth2TokenIntrospection.Builder tokenClaims;
    if (!CollectionUtils.isEmpty(authorizedToken.getClaims())) {
      Map<String, Object> claims = convertClaimsIfNecessary(authorizedToken.getClaims());
      tokenClaims = OAuth2TokenIntrospection.withClaims(claims).active(true);
    } else {
      tokenClaims = OAuth2TokenIntrospection.builder(true);
    }

    tokenClaims.clientId(authorizedClient.getClientId());

    var token = authorizedToken.getToken();
    if (token.getIssuedAt() != null) tokenClaims.issuedAt(token.getIssuedAt());
    if (token.getExpiresAt() != null) tokenClaims.expiresAt(token.getExpiresAt());

    if (OAuth2AccessToken.class.isAssignableFrom(token.getClass())) {
      var accessToken = (OAuth2AccessToken) token;
      tokenClaims.tokenType(accessToken.getTokenType().getValue());
    }

    return tokenClaims.build();
  }

  /**
   * Converts token claims to the required types for introspection.
   *
   * <p>Claims such as {@code iss}, {@code scope}, and {@code aud} are converted to expected types
   * (e.g., {@link URL} or {@link List}).
   *
   * @param claims the original token claims.
   * @return the converted claims.
   */
  private static Map<String, Object> convertClaimsIfNecessary(Map<String, Object> claims) {
    var convertedClaims = new HashMap<String, Object>(claims);

    var value = claims.get(OAuth2TokenIntrospectionClaimNames.ISS);
    if (value != null && !(value instanceof URL)) {
      var convertedValue = ClaimConversionService.getSharedInstance().convert(value, URL.class);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.ISS, convertedValue);
    }

    value = claims.get(OAuth2TokenIntrospectionClaimNames.SCOPE);
    if (value != null && !(value instanceof List)) {
      var convertedValue =
          ClaimConversionService.getSharedInstance()
              .convert(value, OBJECT_TYPE_DESCRIPTOR, LIST_STRING_TYPE_DESCRIPTOR);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.SCOPE, convertedValue);
    }

    value = claims.get(OAuth2TokenIntrospectionClaimNames.AUD);
    if (value != null && !(value instanceof List)) {
      var convertedValue =
          ClaimConversionService.getSharedInstance()
              .convert(value, OBJECT_TYPE_DESCRIPTOR, LIST_STRING_TYPE_DESCRIPTOR);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.AUD, convertedValue);
    }

    return convertedClaims;
  }
}
