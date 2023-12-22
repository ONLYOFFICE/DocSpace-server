package com.onlyoffice.authorization.web.security.oauth.authorities;

import lombok.AllArgsConstructor;
import org.springframework.security.core.GrantedAuthority;

import java.io.Serializable;

/**
 *
 */
@AllArgsConstructor
public class TenantAuthority implements GrantedAuthority, Serializable {
    private String tenantUrl;
    public String getAuthority() {
        return tenantUrl;
    }
}
