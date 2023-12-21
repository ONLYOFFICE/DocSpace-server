/**
 *
 */
package com.onlyoffice.authorization.api.web.client;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.SettingsDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RequestParam;

import java.net.URI;

/**
 *
 */
@FeignClient(url = "http://localhost", value = "asc", fallback = APIClientFallback.class)
public interface APIClient {
    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    APIClientDTOWrapper<PersonDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);

    /**
     *
     * @param base
     * @param cookie
     * @param email
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/email")
    APIClientDTOWrapper<PersonDTO> getProfile(URI base, @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/portal")
    APIClientDTOWrapper<TenantDTO> getTenant(URI base, @RequestHeader("Cookie") String cookie);

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/settings")
    APIClientDTOWrapper<SettingsDTO> getSettings(URI base, @RequestHeader("Cookie") String cookie);
}
