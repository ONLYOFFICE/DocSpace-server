/**
 *
 */
package com.asc.authorization.api.web.client.transfer;

import lombok.*;

/**
 *
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class PersonDTO {
    private String id;
    private String firstName;
    private String lastName;
    private String userName;
    private String email;
    private String avatar;
    private String avatarSmall;
    private Boolean isAdmin;
}