/**
 *
 */
package com.onlyoffice.authorization.api.security.container;

import com.onlyoffice.authorization.api.core.transfer.response.docspace.DocspaceResponseDTO;
import com.onlyoffice.authorization.api.core.transfer.response.docspace.MeDTO;

/**
 *
 */
public class UserContextContainer {
    public static ThreadLocal<DocspaceResponseDTO<MeDTO>> context = new ThreadLocal<>();
}
