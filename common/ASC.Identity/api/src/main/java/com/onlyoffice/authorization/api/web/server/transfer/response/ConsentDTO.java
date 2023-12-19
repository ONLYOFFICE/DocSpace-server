package com.onlyoffice.authorization.api.web.server.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import lombok.Getter;
import lombok.Setter;

import java.time.ZonedDateTime;

@Getter
@Setter
@Data
public class ConsentDTO {
    @JsonProperty("registered_client_id")
    private String registeredClientId;
    @JsonProperty("principal_name")
    private String principalName;
    private String scopes;
    @JsonProperty("modified_at")
    private ZonedDateTime modifiedAt;
    private boolean invalidated;
    private ClientDTO client;
}
