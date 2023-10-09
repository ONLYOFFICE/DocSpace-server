package com.onlyoffice.authorization.api.dto.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotNull;
import lombok.*;

import java.io.Serializable;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ChangeClientActivationDTO implements Serializable {
    @JsonProperty("enabled")
    @NotNull(message = "enabled field is expected to be set")
    private Boolean enabled;
}
