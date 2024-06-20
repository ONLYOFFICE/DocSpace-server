package com.asc.common.application.transfer.response;

import java.io.Serializable;
import lombok.*;

/** A data transfer object representing the response for a person. */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscPersonResponse implements Serializable {

  /** The unique identifier for the person. */
  private String id;

  /** The first name of the person. */
  private String firstName;

  /** The last name of the person. */
  private String lastName;

  /** The username of the person. */
  private String userName;

  /** The email address of the person. */
  private String email;

  /** The URL to the person's avatar image. */
  private String avatar;

  /** The URL to the person's small avatar image. */
  private String avatarSmall;

  /** Indicates whether the person is the owner. */
  private Boolean isOwner;

  /** Indicates whether the person is an admin. */
  private Boolean isAdmin;
}
