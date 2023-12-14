package com.onlyoffice.authorization.api.web.client;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

import java.net.URI;

@Component
@Slf4j
public class APIClientFallback implements APIClient {
    @Override
    public APIClientDTOWrapper<PersonDTO> getMe(URI base, String cookie) {
        MDC.put("URL", base.toString());
        log.warn("Get me fallback");
        MDC.clear();
        return APIClientDTOWrapper.<PersonDTO>builder().build();
    }

    @Override
    public APIClientDTOWrapper<PersonDTO> getProfile(URI base, String cookie, String email) {
        MDC.put("URL", base.toString());
        log.warn("Get profile fallback");
        MDC.clear();
        return APIClientDTOWrapper.<PersonDTO>builder().build();
    }

    @Override
    public APIClientDTOWrapper<TenantDTO> getTenant(URI base, String cookie) {
        MDC.put("URL", base.toString());
        log.warn("Get tenant fallback");
        MDC.clear();
        return APIClientDTOWrapper.<TenantDTO>builder().build();
    }
}
