/**
 *
 */
package com.onlyoffice.authorization.web.transfer.dto;

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
    private String id;
    private String firstName;
    private String lastName;
    private String userName;
    private String email;
    private Boolean isAdmin;
}
