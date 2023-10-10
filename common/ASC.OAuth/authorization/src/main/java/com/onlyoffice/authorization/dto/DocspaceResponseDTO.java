package com.onlyoffice.authorization.dto;

import lombok.*;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class DocspaceResponseDTO<B> {
    private B response;
    private int status;
    private int statusCode;
}
