package com.asc.common.application.transfer.response;

import java.io.Serializable;
import lombok.*;

/** A response class that represents the settings associated with a user or a system. */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscSettingsResponse implements Serializable {

  /** The timezone setting. */
  private String timezone;
}
