package com.onlyoffice.authorization.external.clients;

import com.onlyoffice.authorization.core.transfer.dto.DocspaceResponseDTO;
import com.onlyoffice.authorization.core.transfer.dto.MeDTO;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

import java.net.URI;

@Component
@Slf4j
public class DocspaceClientFallback implements DocspaceClient {
    @Override
    public DocspaceResponseDTO<MeDTO> getMe(URI base, String cookie) {
        MDC.put("URL", base.toString());
        log.warn("Get me fallback");
        MDC.clear();
        return DocspaceResponseDTO.<MeDTO>builder().build();
    }
}
