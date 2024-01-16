package com.asc.authorization.api.web.security.context;

import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.TenantDTO;

/**
 *
 */
public class TenantContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<TenantDTO>> context = new ThreadLocal<>();
}
