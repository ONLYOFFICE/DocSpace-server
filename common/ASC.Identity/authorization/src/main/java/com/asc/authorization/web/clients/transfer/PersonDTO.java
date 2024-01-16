/**
 *
 */
package com.asc.authorization.web.clients.transfer;

import lombok.*;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class PersonDTO {
    private String id;
    private String firstName;
    private String lastName;
    private String userName;
    private String email;
    private Boolean isAdmin;
}
