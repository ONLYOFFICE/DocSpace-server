package com.onlyoffice.authorization.api.dto.response;

import lombok.*;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ErrorDTO {
    private String reason;
}
