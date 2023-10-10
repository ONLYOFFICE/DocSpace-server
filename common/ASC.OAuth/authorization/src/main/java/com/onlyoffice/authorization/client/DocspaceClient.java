package com.onlyoffice.authorization.client;

import com.onlyoffice.authorization.dto.DocspaceResponseDTO;
import com.onlyoffice.authorization.dto.MeDTO;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;

@FeignClient(value = "docspace", url = "${docspace.server.url}")
public interface DocspaceClient {
    @RequestMapping(method = RequestMethod.GET, value = "/api/2.0/people/@self")
    DocspaceResponseDTO<MeDTO> getMe(@RequestHeader("Cookie") String cookie);
}
