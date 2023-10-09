package com.onlyoffice.authorization.api.clients;

import com.onlyoffice.authorization.api.dto.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.dto.response.docspace.MeDTO;
import com.onlyoffice.authorization.api.dto.response.docspace.TenantDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;

@FeignClient(value = "docspace", url = "${docspace.server.url}")
public interface DocspaceClient {
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    DocspaceResponseDTO<MeDTO> getMe(@RequestHeader("Cookie") String cookie);
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/portal")
    DocspaceResponseDTO<TenantDTO> getTenant(@RequestHeader("Cookie") String cookie);
}
