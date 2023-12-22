/**
 *
 */
package com.onlyoffice.authorization.web.clients;

import com.onlyoffice.authorization.web.clients.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.web.clients.transfer.PersonDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;

import java.net.URI;

/**
 *
 */
@FeignClient(url = "http://localhost", value = "asc", fallback = APIClientFallback.class)
public interface APIClient {
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    APIClientDTOWrapper<PersonDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);
}