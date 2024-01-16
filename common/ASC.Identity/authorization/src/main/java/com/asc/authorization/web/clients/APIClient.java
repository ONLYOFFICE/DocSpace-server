/**
 *
 */
package com.asc.authorization.web.clients;

import com.asc.authorization.web.clients.transfer.APIClientDTOWrapper;
import com.asc.authorization.web.clients.transfer.PersonDTO;
import org.springframework.cache.annotation.Cacheable;
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
    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    @Cacheable(cacheNames = {"ascMe"}, key = "{#cookie}", cacheManager = "ascClientCacheManager")
    APIClientDTOWrapper<PersonDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);
}
