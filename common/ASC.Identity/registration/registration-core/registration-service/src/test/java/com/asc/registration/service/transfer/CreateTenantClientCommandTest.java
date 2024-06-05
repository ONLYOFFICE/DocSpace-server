package com.asc.registration.service.transfer;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validation;
import jakarta.validation.Validator;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class CreateTenantClientCommandTest {
  private Validator validator;

  @BeforeEach
  void setUp() {
    var factory = Validation.buildDefaultValidatorFactory();
    validator = factory.getValidator();
  }

  @Test
  void testValidCreateTenantClientCommand() {
    CreateTenantClientCommand command =
        CreateTenantClientCommand.builder()
            .tenantId(1)
            .tenantUrl("http://tenant.url")
            .name("Valid Client")
            .description("A valid description")
            .logo(
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAoAAAAHgCAIAAABGjpL1AAAAGXRFWHRTb2Z0d2FyZQBNYX")
            .allowPkce(true)
            .websiteUrl("http://website.url")
            .termsUrl("http://terms.url")
            .policyUrl("http://policy.url")
            .redirectUris(Set.of("http://redirect1.url", "http://redirect2.url"))
            .allowedOrigins(Set.of("http://allowed.origin"))
            .logoutRedirectUri("http://logout.url")
            .scopes(Set.of("read", "write"))
            .build();

    Set<ConstraintViolation<CreateTenantClientCommand>> violations = validator.validate(command);
    assertTrue(violations.isEmpty());
  }

  @Test
  void testInvalidTenantId() {
    CreateTenantClientCommand command =
        CreateTenantClientCommand.builder()
            .tenantId(0)
            .tenantUrl("http://tenant.url")
            .name("Valid Client")
            .description("A valid description")
            .logo(
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAoAAAAHgCAIAAABGjpL1AAAAGXRFWHRTb2Z0d2FyZQBNYX")
            .allowPkce(true)
            .websiteUrl("http://website.url")
            .termsUrl("http://terms.url")
            .policyUrl("http://policy.url")
            .redirectUris(Set.of("http://redirect1.url", "http://redirect2.url"))
            .allowedOrigins(Set.of("http://allowed.origin"))
            .logoutRedirectUri("http://logout.url")
            .scopes(Set.of("read", "write"))
            .build();

    Set<ConstraintViolation<CreateTenantClientCommand>> violations = validator.validate(command);
    assertFalse(violations.isEmpty());
    assertEquals(
        "tenant id must be greater than or equal to 1", violations.iterator().next().getMessage());
  }

  @Test
  void testEmptyName() {
    CreateTenantClientCommand command =
        CreateTenantClientCommand.builder()
            .tenantId(1)
            .tenantUrl("http://tenant.url")
            .name("")
            .description("A valid description")
            .logo(
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAoAAAAHgCAIAAABGjpL1AAAAGXRFWHRTb2Z0d2FyZQBNYX")
            .allowPkce(true)
            .websiteUrl("http://website.url")
            .termsUrl("http://terms.url")
            .policyUrl("http://policy.url")
            .redirectUris(Set.of("http://redirect1.url", "http://redirect2.url"))
            .allowedOrigins(Set.of("http://allowed.origin"))
            .logoutRedirectUri("http://logout.url")
            .scopes(Set.of("read", "write"))
            .build();

    Set<ConstraintViolation<CreateTenantClientCommand>> violations = validator.validate(command);
    assertFalse(violations.isEmpty());
  }

  @Test
  void testNameSize() {
    CreateTenantClientCommand command =
        CreateTenantClientCommand.builder()
            .tenantId(1)
            .tenantUrl("http://tenant.url")
            .name("ab") // Invalid length
            .description("A valid description")
            .logo(
                "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAoAAAAHgCAIAAABGjpL1AAAAGXRFWHRTb2Z0d2FyZQBNYX")
            .allowPkce(true)
            .websiteUrl("http://website.url")
            .termsUrl("http://terms.url")
            .policyUrl("http://policy.url")
            .redirectUris(Set.of("http://redirect1.url", "http://redirect2.url"))
            .allowedOrigins(Set.of("http://allowed.origin"))
            .logoutRedirectUri("http://logout.url")
            .scopes(Set.of("read", "write"))
            .build();

    Set<ConstraintViolation<CreateTenantClientCommand>> violations = validator.validate(command);
    assertFalse(violations.isEmpty());
    assertEquals(
        "client name length is expected to be between 3 and 256 characters",
        violations.iterator().next().getMessage());
  }
}
