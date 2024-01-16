package com.asc.authorization.web.clients;

import com.asc.authorization.web.clients.transfer.APIClientDTOWrapper;
import com.asc.authorization.web.clients.transfer.PersonDTO;
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
}
