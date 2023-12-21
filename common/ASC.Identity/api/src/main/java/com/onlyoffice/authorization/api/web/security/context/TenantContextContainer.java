package com.onlyoffice.authorization.api.web.security.context;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;

/**
 *
 */
public class TenantContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<TenantDTO>> context = new ThreadLocal<>();
}
