package com.asc.registration.service.mapper;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.entity.Consent;
import com.asc.common.core.domain.value.ConsentId;
import com.asc.common.core.domain.value.enums.ConsentStatus;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import java.time.ZonedDateTime;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ConsentDataMapperTest {
  private ConsentDataMapper consentDataMapper;

  @BeforeEach
  void setUp() {
    consentDataMapper = new ConsentDataMapper();
  }

  @Test
  void testToConsentResponse() {
    var consent = createConsent();
    var clientInfoResponse = createClientInfoResponse();
    var response = consentDataMapper.toConsentResponse(consent, clientInfoResponse);

    assertNotNull(response);
    assertEquals(consent.getId().getRegisteredClientId(), response.getRegisteredClientId());
    assertEquals(consent.getId().getPrincipalName(), response.getPrincipalName());
    assertEquals(String.join(",", consent.getScopes()), response.getScopes());
    assertEquals(consent.getStatus().equals(ConsentStatus.INVALIDATED), response.isInvalidated());
    assertEquals(consent.getModifiedOn(), response.getModifiedOn());
    assertEquals(clientInfoResponse, response.getClient());
  }

  @Test
  void testToConsentResponse_NullConsent() {
    var clientInfoResponse = createClientInfoResponse();
    var exception =
        assertThrows(
            IllegalArgumentException.class,
            () -> consentDataMapper.toConsentResponse(null, clientInfoResponse));

    assertEquals("Consent cannot be null", exception.getMessage());
  }

  @Test
  void testToConsentResponse_NullClientInfoResponse() {
    var consent = createConsent();
    var exception =
        assertThrows(
            IllegalArgumentException.class,
            () -> consentDataMapper.toConsentResponse(consent, null));

    assertEquals("ClientResponse cannot be null", exception.getMessage());
  }

  private Consent createConsent() {
    return Consent.Builder.builder()
        .id(new ConsentId("clientId", "principalName"))
        .scopes(Set.of("read", "write"))
        .status(ConsentStatus.ACTIVE)
        .modifiedOn(ZonedDateTime.now())
        .build();
  }

  private ClientInfoResponse createClientInfoResponse() {
    return ClientInfoResponse.builder()
        .name("Test Client")
        .clientId("clientId")
        .description("Test Description")
        .websiteUrl("http://test.com")
        .termsUrl("http://test.com/terms")
        .policyUrl("http://test.com/policy")
        .logo("Test Logo")
        .authenticationMethods(Set.of("client_secret_post", "pkce"))
        .redirectUris(Set.of("http://test.com/redirect"))
        .allowedOrigins(Set.of("http://test.com"))
        .logoutRedirectUri(Set.of("http://test.com/logout"))
        .scopes(Set.of("read", "write"))
        .createdOn(ZonedDateTime.now())
        .createdBy("creator")
        .modifiedOn(ZonedDateTime.now())
        .modifiedBy("modifier")
        .build();
  }
}
