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

package com.asc.identity.minified;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.common.utilities.crypto.MachinePseudoKeys;
import com.asc.registration.application.transfer.CreateClientRequest;
import com.asc.registration.application.transfer.UpdateClientRequest;
import com.asc.registration.service.transfer.response.ScopeResponse;
import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import java.util.Arrays;
import java.util.Date;
import java.util.Objects;
import java.util.Set;
import java.util.UUID;
import java.util.regex.Pattern;
import java.util.stream.Stream;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestClient;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.junit.jupiter.Testcontainers;

@Testcontainers
@ActiveProfiles({"test", "minified"})
@TestInstance(TestInstance.Lifecycle.PER_CLASS)
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
@EnabledIfSystemProperty(named = "RUN_INTEGRATION_TESTS", matches = "true")
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
public class IdentityMinifiedServiceIT {
  private static final String WEB_API = "/api/2.0";
  private static final String SCOPE = "files:read";
  private static final String TEST_CLIENT_NAME = "OAuth Client";
  private static final String UPDATED_CLIENT_NAME = "Updated OAuth Client";
  private static final String SIGNATURE_SECRET = "1123askdasjklasbnd";
  private static final String REDIRECT_URI = "https://example.com/callback";
  private static final String DEFAULT_USER_ID = "66faa6e4-f133-11ea-b126-00ffeec8b4ea";

  private String createdClientId;
  private String createdClientSecret;

  static MySQLContainer<?> mysql = new MySQLContainer<>("mysql:8.0").withInitScript("init.sql");

  static {
    mysql.start();
  }

  @LocalServerPort private int serverPort;

  @DynamicPropertySource
  static void configureProperties(DynamicPropertyRegistry registry) {
    registry.add("spring.datasource.url", mysql::getJdbcUrl);
    registry.add("spring.datasource.username", mysql::getUsername);
    registry.add("spring.datasource.password", mysql::getPassword);
    registry.add("spring.datasource.driver-class-name", () -> "com.mysql.cj.jdbc.Driver");
  }

  protected static final class SignatureGenerator {
    private static final long DEFAULT_TENANT_ID = 1L;
    private static final long TOKEN_VALIDITY_MS = 3600_000L;
    private static final String DEFAULT_USER_NAME = "Administrator";
    private static final String DEFAULT_USER_EMAIL = "admin@admin.admin";
    private static final String DEFAULT_TENANT_URL = "http://localhost:8092";

    private final byte[] signingKey;

    public SignatureGenerator(String secret) {
      this.signingKey = new MachinePseudoKeys(secret).getMachineConstant(256);
    }

    public String generate(String userId, boolean isAdmin, boolean isGuest, boolean isPublic) {
      try {
        var now = new Date();
        var claims =
            new JWTClaimsSet.Builder()
                .subject(userId)
                .claim("user_id", userId)
                .claim("user_name", DEFAULT_USER_NAME)
                .claim("user_email", DEFAULT_USER_EMAIL)
                .claim("tenant_id", DEFAULT_TENANT_ID)
                .claim("tenant_url", DEFAULT_TENANT_URL)
                .claim("is_admin", isAdmin)
                .claim("is_guest", isGuest)
                .claim("is_public", isPublic)
                .expirationTime(new Date(now.getTime() + TOKEN_VALIDITY_MS))
                .issuer(DEFAULT_TENANT_URL)
                .audience(DEFAULT_TENANT_URL)
                .build();

        var jwt = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), claims);
        jwt.sign(new MACSigner(signingKey));
        return jwt.serialize();
      } catch (Exception e) {
        throw new IllegalStateException("Failed to generate test signature", e);
      }
    }
  }

  private final SignatureGenerator signatureGenerator = new SignatureGenerator(SIGNATURE_SECRET);

  protected RestClient restClient() {
    return RestClient.builder().baseUrl("http://localhost:" + serverPort).build();
  }

  protected RestClient restClientIgnoringErrors() {
    return RestClient.builder()
        .baseUrl("http://localhost:" + serverPort)
        .defaultStatusHandler(status -> true, (req, res) -> {})
        .build();
  }

  protected String randomUserId() {
    return UUID.randomUUID().toString();
  }

  protected String signatureCookie(String userId) {
    return "x-signature=" + signatureGenerator.generate(userId, false, false, true);
  }

  protected String signatureCookie(
      String userId, boolean isAdmin, boolean isGuest, boolean isPublic) {
    return "x-signature=" + signatureGenerator.generate(userId, isAdmin, isGuest, isPublic);
  }

  protected String defaultUserCookie() {
    return signatureCookie(DEFAULT_USER_ID);
  }

  protected String adminCookie() {
    return signatureCookie(DEFAULT_USER_ID, true, false, false);
  }

  protected String signatureHeader(String userId) {
    return signatureGenerator.generate(userId, false, false, true);
  }

  protected String extractJsonField(String json, String fieldName) {
    if (json == null) return null;
    var pattern = "\"" + fieldName + "\":\"";
    var startIndex = json.indexOf(pattern);
    if (startIndex == -1) return null;
    startIndex += pattern.length();
    var endIndex = json.indexOf("\"", startIndex);
    return endIndex == -1 ? null : json.substring(startIndex, endIndex);
  }

  protected void assertOk(ResponseEntity<?> response) {
    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
  }

  protected void assertCreated(ResponseEntity<?> response) {
    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CREATED);
  }

  protected void assertBadRequest(ResponseEntity<?> response) {
    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
  }

  protected void assertForbidden(ResponseEntity<?> response) {
    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FORBIDDEN);
  }

  protected void assertNotFound(ResponseEntity<?> response) {
    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
  }

  protected void assertClientError(ResponseEntity<?> response) {
    assertThat(response.getStatusCode().is4xxClientError()).isTrue();
  }

  protected static CreateClientRequest validCreateClientRequest(Set<String> scopes) {
    return CreateClientRequest.builder()
        .name(TEST_CLIENT_NAME)
        .description("OAuth2 client for integration testing")
        .logo("data:image/png;base64,ivBORw0KGgo=")
        .allowPkce(false)
        .isPublic(false)
        .websiteUrl("https://example.com")
        .termsUrl("https://example.com/terms")
        .policyUrl("https://example.com/policy")
        .redirectUris(Set.of(REDIRECT_URI))
        .allowedOrigins(Set.of("https://example.com"))
        .logoutRedirectUri("https://example.com/logout")
        .scopes(scopes)
        .build();
  }

  protected static UpdateClientRequest validUpdateClientRequest() {
    return UpdateClientRequest.builder()
        .name(UPDATED_CLIENT_NAME)
        .description("Updated OAuth2 client description")
        .logo("data:image/png;base64,ivBORw0KGgo=")
        .allowPkce(true)
        .isPublic(true)
        .allowedOrigins(Set.of("https://updated.example.com"))
        .redirectUris(Set.of("https://updated.example.com/redirect"))
        .scopes(Set.of("files:read", "openid"))
        .build();
  }

  @Nested
  @Order(1)
  @DisplayName("Client CRUD lifecycle tests")
  @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
  class ClientCrudTests {
    @Test
    @Order(1)
    void givenValidSignature_whenGetScopes_thenReturnScopes() {
      var response =
          restClient()
              .get()
              .uri(WEB_API + "/scopes")
              .header(HttpHeaders.COOKIE, signatureCookie(randomUserId()))
              .retrieve()
              .toEntity(ScopeResponse[].class);

      assertOk(response);
      assertThat(response.getBody()).isNotNull();
      assertThat(Stream.of(response.getBody()).map(ScopeResponse::getName))
          .contains("openid", "files:read", "files:write", "accounts:read", "accounts:write");
    }

    @Test
    @Order(2)
    void givenValidRequest_whenCreateClient_thenReturnCreatedClient() {
      var response =
          restClient()
              .post()
              .uri(WEB_API + "/clients")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body(validCreateClientRequest(Set.of("files:read", "openid")))
              .retrieve()
              .toEntity(String.class);

      assertCreated(response);
      assertThat(response.getBody()).contains("client_id", "client_secret", TEST_CLIENT_NAME);

      createdClientId = extractJsonField(response.getBody(), "client_id");
      createdClientSecret = extractJsonField(response.getBody(), "client_secret");

      assertThat(createdClientId).isNotBlank();
      assertThat(createdClientSecret).isNotBlank();
    }

    @Test
    @Order(3)
    void givenValidSignature_whenGetClient_thenReturnClient() {
      assertThat(createdClientId).isNotBlank();

      var response =
          restClient()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
      assertThat(response.getBody()).contains(createdClientId, TEST_CLIENT_NAME);
    }

    @Test
    @Order(4)
    void givenValidSignature_whenGetClientInfo_thenReturnClientInfo() {
      var response =
          restClient()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId + "/info")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
      assertThat(response.getBody()).contains(createdClientId);
    }

    @Test
    @Order(5)
    void givenNoSignature_whenGetPublicClientInfo_thenReturnClientInfo() {
      var response =
          restClient()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId + "/public/info")
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
      assertThat(response.getBody()).contains(createdClientId);
    }

    @Test
    @Order(6)
    void givenValidSignature_whenGetClients_thenReturnClients() {
      var response =
          restClient()
              .get()
              .uri(WEB_API + "/clients?limit=10")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
      assertThat(response.getBody()).contains(createdClientId);
    }

    @Test
    @Order(7)
    void givenValidRequest_whenUpdateClient_thenUpdateClient() {
      var response =
          restClient()
              .put()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body(validUpdateClientRequest())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);

      var getResponse =
          restClient()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertThat(getResponse.getBody())
          .contains(UPDATED_CLIENT_NAME, "https://updated.example.com");
    }

    @Test
    @Order(8)
    void givenValidSignature_whenRegenerateSecret_thenReturnNewSecret() {
      var oldSecret = createdClientSecret;

      var response =
          restClient()
              .patch()
              .uri(WEB_API + "/clients/" + createdClientId + "/regenerate")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
      var newSecret = extractJsonField(response.getBody(), "client_secret");
      assertThat(newSecret).isNotBlank().isNotEqualTo(oldSecret);

      createdClientSecret = newSecret;
    }

    @Test
    @Order(9)
    void givenValidSignature_whenChangeActivationToDisabled_thenDisableClient() {
      var response =
          restClient()
              .patch()
              .uri(WEB_API + "/clients/" + createdClientId + "/activation")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body("{\"enabled\": false}")
              .retrieve()
              .toEntity(String.class);

      assertOk(response);

      var getResponse =
          restClient()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertThat(getResponse.getBody()).contains("\"enabled\":false");
    }

    @Test
    @Order(10)
    void givenValidSignature_whenChangeActivationToEnabled_thenEnableClient() {
      var response =
          restClient()
              .patch()
              .uri(WEB_API + "/clients/" + createdClientId + "/activation")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body("{\"enabled\": true}")
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
    }
  }

  @Nested
  @Order(2)
  @DisplayName("OAuth2 Authorization Code tests")
  @TestInstance(TestInstance.Lifecycle.PER_CLASS)
  @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
  class AuthorizationCodeGrantFlowTests {
    private String authFlowClientId;
    private String authFlowClientSecret;

    @BeforeAll
    void createClientForAuthFlow() {
      var response =
          restClient()
              .post()
              .uri(WEB_API + "/clients")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body(
                  CreateClientRequest.builder()
                      .name("Auth Client")
                      .description("Client for testing OAuth2 authorization code flow")
                      .logo("data:image/png;base64,ivBORw0KGgo=")
                      .allowPkce(false)
                      .isPublic(false)
                      .websiteUrl("https://authflow.example.com")
                      .termsUrl("https://authflow.example.com/terms")
                      .policyUrl("https://authflow.example.com/policy")
                      .redirectUris(Set.of(REDIRECT_URI))
                      .allowedOrigins(Set.of("https://authflow.example.com"))
                      .logoutRedirectUri("https://authflow.example.com/logout")
                      .scopes(Set.of("files:read", "openid"))
                      .build())
              .retrieve()
              .toEntity(String.class);

      assertCreated(response);
      authFlowClientId = extractJsonField(response.getBody(), "client_id");
      authFlowClientSecret = extractJsonField(response.getBody(), "client_secret");
      assertThat(authFlowClientId).isNotBlank();
      assertThat(authFlowClientSecret).isNotBlank();
    }

    private ResponseEntity<String> startAuthorization(RestClient client, String clientId) {
      return client
          .get()
          .uri(
              uriBuilder ->
                  uriBuilder
                      .path("/oauth2/authorize")
                      .queryParam("redirect_uri", REDIRECT_URI)
                      .queryParam("response_type", "code")
                      .queryParam("scope", SCOPE)
                      .queryParam("client_id", clientId)
                      .build())
          .header(
              HttpHeaders.COOKIE,
              "x-signature=" + signatureGenerator.generate(DEFAULT_USER_ID, false, false, true))
          .retrieve()
          .toEntity(String.class);
    }

    private String extractStateFromCookie(ResponseEntity<String> response) {
      var cookies = response.getHeaders().get(HttpHeaders.SET_COOKIE);
      if (cookies == null) return null;

      var stateCookie =
          cookies.stream()
              .filter(cookie -> cookie.startsWith("client_state="))
              .findFirst()
              .orElse(null);
      if (stateCookie == null) return null;

      var state = Arrays.stream(stateCookie.split(";")).findFirst().orElse(null);
      if (state == null) return null;

      return Arrays.stream(state.split("client_state=")).skip(1).findFirst().orElse(null);
    }

    private ResponseEntity<String> submitConsent(String clientId, String stateValue) {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("client_id", clientId);
      formData.add("state", stateValue);
      formData.add("scope", SCOPE);

      return restClient()
          .post()
          .uri("/oauth2/authorize")
          .contentType(MediaType.APPLICATION_FORM_URLENCODED)
          .body(formData)
          .header(
              HttpHeaders.COOKIE,
              "x-signature=" + signatureGenerator.generate(DEFAULT_USER_ID, false, false, true))
          .header("X-Signature", signatureGenerator.generate(DEFAULT_USER_ID, false, false, true))
          .retrieve()
          .toEntity(String.class);
    }

    private String extractCodeFromRedirect(ResponseEntity<String> response) {
      assertThat(response.getHeaders().getLocation()).isNotNull();
      var locationUrl = response.getHeaders().getLocation().toString();
      assertThat(locationUrl).contains("code=");

      return Arrays.stream(locationUrl.split("code=")).skip(1).findFirst().orElse(null);
    }

    private ResponseEntity<String> exchangeCodeForTokens(
        RestClient client, String clientId, String clientSecret, String code) {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("grant_type", "authorization_code");
      formData.add("code", code);
      formData.add("redirect_uri", REDIRECT_URI);
      formData.add("client_id", clientId);
      formData.add("client_secret", clientSecret);

      return postTokenEndpoint(client, formData);
    }

    private ResponseEntity<String> refreshTokens(
        RestClient client, String clientId, String clientSecret, String refreshToken) {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("grant_type", "refresh_token");
      formData.add("refresh_token", refreshToken);
      formData.add("client_id", clientId);
      formData.add("client_secret", clientSecret);

      return postTokenEndpoint(client, formData);
    }

    private ResponseEntity<String> postTokenEndpoint(
        RestClient client, MultiValueMap<String, String> formData) {
      return client
          .post()
          .uri("/oauth2/token")
          .header(
              HttpHeaders.COOKIE,
              "x-signature=" + signatureGenerator.generate(DEFAULT_USER_ID, false, false, true))
          .contentType(MediaType.APPLICATION_FORM_URLENCODED)
          .body(formData)
          .retrieve()
          .toEntity(String.class);
    }

    private String extractJsonValue(String json, String key) {
      var pattern = "\"" + key + "\":\"([^\"]+)\"";
      var matcher = Pattern.compile(pattern).matcher(json);
      return matcher.find() ? matcher.group(1) : null;
    }

    @Test
    @Order(1)
    void givenValidClient_whenAuthStarts_thenStateCookieIsSet() {
      var response = startAuthorization(restClient(), authFlowClientId);

      assertThat(response.getHeaders().get(HttpHeaders.SET_COOKIE))
          .isNotNull()
          .anyMatch(cookie -> cookie.startsWith("client_state="));
    }

    @Test
    @Order(2)
    void givenInvalidClient_whenAuthStarts_thenErrorRedirect() {
      var response = startAuthorization(restClientIgnoringErrors(), "invalid-client-id");

      assertThat(response.getStatusCode().is3xxRedirection()).isTrue();
      assertThat(Objects.requireNonNull(response.getHeaders().getLocation()).getQuery())
          .endsWith("error=client_not_found_error");
    }

    @Test
    @Order(3)
    void givenValidClient_whenFullAuthFlow_thenAccessTokenReturned() {
      var client = restClient();

      var authResponse = startAuthorization(client, authFlowClientId);
      var stateValue = extractStateFromCookie(authResponse);

      var consentResponse = submitConsent(authFlowClientId, stateValue);
      assertThat(consentResponse.getHeaders().getLocation()).isNotNull();
      assertThat(consentResponse.getHeaders().getLocation().toString()).contains("code=");

      var code = extractCodeFromRedirect(consentResponse);
      assertThat(code).isNotNull();

      var tokenResponse =
          exchangeCodeForTokens(client, authFlowClientId, authFlowClientSecret, code);
      assertThat(tokenResponse.getStatusCode().is2xxSuccessful()).isTrue();
      assertThat(tokenResponse.getBody()).contains("access_token");
    }

    @Test
    @Order(4)
    void givenValidRefreshToken_whenRefreshTokenExchange_thenNewTokensReturned() {
      var client = restClientIgnoringErrors();

      var authResponse = startAuthorization(client, authFlowClientId);
      var cookies = authResponse.getHeaders().get(HttpHeaders.SET_COOKIE);
      if (cookies == null || cookies.stream().noneMatch(c -> c.startsWith("client_state="))) {
        return;
      }

      var stateValue = extractStateFromCookie(authResponse);
      var consentResponse = submitConsent(authFlowClientId, stateValue);

      if (consentResponse.getHeaders().getLocation() == null
          || !consentResponse.getHeaders().getLocation().toString().contains("code=")) {
        return;
      }

      var code = extractCodeFromRedirect(consentResponse);
      var tokenResponse =
          exchangeCodeForTokens(client, authFlowClientId, authFlowClientSecret, code);

      if (!tokenResponse.getStatusCode().is2xxSuccessful()) {
        return;
      }

      assertThat(tokenResponse.getBody()).contains("refresh_token");
      var refreshToken = extractJsonValue(tokenResponse.getBody(), "refresh_token");
      assertThat(refreshToken).isNotNull();

      var refreshResponse =
          refreshTokens(client, authFlowClientId, authFlowClientSecret, refreshToken);

      assertThat(refreshResponse.getStatusCode().is2xxSuccessful()).isTrue();
      assertThat(refreshResponse.getBody()).contains("access_token");
    }

    @Test
    @Order(5)
    void givenInvalidCode_whenTokenExchange_thenError() {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("grant_type", "authorization_code");
      formData.add("code", "invalid-code-value");
      formData.add("redirect_uri", REDIRECT_URI);
      formData.add("client_id", authFlowClientId);
      formData.add("client_secret", authFlowClientSecret);

      var response = postTokenEndpoint(restClientIgnoringErrors(), formData);

      assertThat(response.getStatusCode().is4xxClientError()).isTrue();
      assertThat(response.getBody()).contains("error");
    }

    @Test
    @Order(6)
    void givenInvalidToken_whenIntrospect_thenInactive() {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("token", "invalid-token");
      formData.add("client_id", authFlowClientId);
      formData.add("client_secret", authFlowClientSecret);

      var response =
          restClientIgnoringErrors()
              .post()
              .uri("/oauth2/introspect")
              .header(
                  HttpHeaders.COOKIE,
                  "x-signature=" + signatureGenerator.generate(DEFAULT_USER_ID, false, false, true))
              .contentType(MediaType.APPLICATION_FORM_URLENCODED)
              .body(formData)
              .retrieve()
              .toEntity(String.class);

      assertThat(response.getBody()).contains("\"active\":false");
    }
  }

  @Nested
  @Order(3)
  @DisplayName("Validation tests")
  @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
  class ValidationTests {
    @Test
    void givenEmptyName_whenCreateClient_thenReturnBadRequest() {
      var request =
          CreateClientRequest.builder()
              .name("")
              .description("Test")
              .logo("data:image/png;base64,ivBORw0KGgo=")
              .allowPkce(false)
              .isPublic(false)
              .websiteUrl("https://example.com")
              .termsUrl("https://example.com/terms")
              .policyUrl("https://example.com/policy")
              .redirectUris(Set.of("https://example.com/redirect"))
              .allowedOrigins(Set.of("https://example.com"))
              .logoutRedirectUri("https://example.com/logout")
              .scopes(Set.of("files:read"))
              .build();

      var response =
          restClientIgnoringErrors()
              .post()
              .uri(WEB_API + "/clients")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body(request)
              .retrieve()
              .toEntity(String.class);

      assertBadRequest(response);
    }

    @Test
    void givenEmptyRedirectUris_whenCreateClient_thenReturnBadRequest() {
      var request =
          CreateClientRequest.builder()
              .name(TEST_CLIENT_NAME)
              .description("Test")
              .logo("data:image/png;base64,ivBORw0KGgo=")
              .allowPkce(false)
              .isPublic(false)
              .websiteUrl("https://example.com")
              .termsUrl("https://example.com/terms")
              .policyUrl("https://example.com/policy")
              .redirectUris(Set.of())
              .allowedOrigins(Set.of("https://example.com"))
              .logoutRedirectUri("https://example.com/logout")
              .scopes(Set.of("files:read"))
              .build();

      var response =
          restClientIgnoringErrors()
              .post()
              .uri(WEB_API + "/clients")
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .contentType(MediaType.APPLICATION_JSON)
              .body(request)
              .retrieve()
              .toEntity(String.class);

      assertBadRequest(response);
    }

    @Test
    void givenInvalidScopes_whenCreateClient_thenReturnBadRequest() {
      var response =
          restClientIgnoringErrors()
              .post()
              .uri(WEB_API + "/clients")
              .header(HttpHeaders.COOKIE, signatureCookie(randomUserId()))
              .contentType(MediaType.APPLICATION_JSON)
              .body(validCreateClientRequest(Set.of("invalid:scope")))
              .retrieve()
              .toEntity(String.class);

      assertBadRequest(response);
    }

    @Test
    void givenClientNonExistent_whenGetClient_thenReturnNotFound() {
      var response =
          restClientIgnoringErrors()
              .get()
              .uri(WEB_API + "/clients/" + UUID.randomUUID())
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertNotFound(response);
    }
  }

  @Nested
  @Order(4)
  @DisplayName("Authentication tests")
  @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
  class AuthenticationTests {
    @Test
    void givenNoSignature_whenGetClientInfo_thenReturnUnauthorized() {
      var response =
          restClientIgnoringErrors()
              .get()
              .uri(WEB_API + "/clients/some-client-id/info")
              .retrieve()
              .toEntity(String.class);

      assertClientError(response);
    }

    @Test
    void givenInvalidSignature_whenGetScopes_thenReturnForbidden() {
      var invalidGenerator = new SignatureGenerator("wrong-secret");
      var invalidCookie =
          "x-signature=" + invalidGenerator.generate(randomUserId(), false, false, true);

      var response =
          restClientIgnoringErrors()
              .get()
              .uri(WEB_API + "/scopes")
              .header(HttpHeaders.COOKIE, invalidCookie)
              .retrieve()
              .toEntity(String.class);

      assertForbidden(response);
    }

    @Test
    void givenSignatureInHeader_whenGetScopes_thenReturnScopes() {
      var response =
          restClient()
              .get()
              .uri(WEB_API + "/scopes")
              .header("x-signature", signatureHeader(randomUserId()))
              .retrieve()
              .toEntity(String.class);

      assertOk(response);
    }
  }

  @Nested
  @Order(5)
  @DisplayName("Cleanup tests")
  @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
  class CleanupTests {
    @Test
    @Order(1)
    void givenValidSignature_whenDeleteClient_thenDeleteClient() {
      assertThat(createdClientId).isNotBlank();

      var response =
          restClient()
              .delete()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertOk(response);

      var getResponse =
          restClientIgnoringErrors()
              .get()
              .uri(WEB_API + "/clients/" + createdClientId)
              .header(HttpHeaders.COOKIE, defaultUserCookie())
              .retrieve()
              .toEntity(String.class);

      assertClientError(getResponse);
    }
  }
}
