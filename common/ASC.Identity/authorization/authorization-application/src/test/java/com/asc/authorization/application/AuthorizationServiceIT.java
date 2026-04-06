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

package com.asc.authorization.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.authorization.application.security.oauth.service.GrpcRegisteredClientService;
import com.asc.authorization.application.security.oauth.service.RegisteredClientService;
import com.asc.common.application.proto.ClientResponse;
import com.asc.common.utilities.crypto.HashingService;
import com.asc.common.utilities.crypto.MachinePseudoKeys;
import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.JWSSigner;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import java.time.Instant;
import java.util.Arrays;
import java.util.Date;
import java.util.Objects;
import java.util.regex.Pattern;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.mockito.Mockito;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestClient;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;
import org.testcontainers.junit.jupiter.Container;
import org.testcontainers.junit.jupiter.Testcontainers;

@Testcontainers
@ActiveProfiles("test")
@EnabledIfSystemProperty(named = "RUN_INTEGRATION_TESTS", matches = "true")
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    classes = AuthorizationServiceIT.TestApplication.class)
public class AuthorizationServiceIT {
  static class TestSignatureGenerator {
    private static final String DEFAULT_TENANT_ID = "1";
    private static final String DEFAULT_USER_NAME = "Administrator";
    private static final String DEFAULT_USER_EMAIL = "admin@admin.admin";
    private static final String DEFAULT_TENANT_URL = "http://localhost:8092";
    private static final String DEFAULT_USER_ID = "66faa6e4-f133-11ea-b126-00ffeec8b4ea";

    private final byte[] signingKey;

    TestSignatureGenerator(String secret) {
      var machineKeys = new MachinePseudoKeys(secret);
      this.signingKey = machineKeys.getMachineConstant(256);
    }

    String generateSignature() {
      try {
        var now = new Date();
        var expiration = new Date(now.getTime() + 3600_000);

        var claimsSet =
            new JWTClaimsSet.Builder()
                .subject(TestSignatureGenerator.DEFAULT_USER_ID)
                .claim("user_id", TestSignatureGenerator.DEFAULT_USER_ID)
                .claim("user_name", TestSignatureGenerator.DEFAULT_USER_NAME)
                .claim("user_email", TestSignatureGenerator.DEFAULT_USER_EMAIL)
                .claim("tenant_id", TestSignatureGenerator.DEFAULT_TENANT_ID)
                .claim("tenant_url", TestSignatureGenerator.DEFAULT_TENANT_URL)
                .claim("is_admin", String.valueOf(true))
                .claim("is_guest", String.valueOf(false))
                .claim("is_public", String.valueOf(true))
                .expirationTime(expiration)
                .issuer(TestSignatureGenerator.DEFAULT_TENANT_URL)
                .audience(TestSignatureGenerator.DEFAULT_TENANT_URL)
                .build();

        var header = new JWSHeader(JWSAlgorithm.HS256);
        var signedJWT = new SignedJWT(header, claimsSet);

        JWSSigner signer = new MACSigner(signingKey);
        signedJWT.sign(signer);

        return signedJWT.serialize();
      } catch (Exception e) {
        throw new RuntimeException("Failed to generate test signature", e);
      }
    }
  }

  private static final String CLIENT_SECRET = "client-secret";
  private static final String REDIRECT_URI = "https://google.com";
  private static final String SCOPE = "files:read";
  private static final String SIGNATURE_SECRET = "1123askdasjklasbnd";
  private final TestSignatureGenerator signatureGenerator =
      new TestSignatureGenerator(SIGNATURE_SECRET);

  @LocalServerPort int randomServerPort;

  @EntityScan(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
  @SpringBootApplication(scanBasePackages = {"com.asc.authorization", "com.asc.common"})
  @EnableJpaRepositories(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
  static class TestApplication {}

  @Container
  static MySQLContainer<?> mysql = new MySQLContainer<>("mysql:8.0").withInitScript("init.sql");

  @Container static RabbitMQContainer rabbitmq = new RabbitMQContainer("rabbitmq:3.11-management");

  @Container
  static GenericContainer<?> redis = new GenericContainer<>("redis:7.0").withExposedPorts(6379);

  @MockitoBean private RegisteredClientService registeredClientService;
  @MockitoBean private GrpcRegisteredClientService grpcRegisteredClientService;
  @MockitoBean private HashingService hashingService;

  @DynamicPropertySource
  static void configureTestContainers(DynamicPropertyRegistry registry) {
    registry.add("spring.datasource.url", mysql::getJdbcUrl);
    registry.add("spring.datasource.username", mysql::getUsername);
    registry.add("spring.datasource.password", mysql::getPassword);
    registry.add("spring.datasource.driver-class-name", () -> "com.mysql.cj.jdbc.Driver");

    registry.add("spring.rabbitmq.host", rabbitmq::getHost);
    registry.add("spring.rabbitmq.port", rabbitmq::getAmqpPort);
    registry.add("spring.rabbitmq.username", rabbitmq::getAdminUsername);
    registry.add("spring.rabbitmq.password", rabbitmq::getAdminPassword);

    registry.add("spring.data.redis.host", redis::getHost);
    registry.add("spring.data.redis.port", () -> redis.getMappedPort(6379));
  }

  @BeforeEach
  void setUp() {
    Mockito.when(registeredClientService.validateClientAccessibility(Mockito.anyString()))
        .thenReturn(true);
    Mockito.when(hashingService.hash(Mockito.anyString()))
        .thenAnswer(invocation -> invocation.getArgument(0));
  }

  private RestClient createClient() {
    return RestClient.builder().baseUrl("http://localhost:" + randomServerPort).build();
  }

  private RestClient createClientIgnoringErrors() {
    return RestClient.builder()
        .baseUrl("http://localhost:" + randomServerPort)
        .defaultStatusHandler(status -> true, (req, res) -> {})
        .build();
  }

  private void setupMockClient(String clientId) {
    setupMockClient(clientId, false);
  }

  private void setupMockClient(String clientId, boolean allowPersonalAccessToken) {
    Mockito.when(grpcRegisteredClientService.getClient(clientId))
        .thenReturn(
            ClientResponse.newBuilder()
                .setClientId(clientId)
                .setClientSecret(CLIENT_SECRET)
                .setEnabled(true)
                .setIsPublic(true)
                .build());

    var clientBuilder =
        RegisteredClient.withId(clientId)
            .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
            .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
            .authorizationGrantType(AuthorizationGrantType.CLIENT_CREDENTIALS)
            .clientId(clientId)
            .clientSecret(CLIENT_SECRET)
            .clientAuthenticationMethod(ClientAuthenticationMethod.CLIENT_SECRET_POST)
            .clientIdIssuedAt(Instant.MIN)
            .clientSecretExpiresAt(Instant.MAX)
            .redirectUri(REDIRECT_URI)
            .scope(SCOPE)
            .clientSettings(
                ClientSettings.builder()
                    .requireAuthorizationConsent(true)
                    .requireProofKey(false)
                    .build());

    if (allowPersonalAccessToken) {
      clientBuilder.authorizationGrantType(new AuthorizationGrantType("personal_access_token"));
    }

    Mockito.when(registeredClientService.findByClientId(clientId))
        .thenReturn(clientBuilder.build());
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
        .header(HttpHeaders.COOKIE, "x-signature=" + signatureGenerator.generateSignature())
        .retrieve()
        .toEntity(String.class);
  }

  private String extractStateFromCookie(ResponseEntity<String> response) {
    var cookies = response.getHeaders().get(HttpHeaders.SET_COOKIE);
    assertThat(cookies).isNotNull();

    var stateCookie =
        cookies.stream()
            .filter(cookie -> cookie.startsWith("client_state="))
            .findFirst()
            .orElse(null);
    assertThat(stateCookie).isNotNull();

    var state = Arrays.stream(stateCookie.split(";")).findFirst().orElse(null);
    assertThat(state).isNotNull();

    var stateValue = Arrays.stream(state.split("client_state=")).skip(1).findFirst().orElse(null);
    assertThat(stateValue).isNotNull();

    return stateValue;
  }

  private ResponseEntity<String> submitConsent(String clientId, String stateValue) {
    var formData = new LinkedMultiValueMap<String, String>();
    formData.add("client_id", clientId);
    formData.add("state", stateValue);
    formData.add("scope", SCOPE);

    return createClient()
        .post()
        .uri("/oauth2/authorize")
        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
        .body(formData)
        .header(HttpHeaders.COOKIE, "x-signature=" + signatureGenerator.generateSignature())
        .header("X-Signature", signatureGenerator.generateSignature())
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
      RestClient client, String clientId, String code) {
    var formData = new LinkedMultiValueMap<String, String>();
    formData.add("grant_type", "authorization_code");
    formData.add("code", code);
    formData.add("redirect_uri", REDIRECT_URI);
    formData.add("client_id", clientId);
    formData.add("client_secret", CLIENT_SECRET);

    return postTokenEndpoint(client, formData);
  }

  private ResponseEntity<String> refreshTokens(
      RestClient client, String clientId, String refreshToken) {
    var formData = new LinkedMultiValueMap<String, String>();
    formData.add("grant_type", "refresh_token");
    formData.add("refresh_token", refreshToken);
    formData.add("client_id", clientId);
    formData.add("client_secret", CLIENT_SECRET);

    return postTokenEndpoint(client, formData);
  }

  private ResponseEntity<String> requestPersonalAccessToken(RestClient client, String clientId) {
    var formData = new LinkedMultiValueMap<String, String>();
    formData.add("grant_type", "personal_access_token");
    formData.add("scope", SCOPE);
    formData.add("client_id", clientId);
    formData.add("client_secret", CLIENT_SECRET);

    return postTokenEndpoint(client, formData);
  }

  private ResponseEntity<String> introspectToken(RestClient client, String clientId, String token) {
    var formData = new LinkedMultiValueMap<String, String>();
    formData.add("token", token);
    formData.add("client_id", clientId);
    formData.add("client_secret", CLIENT_SECRET);

    return client
        .post()
        .uri("/oauth2/introspect")
        .header(HttpHeaders.COOKIE, "x-signature=" + signatureGenerator.generateSignature())
        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
        .body(formData)
        .retrieve()
        .toEntity(String.class);
  }

  private ResponseEntity<String> postTokenEndpoint(
      RestClient client, MultiValueMap<String, String> formData) {
    return client
        .post()
        .uri("/oauth2/token")
        .header(HttpHeaders.COOKIE, "x-signature=" + signatureGenerator.generateSignature())
        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
        .body(formData)
        .retrieve()
        .toEntity(String.class);
  }

  private String completeAuthorizationFlow(String clientId) {
    setupMockClient(clientId);
    var client = createClient();

    var authResponse = startAuthorization(client, clientId);
    var stateValue = extractStateFromCookie(authResponse);
    var consentResponse = submitConsent(clientId, stateValue);
    var code = extractCodeFromRedirect(consentResponse);
    assertThat(code).isNotNull();

    var tokenResponse = exchangeCodeForTokens(client, clientId, code);
    assertThat(tokenResponse.getStatusCode().is2xxSuccessful()).isTrue();

    return tokenResponse.getBody();
  }

  private static String extractJsonValue(String json, String key) {
    var pattern = "\"" + key + "\":\"([^\"]+)\"";
    var matcher = Pattern.compile(pattern).matcher(json);
    return matcher.find() ? matcher.group(1) : null;
  }

  @Nested
  @DisplayName("Authorization state lifecycle")
  class AuthorizationStateIT {
    private static final String CLIENT_ID = "state-client-id";

    @BeforeEach
    void setUpClient() {
      setupMockClient(CLIENT_ID);
    }

    @Test
    void givenValidClient_whenAuthStarts_thenStateCookieIsSet() {
      var response = startAuthorization(createClient(), CLIENT_ID);

      assertThat(response.getHeaders().get(HttpHeaders.SET_COOKIE))
          .isNotNull()
          .anyMatch(cookie -> cookie.startsWith("client_state="));
    }

    @Test
    void givenInvalidClient_whenAuthStarts_thenErrorRedirect() {
      var response = startAuthorization(createClientIgnoringErrors(), "invalid-client-id");

      assertThat(response.getStatusCode().is3xxRedirection()).isTrue();
      assertThat(Objects.requireNonNull(response.getHeaders().getLocation()).getQuery())
          .endsWith("error=client_not_found_error");
    }
  }

  @Nested
  @DisplayName("Authorization consent lifecycle")
  class AuthorizationConsentIT {
    private static final String CLIENT_ID = "consent-client-id";

    @BeforeEach
    void setUpClient() {
      setupMockClient(CLIENT_ID);
    }

    @Test
    void givenValidClient_whenConsentApproved_thenCodeReturned() {
      var authResponse = startAuthorization(createClient(), CLIENT_ID);
      var stateValue = extractStateFromCookie(authResponse);

      var consentResponse = submitConsent(CLIENT_ID, stateValue);

      assertThat(consentResponse.getHeaders().getLocation()).isNotNull();
      assertThat(consentResponse.getHeaders().getLocation().toString()).contains("code=");
    }

    @Test
    void givenValidClient_whenConsentWithInvalidState_thenErrorRedirect() {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("client_id", CLIENT_ID);
      formData.add("state", "invalid-state-value");
      formData.add("scope", SCOPE);

      var response =
          createClientIgnoringErrors()
              .post()
              .uri("/oauth2/authorize")
              .contentType(MediaType.APPLICATION_FORM_URLENCODED)
              .body(formData)
              .header(HttpHeaders.COOKIE, "x-signature=" + signatureGenerator.generateSignature())
              .retrieve()
              .toEntity(String.class);

      assertThat(response.getStatusCode().is3xxRedirection()).isTrue();
      assertThat(response.getHeaders().getLocation()).isNotNull();
      assertThat(response.getHeaders().getLocation().toString()).contains("error=");
    }
  }

  @Nested
  @DisplayName("Authorization token lifecycle")
  class AuthorizationTokenIT {
    private static final String CLIENT_ID = "token-client-id";

    @BeforeEach
    void setUpClient() {
      setupMockClient(CLIENT_ID);
    }

    @Test
    void givenValidClient_whenFullAuthFlow_thenAccessTokenReturned() {
      var tokenResponseBody = completeAuthorizationFlow("full-flow-client");

      assertThat(tokenResponseBody).isNotNull();
      assertThat(tokenResponseBody).contains("access_token");
    }

    @Test
    void givenInvalidCode_whenTokenExchange_thenError() {
      var formData = new LinkedMultiValueMap<String, String>();
      formData.add("grant_type", "authorization_code");
      formData.add("code", "invalid-code-value");
      formData.add("redirect_uri", REDIRECT_URI);
      formData.add("client_id", CLIENT_ID);
      formData.add("client_secret", CLIENT_SECRET);

      var response = postTokenEndpoint(createClientIgnoringErrors(), formData);

      assertThat(response.getStatusCode().is4xxClientError()).isTrue();
      assertThat(response.getBody()).contains("error");
    }

    @Test
    void givenValidRefreshToken_whenRefreshTokenExchange_thenNewTokensReturned() {
      var clientId = "refresh-token-client";
      var tokenResponseBody = completeAuthorizationFlow(clientId);

      assertThat(tokenResponseBody).contains("refresh_token");
      var refreshToken = extractJsonValue(tokenResponseBody, "refresh_token");
      assertThat(refreshToken).isNotNull();

      var refreshResponse = refreshTokens(createClient(), clientId, refreshToken);

      assertThat(refreshResponse.getStatusCode().is2xxSuccessful()).isTrue();
      assertThat(refreshResponse.getBody()).contains("access_token");
    }

    @Test
    void givenPatEnabledClient_whenRequestPat_thenAccessTokenReturned() {
      var clientId = "pat-client";
      setupMockClient(clientId, true);

      var response = requestPersonalAccessToken(createClient(), clientId);

      assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
      assertThat(response.getBody()).contains("access_token");
    }

    @Test
    void givenInvalidToken_whenIntrospect_thenInactive() {
      var clientId = "introspect-invalid-client";
      setupMockClient(clientId);

      var response = introspectToken(createClientIgnoringErrors(), clientId, "invalid-token");

      assertThat(response.getBody()).contains("\"active\":false");
    }
  }
}
