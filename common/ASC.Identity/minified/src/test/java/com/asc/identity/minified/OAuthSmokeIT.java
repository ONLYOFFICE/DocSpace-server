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

package com.asc.identity.minified;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.common.utilities.crypto.MachinePseudoKeys;
import com.asc.registration.application.transfer.CreateClientRequest;
import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import java.net.URI;
import java.net.http.HttpClient;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Base64;
import java.util.Date;
import java.util.List;
import java.util.Set;
import java.util.regex.Pattern;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestInstance;
import org.junit.jupiter.api.TestMethodOrder;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestClient;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.junit.jupiter.Testcontainers;

/**
 * Smoke test of the main OAuth scenarios, run against the minified build (registration and
 * authorization in one process, MySQL only).
 *
 * <p>It walks the paths a third-party app takes, in order: discovery and JWKS, client registration,
 * the authorization code flow of a confidential client (token, introspect, refresh, revoke), the
 * same flow of a public client with PKCE and no secret, the consent list, and client deletion. It
 * fills the gaps of {@link IdentityMinifiedServiceIT}, which does not cover discovery, JWKS, PKCE,
 * revocation or an active introspection.
 *
 * <p>Run it with {@code make smoke-tests}, or: {@code mvn install -Pminified -pl minified -am
 * -DskipTests -Dfmt.skip=true}, then {@code cd minified && mvn test -Dfmt.skip=true
 * -DRUN_INTEGRATION_TESTS=true -Dtest=OAuthSmokeIT}. Needs Docker for the MySQL container.
 */
@Testcontainers
@ActiveProfiles({"test", "minified"})
@TestInstance(TestInstance.Lifecycle.PER_CLASS)
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
@EnabledIfSystemProperty(named = "RUN_INTEGRATION_TESTS", matches = "true")
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
public class OAuthSmokeIT {
  private static final String WEB_API = "/api/2.0";
  private static final String SCOPE = "files:read";
  private static final String SIGNATURE_SECRET = "1123askdasjklasbnd";
  private static final String REDIRECT_URI = "https://smoke.example.com/callback";
  private static final String USER_ID = "7a1d2c3b-0000-4000-8000-00000000a001";
  private static final String TENANT_URL = "http://localhost:8092";

  static MySQLContainer<?> mysql = new MySQLContainer<>("mysql:8.0").withInitScript("init.sql");

  static {
    mysql.start();
  }

  @DynamicPropertySource
  static void configureProperties(DynamicPropertyRegistry registry) {
    registry.add("spring.datasource.url", mysql::getJdbcUrl);
    registry.add("spring.datasource.username", mysql::getUsername);
    registry.add("spring.datasource.password", mysql::getPassword);
    registry.add("spring.datasource.driver-class-name", () -> "com.mysql.cj.jdbc.Driver");
  }

  @LocalServerPort private int serverPort;

  private final byte[] signingKey = new MachinePseudoKeys(SIGNATURE_SECRET).getMachineConstant(256);

  private String confidentialClientId;
  private String confidentialClientSecret;
  private String confidentialAccessToken;
  private String confidentialRefreshToken;
  private String publicClientId;
  private String publicRefreshToken;

  @Test
  @Order(1)
  void givenRunningService_whenGetHealth_thenReturnUp() {
    var response = http().get().uri("/health").retrieve().toEntity(String.class);

    assertThat(response.getStatusCode().value()).isEqualTo(200);
    assertThat(response.getBody()).contains("UP");
  }

  @Test
  @Order(2)
  void givenNoSignature_whenGetDiscoveryDocuments_thenListEndpoints() {
    for (var path :
        List.of("/.well-known/oauth-authorization-server", "/.well-known/openid-configuration")) {
      var response = http().get().uri(path).retrieve().toEntity(String.class);

      assertThat(response.getStatusCode().value()).as(path).isEqualTo(200);
      assertThat(response.getBody())
          .as(path)
          .contains("\"issuer\"")
          .contains("/oauth2/authorize")
          .contains("/oauth2/token")
          .contains("/oauth2/jwks");
    }
  }

  @Test
  @Order(3)
  void givenNoSignature_whenGetJwks_thenReturnSigningKey() {
    var response = http().get().uri("/oauth2/jwks").retrieve().toEntity(String.class);

    assertThat(response.getStatusCode().value()).isEqualTo(200);
    assertThat(response.getBody()).contains("\"keys\"").contains("\"kid\"");
  }

  @Test
  @Order(4)
  void givenValidSignature_whenGetScopes_thenReturnScopes() {
    var response =
        http()
            .get()
            .uri(WEB_API + "/scopes")
            .header(HttpHeaders.COOKIE, signatureCookie())
            .retrieve()
            .toEntity(String.class);

    assertThat(response.getStatusCode().value()).isEqualTo(200);
    assertThat(response.getBody()).contains("openid").contains(SCOPE);
  }

  @Test
  @Order(10)
  void givenValidRequest_whenCreateConfidentialClient_thenPublicInfoHidesSecret() {
    var response = createClient("Smoke confidential client", false);

    confidentialClientId = jsonValue(response.getBody(), "client_id");
    confidentialClientSecret = jsonValue(response.getBody(), "client_secret");
    assertThat(confidentialClientId).isNotBlank();
    assertThat(confidentialClientSecret).isNotBlank();

    var publicInfo =
        http()
            .get()
            .uri(WEB_API + "/clients/" + confidentialClientId + "/public/info")
            .retrieve()
            .toEntity(String.class);
    assertThat(publicInfo.getStatusCode().value()).isEqualTo(200);
    assertThat(publicInfo.getBody())
        .contains(confidentialClientId)
        .doesNotContain(confidentialClientSecret);
  }

  @Test
  @Order(11)
  void givenConsentedCode_whenExchangeWithSecret_thenReturnTokens() {
    var code = authorizeAndConsent(confidentialClientId, null);

    var form = form("grant_type", "authorization_code");
    form.add("code", code);
    form.add("redirect_uri", REDIRECT_URI);
    form.add("client_id", confidentialClientId);
    form.add("client_secret", confidentialClientSecret);
    var response = post("/oauth2/token", form);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(200);
    confidentialAccessToken = jsonValue(response.getBody(), "access_token");
    confidentialRefreshToken = jsonValue(response.getBody(), "refresh_token");
    assertThat(confidentialAccessToken).isNotBlank();
    assertThat(confidentialRefreshToken).isNotBlank();
  }

  @Test
  @Order(12)
  void givenIssuedAccessToken_whenIntrospect_thenActive() {
    var response = introspect(confidentialAccessToken);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(200);
    assertThat(response.getBody()).contains("\"active\":true").contains(SCOPE);
  }

  @Test
  @Order(13)
  void givenRefreshToken_whenRefresh_thenRotateAndRejectReuse() {
    var form = form("grant_type", "refresh_token");
    form.add("refresh_token", confidentialRefreshToken);
    form.add("client_id", confidentialClientId);
    form.add("client_secret", confidentialClientSecret);
    var response = post("/oauth2/token", form);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(200);
    var rotated = jsonValue(response.getBody(), "refresh_token");
    assertThat(rotated).isNotBlank().isNotEqualTo(confidentialRefreshToken);

    var reuse = form("grant_type", "refresh_token");
    reuse.add("refresh_token", confidentialRefreshToken);
    reuse.add("client_id", confidentialClientId);
    reuse.add("client_secret", confidentialClientSecret);
    assertThat(post("/oauth2/token", reuse).getStatusCode().is4xxClientError())
        .as("a rotated refresh token must not work again")
        .isTrue();

    confidentialAccessToken = jsonValue(response.getBody(), "access_token");
    confidentialRefreshToken = rotated;
  }

  @Test
  @Order(14)
  void givenRevokedAccessToken_whenIntrospect_thenInactive() {
    var form = form("token", confidentialAccessToken);
    form.add("client_id", confidentialClientId);
    form.add("client_secret", confidentialClientSecret);
    var revoke = post("/oauth2/revoke", form);

    assertThat(revoke.getStatusCode().value()).as(revoke.getBody()).isEqualTo(200);
    assertThat(introspect(confidentialAccessToken).getBody()).contains("\"active\":false");
  }

  @Test
  @Order(20)
  void givenPkceRequest_whenCreateClient_thenReturnCreatedClient() {
    var response = createClient("Smoke PKCE client", true);

    publicClientId = jsonValue(response.getBody(), "client_id");
    assertThat(publicClientId).isNotBlank();
  }

  @Test
  @Order(21)
  void givenPkceCode_whenExchangeWithVerifier_thenReturnTokens() throws Exception {
    var verifier = codeVerifier();
    var code = authorizeAndConsent(publicClientId, codeChallenge(verifier));

    var wrong = form("grant_type", "authorization_code");
    wrong.add("code", code);
    wrong.add("redirect_uri", REDIRECT_URI);
    wrong.add("client_id", publicClientId);
    wrong.add("code_verifier", codeVerifier());
    assertThat(post("/oauth2/token", wrong).getStatusCode().is4xxClientError())
        .as("a wrong code_verifier must be rejected")
        .isTrue();

    var freshCode = authorizeAndConsent(publicClientId, codeChallenge(verifier));
    var form = form("grant_type", "authorization_code");
    form.add("code", freshCode);
    form.add("redirect_uri", REDIRECT_URI);
    form.add("client_id", publicClientId);
    form.add("code_verifier", verifier);
    var response = post("/oauth2/token", form);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(200);
    assertThat(jsonValue(response.getBody(), "access_token")).isNotBlank();
    publicRefreshToken = jsonValue(response.getBody(), "refresh_token");
    assertThat(publicRefreshToken).isNotBlank();
  }

  @Test
  @Order(22)
  void givenPublicClientRefreshToken_whenRefreshWithoutSecret_thenReturnTokens() {
    var form = form("grant_type", "refresh_token");
    form.add("refresh_token", publicRefreshToken);
    form.add("client_id", publicClientId);
    var response = post("/oauth2/token", form);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(200);
    publicRefreshToken = jsonValue(response.getBody(), "refresh_token");
    assertThat(publicRefreshToken).isNotBlank();
  }

  @Test
  @Order(23)
  void givenPublicClientRefreshToken_whenRevokeWithoutSecret_thenRefreshFails() {
    var revokeForm = form("token", publicRefreshToken);
    revokeForm.add("token_type_hint", "refresh_token");
    revokeForm.add("client_id", publicClientId);
    var revoke = post("/oauth2/revoke", revokeForm);
    assertThat(revoke.getStatusCode().value()).as(revoke.getBody()).isEqualTo(200);

    var refresh = form("grant_type", "refresh_token");
    refresh.add("refresh_token", publicRefreshToken);
    refresh.add("client_id", publicClientId);
    assertThat(post("/oauth2/token", refresh).getStatusCode().is4xxClientError())
        .as("a revoked refresh token must not work")
        .isTrue();
  }

  @Test
  @Order(30)
  void givenConsentedClients_whenGetConsents_thenListBoth() {
    var response =
        http()
            .get()
            .uri(WEB_API + "/clients/consents?limit=50")
            .header(HttpHeaders.COOKIE, signatureCookie())
            .retrieve()
            .toEntity(String.class);

    assertThat(response.getStatusCode().value()).isEqualTo(200);
    assertThat(response.getBody()).contains(confidentialClientId).contains(publicClientId);
  }

  @Test
  @Order(40)
  void givenDeletedClient_whenRefresh_thenFail() {
    for (var clientId : List.of(confidentialClientId, publicClientId)) {
      var delete =
          http()
              .delete()
              .uri(WEB_API + "/clients/" + clientId)
              .header(HttpHeaders.COOKIE, signatureCookie())
              .retrieve()
              .toEntity(String.class);
      assertThat(delete.getStatusCode().is2xxSuccessful()).as(clientId).isTrue();
    }

    var refresh = form("grant_type", "refresh_token");
    refresh.add("refresh_token", confidentialRefreshToken);
    refresh.add("client_id", confidentialClientId);
    refresh.add("client_secret", confidentialClientSecret);
    assertThat(post("/oauth2/token", refresh).getStatusCode().is4xxClientError())
        .as("the refresh token of a deleted client must not work")
        .isTrue();
  }

  private ResponseEntity<String> createClient(String name, boolean pkce) {
    var request =
        CreateClientRequest.builder()
            .name(name)
            .description("Created by OAuthSmokeIT")
            .logo("data:image/png;base64,ivBORw0KGgo=")
            .allowPkce(pkce)
            .isPublic(false)
            .websiteUrl("https://smoke.example.com")
            .termsUrl("https://smoke.example.com/terms")
            .policyUrl("https://smoke.example.com/policy")
            .redirectUris(Set.of(REDIRECT_URI))
            .allowedOrigins(Set.of("https://smoke.example.com"))
            .logoutRedirectUri("https://smoke.example.com/logout")
            .scopes(Set.of(SCOPE, "openid"))
            .build();

    var response =
        http()
            .post()
            .uri(WEB_API + "/clients")
            .header(HttpHeaders.COOKIE, signatureCookie())
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);

    assertThat(response.getStatusCode().value()).as(response.getBody()).isEqualTo(201);
    return response;
  }

  /** Starts the authorization request, approves the consent and returns the issued code. */
  private String authorizeAndConsent(String clientId, String codeChallenge) {
    var authorize =
        http()
            .get()
            .uri(
                builder -> {
                  builder
                      .path("/oauth2/authorize")
                      .queryParam("response_type", "code")
                      .queryParam("client_id", clientId)
                      .queryParam("redirect_uri", REDIRECT_URI)
                      .queryParam("scope", SCOPE);
                  if (codeChallenge != null)
                    builder
                        .queryParam("code_challenge", codeChallenge)
                        .queryParam("code_challenge_method", "S256");
                  return builder.build();
                })
            .header(HttpHeaders.COOKIE, signatureCookie())
            .retrieve()
            .toEntity(String.class);

    // Once the user has consented to these scopes, the server skips the consent and redirects
    // straight back to the app with the code.
    var direct = codeFrom(authorize.getHeaders().getLocation());
    if (direct != null) return direct;

    var state = cookieValue(authorize.getHeaders().get(HttpHeaders.SET_COOKIE), "client_state");
    assertThat(state)
        .as(
            "client_state cookie of the authorization request: HTTP %s, Location %s",
            authorize.getStatusCode(), authorize.getHeaders().getLocation())
        .isNotBlank();

    var consent = form("client_id", clientId);
    consent.add("state", state);
    consent.add("scope", SCOPE);
    var response =
        http()
            .post()
            .uri("/oauth2/authorize")
            .header(HttpHeaders.COOKIE, signatureCookie())
            .contentType(MediaType.APPLICATION_FORM_URLENCODED)
            .body(consent)
            .retrieve()
            .toEntity(String.class);

    var location = response.getHeaders().getLocation();
    var code = codeFrom(location);
    assertThat(code).as("code in the redirect after the consent: %s", location).isNotBlank();
    return code;
  }

  private static String codeFrom(URI location) {
    if (location == null) return null;
    var matcher = Pattern.compile("[?&]code=([^&]+)").matcher(location.toString());
    return matcher.find() ? matcher.group(1) : null;
  }

  private ResponseEntity<String> introspect(String token) {
    var form = form("token", token);
    form.add("client_id", confidentialClientId);
    form.add("client_secret", confidentialClientSecret);
    return post("/oauth2/introspect", form);
  }

  private ResponseEntity<String> post(String path, MultiValueMap<String, String> form) {
    return http()
        .post()
        .uri(path)
        .header(HttpHeaders.COOKIE, signatureCookie())
        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
        .body(form)
        .retrieve()
        .toEntity(String.class);
  }

  private RestClient http() {
    // Redirects are not followed: the authorization request answers with a 302 that carries the
    // client_state cookie, and following it to the consent page would drop that cookie.
    return RestClient.builder()
        .requestFactory(
            new JdkClientHttpRequestFactory(
                HttpClient.newBuilder().followRedirects(HttpClient.Redirect.NEVER).build()))
        .baseUrl("http://localhost:" + serverPort)
        .defaultStatusHandler(status -> true, (request, response) -> {})
        .build();
  }

  private static MultiValueMap<String, String> form(String key, String value) {
    var form = new LinkedMultiValueMap<String, String>();
    form.add(key, value);
    return form;
  }

  private static String jsonValue(String json, String key) {
    if (json == null) return null;
    var matcher = Pattern.compile("\"" + key + "\"\\s*:\\s*\"([^\"]+)\"").matcher(json);
    return matcher.find() ? matcher.group(1) : null;
  }

  private static String cookieValue(List<String> setCookies, String name) {
    if (setCookies == null) return null;
    return setCookies.stream()
        .filter(cookie -> cookie.startsWith(name + "="))
        .map(cookie -> cookie.substring(name.length() + 1).split(";", 2)[0])
        .findFirst()
        .orElse(null);
  }

  private static String codeVerifier() {
    var bytes = new byte[32];
    new SecureRandom().nextBytes(bytes);
    return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
  }

  private static String codeChallenge(String verifier) throws Exception {
    var digest =
        MessageDigest.getInstance("SHA-256").digest(verifier.getBytes(StandardCharsets.US_ASCII));
    return Base64.getUrlEncoder().withoutPadding().encodeToString(digest);
  }

  private String signatureCookie() {
    try {
      var claims =
          new JWTClaimsSet.Builder()
              .subject(USER_ID)
              .claim("user_id", USER_ID)
              .claim("user_name", "Smoke User")
              .claim("user_email", "smoke@example.com")
              .claim("tenant_id", 1L)
              .claim("tenant_url", TENANT_URL)
              .claim("is_admin", true)
              .claim("is_guest", false)
              .claim("is_public", false)
              .expirationTime(new Date(System.currentTimeMillis() + 3_600_000L))
              .issuer(TENANT_URL)
              .audience(TENANT_URL)
              .build();
      var jwt = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), claims);
      jwt.sign(new MACSigner(signingKey));
      return "x-signature=" + jwt.serialize();
    } catch (Exception e) {
      throw new IllegalStateException("Failed to sign the x-signature cookie", e);
    }
  }
}
