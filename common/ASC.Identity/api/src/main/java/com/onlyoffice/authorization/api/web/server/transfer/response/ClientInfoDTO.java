package com.onlyoffice.authorization.api.web.server.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;

import java.io.Serializable;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ClientInfoDTO implements Serializable {
    private String name;
    private String logo;
    @JsonProperty("website_url")
    private String websiteUrl;
}
