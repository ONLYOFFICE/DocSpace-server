package com.asc.authorization.api.web.security.context;

import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.SettingsDTO;

/**
 *
 */
public class SettingsContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<SettingsDTO>> context = new ThreadLocal<>();
}
