package com.onlyoffice.authorization.dto;

import lombok.*;

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
    private Boolean isAdmin;
}
