/**
 *
 */
package com.onlyoffice.authorization.api.web.security.context;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.MeDTO;

/**
 *
 */
public class UserContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<MeDTO>> context = new ThreadLocal<>();
}
