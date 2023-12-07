/**
 *
 */
package com.onlyoffice.authorization.api.web.client;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.MeDTO;
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
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    APIClientDTOWrapper<MeDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/email")
    APIClientDTOWrapper<MeDTO> getProfile(URI base, @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/portal")
    APIClientDTOWrapper<TenantDTO> getTenant(URI base, @RequestHeader("Cookie") String cookie);
}
