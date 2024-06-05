package com.asc.common.application.transfer.response;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscPersonResponse {
  private String id;
  private String firstName;
  private String lastName;
  private String userName;
  private String email;
  private String avatar;
  private String avatarSmall;
  private boolean isAdmin;
}
