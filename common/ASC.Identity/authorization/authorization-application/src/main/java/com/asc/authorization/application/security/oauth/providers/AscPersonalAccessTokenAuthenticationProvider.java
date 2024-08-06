// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.application.security.oauth.providers;

import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.security.oauth.AscAuthorizationGrantType;
import com.asc.authorization.application.security.oauth.authentications.PersonalAccessTokenAuthenticationToken;
import com.asc.authorization.application.security.oauth.authorities.TenantAuthority;
import com.asc.authorization.application.security.oauth.errors.AuthenticationError;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.common.utilities.HttpUtils;
import java.util.List;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.oauth2.core.*;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AccessTokenAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.context.AuthorizationServerContextHolder;
import org.springframework.security.oauth2.server.authorization.settings.OAuth2TokenFormat;
import org.springframework.security.oauth2.server.authorization.token.DefaultOAuth2TokenContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenGenerator;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/** This class provides authentication for personal access tokens. */
@Slf4j
@RequiredArgsConstructor
public class AscPersonalAccessTokenAuthenticationProvider implements AuthenticationProvider {
  private static final String ERROR_URI =
      "https://datatracker.ietf.org/doc/html/rfc6749#section-5.2";

  @Value("${spring.application.name}")
  private String serviceName;

  private final AuditMessagePublisher auditMessagePublisher;
  private final OAuth2AuthorizationService authorizationService;
  private final OAuth2TokenGenerator<? extends OAuth2Token> tokenGenerator;

  /**
   * Authenticates the provided authentication token.
   *
   * @param authentication The authentication request object.
   * @return An authenticated token if authentication is successful.
   * @throws AuthenticationException if authentication fails.
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    log.debug("Authenticating with token: {}", authentication);
    if (authentication instanceof PersonalAccessTokenAuthenticationToken patAuthentication) {
      var ctx = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
      if (ctx == null)
        throw new BadCredentialsException("Authentication failed due to missing request context");
      var request = ctx.getRequest();

      var clientPrincipal = getAuthenticatedClientElseThrowInvalidClient(patAuthentication);
      var registeredClient = clientPrincipal.getRegisteredClient();
      if (registeredClient == null
          || !registeredClient
              .getAuthorizationGrantTypes()
              .contains(AscAuthorizationGrantType.PERSONAL_ACCESS_TOKEN))
        throw new OAuth2AuthenticationException(OAuth2ErrorCodes.UNAUTHORIZED_CLIENT);

      if (!registeredClient.getScopes().containsAll(patAuthentication.getScopes()))
        throw new OAuth2AuthenticationException(OAuth2ErrorCodes.INVALID_SCOPE);

      var tokenContext =
          DefaultOAuth2TokenContext.builder()
              .registeredClient(registeredClient)
              .principal(
                  new UsernamePasswordAuthenticationToken(
                      patAuthentication.getUserId(),
                      null,
                      List.of(
                          new TenantAuthority(
                              patAuthentication.getTenantId(), patAuthentication.getTenantUrl()))))
              .authorizationServerContext(AuthorizationServerContextHolder.getContext())
              .authorizedScopes(patAuthentication.getScopes())
              .tokenType(OAuth2TokenType.ACCESS_TOKEN)
              .authorizationGrantType(AscAuthorizationGrantType.PERSONAL_ACCESS_TOKEN)
              .authorizationGrant(patAuthentication)
              .build();

      var generatedAccessToken = tokenGenerator.generate(tokenContext);
      if (generatedAccessToken == null) {
        var error =
            new OAuth2Error(
                OAuth2ErrorCodes.SERVER_ERROR,
                "The token generator failed to generate the access token.",
                ERROR_URI);
        throw new OAuth2AuthenticationException(error);
      }

      var authorizationBuilder =
          OAuth2Authorization.withRegisteredClient(registeredClient)
              .principalName(patAuthentication.getUserId())
              .authorizationGrantType(AscAuthorizationGrantType.PERSONAL_ACCESS_TOKEN)
              .authorizedScopes(patAuthentication.getScopes());

      var accessToken = accessToken(authorizationBuilder, generatedAccessToken, tokenContext);
      authorizationService.save(authorizationBuilder.build());

      log.debug("Authentication successful for user: {}", patAuthentication.getUserId());

      auditMessagePublisher.publish(
          AuditMessage.builder()
              .ip(
                  HttpUtils.getRequestClientAddress(request)
                      .map(HttpUtils::extractHostFromUrl)
                      .orElseGet(
                          () -> HttpUtils.extractHostFromUrl(HttpUtils.getFirstRequestIP(request))))
              .initiator(serviceName)
              .target(registeredClient.getClientId())
              .browser(HttpUtils.getClientBrowser(request))
              .platform(HttpUtils.getClientOS(request))
              .tenantId(patAuthentication.getTenantId())
              .userId(patAuthentication.getUserId())
              .userEmail(patAuthentication.getUserEmail())
              .userName(patAuthentication.getUserName())
              .page(HttpUtils.getFullURL(request))
              .action(AuditCode.GENERATE_PERSONAL_ACCESS_TOKEN.getCode())
              .build());

      return new OAuth2AccessTokenAuthenticationToken(
          registeredClient, clientPrincipal, accessToken, null);
    }

    log.error("Authentication type not supported: {}", authentication.getClass());
    throw new AuthenticationProcessingException(
        AuthenticationError.AUTHENTICATION_NOT_SUPPORTED_ERROR,
        "Authentication type is not supported");
  }

  /**
   * Checks if this provider supports the given authentication type.
   *
   * @param authentication The authentication class to check.
   * @return True if the authentication type is supported, false otherwise.
   */
  public boolean supports(Class<?> authentication) {
    return PersonalAccessTokenAuthenticationToken.class.equals(authentication);
  }

  private static OAuth2ClientAuthenticationToken getAuthenticatedClientElseThrowInvalidClient(
      Authentication authentication) {
    OAuth2ClientAuthenticationToken clientPrincipal = null;
    if (OAuth2ClientAuthenticationToken.class.isAssignableFrom(
        authentication.getPrincipal().getClass()))
      clientPrincipal = (OAuth2ClientAuthenticationToken) authentication.getPrincipal();
    if (clientPrincipal != null && clientPrincipal.isAuthenticated()) return clientPrincipal;
    throw new OAuth2AuthenticationException(OAuth2ErrorCodes.INVALID_CLIENT);
  }

  private static <T extends OAuth2Token> OAuth2AccessToken accessToken(
      OAuth2Authorization.Builder builder, T token, OAuth2TokenContext accessTokenContext) {
    var accessToken =
        new OAuth2AccessToken(
            OAuth2AccessToken.TokenType.BEARER,
            token.getTokenValue(),
            token.getIssuedAt(),
            token.getExpiresAt(),
            accessTokenContext.getAuthorizedScopes());
    var accessTokenFormat =
        accessTokenContext.getRegisteredClient().getTokenSettings().getAccessTokenFormat();
    builder.token(
        accessToken,
        (metadata) -> {
          if (token instanceof ClaimAccessor claimAccessor)
            metadata.put(OAuth2Authorization.Token.CLAIMS_METADATA_NAME, claimAccessor.getClaims());
          metadata.put(OAuth2Authorization.Token.INVALIDATED_METADATA_NAME, false);
          metadata.put(OAuth2TokenFormat.class.getName(), accessTokenFormat.getValue());
        });

    return accessToken;
  }
}
