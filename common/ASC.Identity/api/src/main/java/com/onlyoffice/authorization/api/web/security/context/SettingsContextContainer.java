package com.onlyoffice.authorization.api.web.security.context;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.SettingsDTO;

public class SettingsContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<SettingsDTO>> context = new ThreadLocal<>();
}
