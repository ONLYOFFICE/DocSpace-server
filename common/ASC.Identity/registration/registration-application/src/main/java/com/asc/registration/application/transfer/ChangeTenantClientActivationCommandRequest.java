package com.asc.registration.application.transfer;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.io.Serializable;
import lombok.*;

/**
 * ChangeTenantClientActivationCommandRequest is a data transfer object (DTO) used in the REST
 * layer. It represents a request to change the activation status of a tenant's client. This class
 * contains the necessary information to change the client's activation status. It implements {@link
 * Serializable} to allow instances of this class to be serialized.
 *
 * <p>The class is annotated with Lombok annotations to generate boilerplate code:
 *
 * <ul>
 *   <li>{@link Getter} - Generates getter methods for all fields.
 *   <li>{@link Setter} - Generates setter methods for all fields.
 *   <li>{@link Builder} - Implements the builder pattern for object creation.
 *   <li>{@link NoArgsConstructor} - Generates a no-argument constructor.
 *   <li>{@link AllArgsConstructor} - Generates an all-arguments constructor.
 * </ul>
 *
 * Example usage:
 *
 * <pre>{@code
 * ChangeTenantClientActivationCommandRequest request = ChangeTenantClientActivationCommandRequest.builder()
 *     .enabled(true)
 *     .build();
 * }</pre>
 *
 * @see Serializable
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ChangeTenantClientActivationCommandRequest implements Serializable {
  /** Indicates whether the client's activation status is enabled or disabled. */
  @JsonProperty("status")
  private boolean enabled;
}
