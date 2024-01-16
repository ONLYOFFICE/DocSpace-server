package com.asc.authorization.api.web.client;

import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.PersonDTO;
import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.client.transfer.SettingsDTO;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

import java.net.URI;

/**
 *
 */
@Slf4j
@Component
public class APIClientFallback implements APIClient {
    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    public APIClientDTOWrapper<PersonDTO> getMe(URI base, String cookie) {
        MDC.put("URL", base.toString());
        MDC.put("Fallback", APIClientFallback.class.getName());
        log.warn("Could not fetch current user profile");
        MDC.clear();
        return APIClientDTOWrapper.<PersonDTO>builder().build();
    }

    /**
     *
     * @param base
     * @param cookie
     * @param email
     * @return
     */
    public APIClientDTOWrapper<PersonDTO> getProfile(URI base, String cookie, String email) {
        MDC.put("URL", base.toString());
        MDC.put("Email", email);
        MDC.put("Fallback", APIClientFallback.class.getName());
        log.warn("Could not fetch user profile");
        MDC.clear();
        return APIClientDTOWrapper.<PersonDTO>builder().build();
    }

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    public APIClientDTOWrapper<TenantDTO> getTenant(URI base, String cookie) {
        MDC.put("URL", base.toString());
        MDC.put("Fallback", APIClientFallback.class.getName());
        log.warn("Could not fetch current tenant info");
        MDC.clear();
        return APIClientDTOWrapper.<TenantDTO>builder().build();
    }

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    public APIClientDTOWrapper<SettingsDTO> getSettings(URI base, String cookie) {
        MDC.put("URL", base.toString());
        MDC.put("Fallback", APIClientFallback.class.getName());
        log.warn("Could not fetch current tenant settings");
        MDC.clear();
        return APIClientDTOWrapper.<SettingsDTO>builder().build();
    }
}
