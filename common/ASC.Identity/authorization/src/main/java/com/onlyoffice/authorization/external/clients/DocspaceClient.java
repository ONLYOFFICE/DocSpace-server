/**
 *
 */
package com.onlyoffice.authorization.external.clients;

import com.onlyoffice.authorization.core.transfer.dto.DocspaceResponseDTO;
import com.onlyoffice.authorization.core.transfer.dto.MeDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;

import java.net.URI;

/**
 *
 */
@FeignClient(url = "http://localhost", value = "docspace", fallback = DocspaceClientFallback.class)
public interface DocspaceClient {
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    DocspaceResponseDTO<MeDTO> getMe(URI base, @RequestHeader("Cookie") String cookie);
}
