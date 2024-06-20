package com.asc.registration.core.domain.value;

import com.asc.common.core.domain.value.TenantId;

/**
 * ClientTenantInfo is a value object that holds information about the client's tenant. It contains
 * the tenant's identifier and the tenant's URL.
 *
 * @param tenantId the identifier of the tenant
 */
public record ClientTenantInfo(TenantId tenantId) {}
