package com.onlyoffice.authorization.api.core.transfer.response;

import lombok.Data;
import lombok.Getter;
import lombok.Setter;

import java.sql.Timestamp;

@Getter
@Setter
@Data
public class ConsentDTO {
    private String registeredClientId;
    private String principalName;
    private String scopes;
    private Timestamp modifiedAt;
    private ClientDTO client;
}
