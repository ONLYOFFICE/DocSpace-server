package com.onlyoffice.authorization.web.security.oauth.authorities;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.annotation.JsonDeserialize;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.security.core.GrantedAuthority;

import java.io.Serializable;

/**
 *
 */
@Getter
@Setter
@JsonDeserialize
@NoArgsConstructor
@AllArgsConstructor
public class TenantAuthority implements GrantedAuthority, Serializable {
    @JsonProperty(value = "tenantUrl")
    private String tenantUrl;
    @JsonIgnore
    public String getAuthority() {
        return tenantUrl;
    }
}
