/**
 *
 */
package com.onlyoffice.authorization.api.external.clients;

import com.onlyoffice.authorization.api.core.transfer.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.MeDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.TenantDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RequestParam;

import java.net.URI;

/**
 *
 */
@FeignClient(url = "http://localhost", value = "docspace", fallback = DocspaceClientFallback.class)
public interface DocspaceClient {
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    DocspaceResponseDTO<MeDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);
    @RequestMapping(method = RequestMethod.GET, value = "api/2.0/people/email")
    DocspaceResponseDTO<MeDTO> getProfile(URI base, @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/portal")
    DocspaceResponseDTO<TenantDTO> getTenant(URI base, @RequestHeader("Cookie") String cookie);
}
