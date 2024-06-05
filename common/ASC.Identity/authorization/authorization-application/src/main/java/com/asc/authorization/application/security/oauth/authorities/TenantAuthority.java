package com.asc.authorization.application.security.oauth.authorities;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.annotation.JsonDeserialize;
import java.io.Serializable;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.security.core.GrantedAuthority;

/** Represents a tenant authority for OAuth2 authorization. */
@Getter
@Setter
@JsonDeserialize
@NoArgsConstructor
@AllArgsConstructor
public class TenantAuthority implements GrantedAuthority, Serializable {
  /** The URL of the tenant. */
  @JsonProperty(value = "tenant_url")
  private String tenantUrl;

  /**
   * Returns the authority granted to the tenant.
   *
   * @return the tenant URL as the authority.
   */
  @Override
  @JsonIgnore
  public String getAuthority() {
    return tenantUrl;
  }
}
