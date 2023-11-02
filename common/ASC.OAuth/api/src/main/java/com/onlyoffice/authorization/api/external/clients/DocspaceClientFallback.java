package com.onlyoffice.authorization.api.external.clients;

import com.onlyoffice.authorization.api.core.transfer.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.MeDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.TenantDTO;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.net.URI;

@Component
@Slf4j
public class DocspaceClientFallback implements DocspaceClient {
    @Override
    public DocspaceResponseDTO<MeDTO> getMe(URI base, String cookie) {
        return DocspaceResponseDTO.<MeDTO>builder().build();
    }

    @Override
    public DocspaceResponseDTO<MeDTO> getProfile(URI base, String cookie, String email) {
        return DocspaceResponseDTO.<MeDTO>builder().build();
    }

    @Override
    public DocspaceResponseDTO<TenantDTO> getTenant(URI base, String cookie) {
        return DocspaceResponseDTO.<TenantDTO>builder().build();
    }
}
