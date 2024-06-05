package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.value.ClientCreationInfo;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ClientTest {
  private Client client;

  @BeforeEach
  void setUp() {
    client =
        Client.Builder.builder()
            .id(new ClientId(UUID.randomUUID()))
            .secret(new ClientSecret(UUID.randomUUID().toString()))
            .authenticationMethods(Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
            .scopes(Set.of("read", "write"))
            .clientInfo(new ClientInfo("Test Client", "Description", "Logo URL"))
            .clientTenantInfo(new ClientTenantInfo(new TenantId(1), "http://tenant.url"))
            .clientRedirectInfo(
                new ClientRedirectInfo(
                    Set.of("http://redirect.url"),
                    Set.of("http://allowed.origin"),
                    Set.of("http://logout.url")))
            .clientCreationInfo(
                ClientCreationInfo.Builder.builder()
                    .createdBy("creator")
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .build();
  }

  @Test
  void testInitialize() {
    client.initialize("creator@example.com");

    assertNotNull(client.getId());
    assertNotNull(client.getSecret());
    assertEquals(ClientStatus.ENABLED, client.getStatus());
    assertNotNull(client.getClientCreationInfo());
  }

  @Test
  void testEnable() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");
    client.enable("modifier@example.com");

    assertEquals(ClientStatus.ENABLED, client.getStatus());
  }

  @Test
  void testDisable() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");

    assertEquals(ClientStatus.DISABLED, client.getStatus());
  }

  @Test
  void testInvalidate() {
    client.initialize("creator@example.com");
    client.invalidate("modifier@example.com");

    assertEquals(ClientStatus.INVALIDATED, client.getStatus());
  }

  @Test
  void testRegenerateSecret() {
    client.initialize("creator@example.com");
    client.disable(
        "modifier@example.com"); // Ensure client is in DISABLED state, not INVALIDATED state
    String oldSecret = client.getSecret().value();
    client.regenerateSecret("modifier@example.com");

    assertNotEquals(oldSecret, client.getSecret().value());
  }

  @Test
  void testAddScope() {
    client.initialize("creator@example.com");
    String newScope = "delete";
    client.addScope(newScope, "modifier@example.com");

    assertTrue(client.getScopes().contains(newScope));
  }

  @Test
  void testRemoveScope() {
    client.initialize("creator@example.com");
    String scopeToRemove = "read";
    client.removeScope(scopeToRemove, "modifier@example.com");

    assertFalse(client.getScopes().contains(scopeToRemove));
  }

  @Test
  void testRemoveLastScopeThrowsException() {
    client.initialize("creator@example.com");
    // Remove all but one scope first
    client.removeScope("read", "modifier@example.com");

    Exception exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeScope("write", "modifier@example.com");
            });

    assertEquals("Client must have at least one scope", exception.getMessage());
  }
}
