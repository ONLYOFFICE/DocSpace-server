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
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.RevokeConsentsResponse;
import com.asc.common.utilities.crypto.MachinePseudoKeys;
import com.asc.registration.application.service.ConsentService;
import com.asc.registration.application.transfer.ChangeClientActivationRequest;
import com.asc.registration.application.transfer.CreateClientRequest;
import com.asc.registration.application.transfer.UpdateClientRequest;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableModificationResponse;
import com.asc.registration.service.transfer.response.ScopeResponse;
import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import java.util.Date;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestInstance;
import org.junit.jupiter.api.TestMethodOrder;
import org.mockito.Mockito;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.RestClient;

@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
@TestInstance(TestInstance.Lifecycle.PER_CLASS)
public abstract class AbstractRegistrationServiceIT {

    protected static final String SIGNATURE_SECRET = "1123askdasjklasbnd";
    protected static final String WEB_API = "/api/2.0";
    protected static final String DEFAULT_USER_ID = "66faa6e4-f133-11ea-b126-00ffeec8b4ea";
    protected static final String TEST_CLIENT_NAME = "Test Client";
    protected static final String UPDATED_CLIENT_NAME = "Updated Client Name";

    @LocalServerPort
    protected int serverPort;

    protected String createdClientId;
    protected String createdClientSecret;

    protected abstract ConsentService getConsentService();

    protected abstract AuthorizationServiceGrpc.AuthorizationServiceBlockingStub getAuthorizationServiceClient();

    protected RestClient restClient() {
        return RestClient.builder()
            .baseUrl("http://localhost:" + serverPort)
            .build();
    }

    protected RestClient restClientIgnoringErrors() {
        return RestClient.builder()
            .baseUrl("http://localhost:" + serverPort)
            .defaultStatusHandler(status -> true, (req, res) -> {})
            .build();
    }

    protected static final class SignatureGenerator {
        private static final String DEFAULT_USER_NAME = "Administrator";
        private static final String DEFAULT_USER_EMAIL = "admin@admin.admin";
        private static final long DEFAULT_TENANT_ID = 1L;
        private static final String DEFAULT_TENANT_URL = "http://localhost:8092";
        private static final long TOKEN_VALIDITY_MS = 3600_000L;

        private final byte[] signingKey;

        public SignatureGenerator(String secret) {
            this.signingKey = new MachinePseudoKeys(secret).getMachineConstant(256);
        }

        public String generate(String userId, boolean isAdmin, boolean isGuest, boolean isPublic) {
            try {
                var now = new Date();
                var claims = new JWTClaimsSet.Builder()
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

    protected String randomUserId() {
        return UUID.randomUUID().toString();
    }

    protected String signatureCookie(String userId) {
        return "x-signature=" + signatureGenerator.generate(userId, false, false, true);
    }

    protected String signatureCookie(String userId, boolean isAdmin, boolean isGuest, boolean isPublic) {
        return "x-signature=" + signatureGenerator.generate(userId, isAdmin, isGuest, isPublic);
    }

    protected String defaultUserCookie() {
        return signatureCookie(DEFAULT_USER_ID);
    }

    protected String adminCookie() {
        return signatureCookie(DEFAULT_USER_ID, true, false, false);
    }

    protected String guestCookie() {
        return signatureCookie(DEFAULT_USER_ID, false, true, false);
    }

    protected String signatureHeader(String userId) {
        return signatureGenerator.generate(userId, false, false, true);
    }

    protected static CreateClientRequest validCreateClientRequest(Set<String> scopes) {
        return CreateClientRequest.builder()
            .name(TEST_CLIENT_NAME)
            .description("Test description")
            .logo("data:image/png;base64,ivBORw0KGgo=")
            .allowPkce(false)
            .isPublic(false)
            .websiteUrl("https://example.com")
            .termsUrl("https://example.com/terms")
            .policyUrl("https://example.com/policy")
            .redirectUris(Set.of("https://example.com/redirect"))
            .allowedOrigins(Set.of("https://example.com"))
            .logoutRedirectUri("https://example.com/logout")
            .scopes(scopes)
            .build();
    }

    protected static UpdateClientRequest validUpdateClientRequest() {
        return UpdateClientRequest.builder()
            .name(UPDATED_CLIENT_NAME)
            .description("Updated description")
            .logo("data:image/png;base64,ivBORw0KGgo=")
            .allowPkce(true)
            .isPublic(true)
            .allowedOrigins(Set.of("https://updated.example.com"))
            .build();
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

    protected ResponseEntity<ScopeResponse[]> getScopes(String cookie) {
        return restClient().get()
            .uri(WEB_API + "/scopes")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(ScopeResponse[].class);
    }

    protected ResponseEntity<String> getScopesRaw(String cookie) {
        return restClient().get()
            .uri(WEB_API + "/scopes")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> createClient(CreateClientRequest request, String cookie) {
        return restClient().post()
            .uri(WEB_API + "/clients")
            .header(HttpHeaders.COOKIE, cookie)
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> createClientIgnoringErrors(CreateClientRequest request, String cookie) {
        return restClientIgnoringErrors().post()
            .uri(WEB_API + "/clients")
            .header(HttpHeaders.COOKIE, cookie)
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClient(String clientId, String cookie) {
        return restClient().get()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClientIgnoringErrors(String clientId, String cookie) {
        return restClientIgnoringErrors().get()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClientInfo(String clientId, String cookie) {
        return restClient().get()
            .uri(WEB_API + "/clients/" + clientId + "/info")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClientInfoIgnoringErrors(String clientId) {
        return restClientIgnoringErrors().get()
            .uri(WEB_API + "/clients/" + clientId + "/info")
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getPublicClientInfo(String clientId) {
        return restClient().get()
            .uri(WEB_API + "/clients/" + clientId + "/public/info")
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClients(int limit, String cookie) {
        return restClient().get()
            .uri(WEB_API + "/clients?limit=" + limit)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClientsIgnoringErrors(int limit, String cookie) {
        return restClientIgnoringErrors().get()
            .uri(WEB_API + "/clients?limit=" + limit)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getClientsInfo(int limit, String cookie) {
        return restClient().get()
            .uri(WEB_API + "/clients/info?limit=" + limit)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> updateClient(String clientId, UpdateClientRequest request, String cookie) {
        return restClient().put()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> updateClientIgnoringErrors(String clientId, UpdateClientRequest request, String cookie) {
        return restClientIgnoringErrors().put()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> regenerateSecret(String clientId, String cookie) {
        return restClient().method(HttpMethod.PATCH)
            .uri(WEB_API + "/clients/" + clientId + "/regenerate")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> changeActivation(String clientId, boolean enabled, String cookie) {
        var request = ChangeClientActivationRequest.builder().enabled(enabled).build();
        return restClient().method(HttpMethod.PATCH)
            .uri(WEB_API + "/clients/" + clientId + "/activation")
            .header(HttpHeaders.COOKIE, cookie)
            .contentType(MediaType.APPLICATION_JSON)
            .body(request)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> revokeClient(String clientId, String cookie) {
        return restClient().delete()
            .uri(WEB_API + "/clients/" + clientId + "/revoke")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> deleteClient(String clientId, String cookie) {
        return restClient().delete()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> deleteClientIgnoringErrors(String clientId, String cookie) {
        return restClientIgnoringErrors().delete()
            .uri(WEB_API + "/clients/" + clientId)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> deleteUserClients(String cookie) {
        return restClient().delete()
            .uri(WEB_API + "/clients")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> deleteTenantClients(String cookie) {
        return restClient().delete()
            .uri(WEB_API + "/clients/tenant")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> deleteTenantClientsIgnoringErrors(String cookie) {
        return restClientIgnoringErrors().delete()
            .uri(WEB_API + "/clients/tenant")
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected ResponseEntity<String> getConsents(int limit, String cookie) {
        return restClient().get()
            .uri(WEB_API + "/clients/consents?limit=" + limit)
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .toEntity(String.class);
    }

    protected void mockRevokeConsentsSuccess() {
        Mockito.when(getAuthorizationServiceClient().revokeConsents(Mockito.any()))
            .thenReturn(RevokeConsentsResponse.newBuilder().setSuccess(true).build());
    }

    protected void mockGetConsentsEmpty() {
        Mockito.when(getConsentService().getConsents(Mockito.anyString(), Mockito.anyInt(), Mockito.any()))
            .thenReturn(PageableModificationResponse.<ConsentResponse>builder()
                .data(Set.of())
                .limit(10)
                .lastModifiedOn(null)
                .build());
    }

    @Nested
    @Order(1)
    @DisplayName("Client Lifecycle Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class ClientLifecycleTests {

        @Test
        @Order(1)
        @DisplayName("GET /scopes returns available scopes")
        void getScopes_withValidSignature_returnsScopes() {
            var response = getScopes(signatureCookie(randomUserId()));

            assertOk(response);
            assertThat(response.getBody()).isNotNull();
            assertThat(List.of(response.getBody()).stream().map(ScopeResponse::getName))
                .contains("openid", "files:read", "files:write", "accounts:read", "accounts:write");
        }

        @Test
        @Order(2)
        @DisplayName("POST /clients creates a new client")
        void createClient_withValidRequest_returnsCreatedClient() {
            var response = createClient(validCreateClientRequest(Set.of("files:read", "openid")), defaultUserCookie());

            assertCreated(response);
            assertThat(response.getBody())
                .contains("client_id", "client_secret", TEST_CLIENT_NAME);

            createdClientId = extractJsonField(response.getBody(), "client_id");
            createdClientSecret = extractJsonField(response.getBody(), "client_secret");

            assertThat(createdClientId).isNotBlank();
            assertThat(createdClientSecret).isNotBlank();
        }

        @Test
        @Order(3)
        @DisplayName("GET /clients/{id} returns client details")
        void getClient_withValidSignature_returnsClient() {
            assertThat(createdClientId).isNotBlank();

            var response = getClient(createdClientId, defaultUserCookie());

            assertOk(response);
            assertThat(response.getBody()).contains(createdClientId, TEST_CLIENT_NAME);
        }

        @Test
        @Order(4)
        @DisplayName("GET /clients/{id}/info returns client info")
        void getClientInfo_withValidSignature_returnsClientInfo() {
            var response = getClientInfo(createdClientId, defaultUserCookie());

            assertOk(response);
            assertThat(response.getBody()).contains(createdClientId);
        }

        @Test
        @Order(5)
        @DisplayName("GET /clients/{id}/public/info returns public info without auth")
        void getPublicClientInfo_withoutSignature_returnsClientInfo() {
            var response = getPublicClientInfo(createdClientId);

            assertOk(response);
            assertThat(response.getBody()).contains(createdClientId);
        }

        @Test
        @Order(6)
        @DisplayName("GET /clients returns client list")
        void getClients_withValidSignature_returnsClients() {
            var response = getClients(10, defaultUserCookie());

            assertOk(response);
            assertThat(response.getBody()).contains(createdClientId);
        }

        @Test
        @Order(7)
        @DisplayName("GET /clients/info returns client info list")
        void getClientsInfo_withValidSignature_returnsClientsInfo() {
            var response = getClientsInfo(10, defaultUserCookie());

            assertOk(response);
            assertThat(response.getBody()).contains(createdClientId);
        }

        @Test
        @Order(8)
        @DisplayName("PUT /clients/{id} updates client")
        void updateClient_withValidRequest_updatesClient() {
            var response = updateClient(createdClientId, validUpdateClientRequest(), defaultUserCookie());
            assertOk(response);

            var getResponse = getClient(createdClientId, defaultUserCookie());
            assertThat(getResponse.getBody())
                .contains(UPDATED_CLIENT_NAME, "https://updated.example.com");
        }

        @Test
        @Order(9)
        @DisplayName("PATCH /clients/{id}/regenerate generates new secret")
        void regenerateSecret_withValidSignature_returnsNewSecret() {
            var oldSecret = createdClientSecret;

            var response = regenerateSecret(createdClientId, defaultUserCookie());

            assertOk(response);
            var newSecret = extractJsonField(response.getBody(), "client_secret");
            assertThat(newSecret).isNotBlank().isNotEqualTo(oldSecret);

            createdClientSecret = newSecret;
        }

        @Test
        @Order(10)
        @DisplayName("PATCH /clients/{id}/activation disables client")
        void changeActivation_toDisabled_disablesClient() {
            var response = changeActivation(createdClientId, false, defaultUserCookie());
            assertOk(response);

            var getResponse = getClient(createdClientId, defaultUserCookie());
            assertThat(getResponse.getBody()).contains("\"enabled\":false");
        }

        @Test
        @Order(11)
        @DisplayName("PATCH /clients/{id}/activation enables client")
        void changeActivation_toEnabled_enablesClient() {
            var response = changeActivation(createdClientId, true, defaultUserCookie());
            assertOk(response);

            var getResponse = getClient(createdClientId, defaultUserCookie());
            assertThat(getResponse.getBody()).contains("\"enabled\":true");
        }

        @Test
        @Order(12)
        @DisplayName("DELETE /clients/{id}/revoke revokes client consents")
        void revokeClient_withValidSignature_revokesConsents() {
            mockRevokeConsentsSuccess();

            var response = revokeClient(createdClientId, defaultUserCookie());

            assertOk(response);
        }

        @Test
        @Order(13)
        @DisplayName("DELETE /clients/{id} deletes client")
        void deleteClient_withValidSignature_deletesClient() {
            var response = deleteClient(createdClientId, defaultUserCookie());
            assertOk(response);

            var getResponse = getClientIgnoringErrors(createdClientId, defaultUserCookie());
            assertClientError(getResponse);
        }

        @Test
        @Order(14)
        @DisplayName("GET /clients/consents returns user consents")
        void getConsents_withValidSignature_returnsConsents() {
            mockGetConsentsEmpty();

            var response = getConsents(10, defaultUserCookie());

            assertOk(response);
        }
    }

    @Nested
    @Order(2)
    @DisplayName("Authentication Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class AuthenticationTests {

        @Test
        @Order(1)
        @DisplayName("Request without signature returns 4xx")
        void getClientInfo_withoutSignature_returnsUnauthorized() {
            var response = getClientInfoIgnoringErrors("some-client-id");

            assertClientError(response);
        }

        @Test
        @Order(2)
        @DisplayName("Request with invalid signature returns 403")
        void getScopes_withInvalidSignature_returnsForbidden() {
            var invalidGenerator = new SignatureGenerator("wrong-secret");
            var invalidCookie = "x-signature=" + invalidGenerator.generate(randomUserId(), false, false, true);

            var response = restClientIgnoringErrors().get()
                .uri(WEB_API + "/scopes")
                .header(HttpHeaders.COOKIE, invalidCookie)
                .retrieve()
                .toEntity(String.class);

            assertForbidden(response);
        }

        @Test
        @Order(3)
        @DisplayName("Signature in header is accepted")
        void getScopes_withSignatureInHeader_returnsScopes() {
            var response = restClient().get()
                .uri(WEB_API + "/scopes")
                .header("x-signature", signatureHeader(randomUserId()))
                .retrieve()
                .toEntity(String.class);

            assertOk(response);
        }
    }

    @Nested
    @Order(3)
    @DisplayName("Authorization Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class AuthorizationTests {

        @Nested
        @DisplayName("Admin Role")
        class AdminRoleTests {

            @Test
            @DisplayName("Admin can delete all tenant clients")
            void deleteTenantClients_asAdmin_deletesAllTenantClients() {
                createClient(validCreateClientRequest(Set.of("files:read")), adminCookie());
                createClient(validCreateClientRequest(Set.of("openid")), adminCookie());

                var response = deleteTenantClients(adminCookie());
                assertOk(response);

                var listResponse = getClients(50, adminCookie());
                assertThat(listResponse.getBody()).doesNotContain(TEST_CLIENT_NAME);
            }
        }

        @Nested
        @DisplayName("User Role")
        class UserRoleTests {

            @Test
            @DisplayName("User can delete own clients")
            void deleteUserClients_withValidSignature_deletesUserClients() {
                var userId = randomUserId();
                var userCookie = signatureCookie(userId);

                createClient(validCreateClientRequest(Set.of("files:read")), userCookie);

                var listBefore = getClients(50, userCookie);
                assertThat(listBefore.getBody()).contains(TEST_CLIENT_NAME);

                deleteUserClients(userCookie);

                var listAfter = getClients(50, userCookie);
                assertThat(listAfter.getBody()).doesNotContain(TEST_CLIENT_NAME);
            }

            @Test
            @DisplayName("User cannot delete tenant clients")
            void deleteTenantClients_asNonAdmin_returnsForbidden() {
                var response = deleteTenantClientsIgnoringErrors(defaultUserCookie());

                assertForbidden(response);
            }
        }

        @Nested
        @DisplayName("Guest Role")
        class GuestRoleTests {

            @Test
            @DisplayName("Guest cannot list clients")
            void getClients_asGuest_returnsForbidden() {
                var response = getClientsIgnoringErrors(10, guestCookie());

                assertForbidden(response);
            }

            @Test
            @DisplayName("Guest cannot create clients")
            void createClient_asGuest_returnsForbidden() {
                var response = createClientIgnoringErrors(
                    validCreateClientRequest(Set.of("files:read")),
                    guestCookie());

                assertForbidden(response);
            }

            @Test
            @DisplayName("Guest can revoke own consents")
            void revokeConsent_asGuest_succeeds() {
                mockRevokeConsentsSuccess();

                var response = revokeClient("some-client-id", guestCookie());

                assertOk(response);
            }
        }
    }

    @Nested
    @Order(4)
    @DisplayName("Validation Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class ValidationTests {

        @Test
        @DisplayName("Empty name returns 400")
        void createClient_withEmptyName_returnsBadRequest() {
            var request = CreateClientRequest.builder()
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

            var response = createClientIgnoringErrors(request, defaultUserCookie());

            assertBadRequest(response);
        }

        @Test
        @DisplayName("Empty redirect URIs returns 400")
        void createClient_withEmptyRedirectUris_returnsBadRequest() {
            var request = CreateClientRequest.builder()
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

            var response = createClientIgnoringErrors(request, defaultUserCookie());

            assertBadRequest(response);
        }

        @Test
        @DisplayName("Empty scopes returns 400")
        void createClient_withEmptyScopes_returnsBadRequest() {
            var request = CreateClientRequest.builder()
                .name(TEST_CLIENT_NAME)
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
                .scopes(Set.of())
                .build();

            var response = createClientIgnoringErrors(request, defaultUserCookie());

            assertBadRequest(response);
        }

        @Test
        @DisplayName("Invalid scopes returns 400")
        void createClient_withInvalidScopes_returnsBadRequest() {
            var response = createClientIgnoringErrors(
                validCreateClientRequest(Set.of("invalid:scope")),
                signatureCookie(randomUserId()));

            assertBadRequest(response);
        }
    }

    @Nested
    @Order(5)
    @DisplayName("Error Handling Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class ErrorHandlingTests {

        @Test
        @DisplayName("GET non-existent client returns 404")
        void getClient_nonExistent_returnsNotFound() {
            var response = getClientIgnoringErrors(UUID.randomUUID().toString(), defaultUserCookie());

            assertNotFound(response);
        }

        @Test
        @DisplayName("PUT non-existent client returns 404")
        void updateClient_nonExistent_returnsNotFound() {
            var response = updateClientIgnoringErrors(
                UUID.randomUUID().toString(),
                validUpdateClientRequest(),
                defaultUserCookie());

            assertNotFound(response);
        }

        @Test
        @DisplayName("DELETE non-existent client returns 4xx")
        void deleteClient_nonExistent_returnsClientError() {
            var response = deleteClientIgnoringErrors(UUID.randomUUID().toString(), defaultUserCookie());

            assertClientError(response);
        }

        @Test
        @DisplayName("Invalid UUID format returns 404")
        void getClient_invalidUuidFormat_returnsNotFound() {
            var response = getClientIgnoringErrors("not-a-valid-uuid", defaultUserCookie());

            assertNotFound(response);
        }
    }

    @Nested
    @Order(6)
    @DisplayName("Pagination Tests")
    @TestMethodOrder(MethodOrderer.OrderAnnotation.class)
    class PaginationTests {

        @Test
        @DisplayName("GET /clients returns pagination metadata")
        void getClients_withPagination_returnsPaginatedResults() {
            var userId = randomUserId();
            var userCookie = signatureCookie(userId);

            for (int i = 0; i < 3; i++) {
                createClient(validCreateClientRequest(Set.of("files:read")), userCookie);
            }

            var firstPage = getClients(2, userCookie);
            assertOk(firstPage);
            assertThat(firstPage.getBody()).contains("last_client_id", "last_created_on");

            deleteUserClients(userCookie);
        }

        @Test
        @DisplayName("GET /clients/info returns pagination metadata")
        void getClientsInfo_withPagination_returnsPaginatedResults() {
            var userId = randomUserId();
            var userCookie = signatureCookie(userId);

            for (int i = 0; i < 3; i++) {
                createClient(validCreateClientRequest(Set.of("openid")), userCookie);
            }

            var firstPage = getClientsInfo(2, userCookie);
            assertOk(firstPage);
            assertThat(firstPage.getBody()).contains("last_client_id");

            deleteUserClients(userCookie);
        }
    }
}
