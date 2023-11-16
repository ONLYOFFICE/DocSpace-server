package com.onlyoffice.authorization.api.security.container;

import com.onlyoffice.authorization.api.core.transfer.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.TenantDTO;

public class TenantContextContainer {
    public static ThreadLocal<DocspaceResponseDTO<TenantDTO>> context = new ThreadLocal<>();
}
