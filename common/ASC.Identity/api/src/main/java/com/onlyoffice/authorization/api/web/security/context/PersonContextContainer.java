/**
 *
 */
package com.onlyoffice.authorization.api.web.security.context;

import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;

/**
 *
 */
public class PersonContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<PersonDTO>> context = new ThreadLocal<>();
}
