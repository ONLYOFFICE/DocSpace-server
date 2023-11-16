/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.response.docspace;

import lombok.*;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class MeDTO {
    private String firstName;
    private String lastName;
    private String userName;
    private String email;
    private String avatar;
    private String avatarSmall;
    private Boolean isAdmin;
}
