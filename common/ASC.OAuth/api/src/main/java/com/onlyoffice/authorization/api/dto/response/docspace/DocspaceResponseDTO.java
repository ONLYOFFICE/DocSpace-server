package com.onlyoffice.authorization.api.dto.response.docspace;

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
