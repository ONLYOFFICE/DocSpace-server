/**
 *
 */
package com.asc.authorization.api.web.security.context;

import com.asc.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.asc.authorization.api.web.client.transfer.PersonDTO;

/**
 *
 */
public class PersonContextContainer {
    public static ThreadLocal<APIClientDTOWrapper<PersonDTO>> context = new ThreadLocal<>();
}
