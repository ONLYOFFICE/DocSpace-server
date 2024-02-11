/**
 *
 */
package com.asc.authorization.api.web.client;

import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.PersonDTO;
import com.asc.authorization.api.web.client.transfer.SettingsDTO;
import com.asc.authorization.api.web.client.transfer.TenantDTO;
import org.springframework.cache.annotation.Cacheable;
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
    @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/@self")
    @Cacheable(cacheNames = {"ascMe"}, key = "{#cookie}", cacheManager = "ascClientCacheManager")
    APIClientDTOWrapper<PersonDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);

    /**
     *
     * @param base
     * @param cookie
     * @param email
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/email")
    @Cacheable(cacheNames = {"ascEmail"}, key = "{#email}", cacheManager = "ascClientCacheManager")
    APIClientDTOWrapper<PersonDTO> getProfile(URI base, @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "${web.api}/portal")
    @Cacheable(cacheNames = {"ascTenant"}, key = "{#cookie}", cacheManager = "ascClientCacheManager")
    APIClientDTOWrapper<TenantDTO> getTenant(URI base, @RequestHeader("Cookie") String cookie);

    /**
     *
     * @param base
     * @param cookie
     * @return
     */
    @RequestMapping(method = RequestMethod.GET, value = "${web.api}/settings")
    @Cacheable(cacheNames = {"ascSettings"}, key = "{#cookie}", cacheManager = "ascClientCacheManager")
    APIClientDTOWrapper<SettingsDTO> getSettings(URI base, @RequestHeader("Cookie") String cookie);
}
