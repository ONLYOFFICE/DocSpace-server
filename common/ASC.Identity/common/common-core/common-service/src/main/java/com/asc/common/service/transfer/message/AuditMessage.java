// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.common.service.transfer.message;

import java.io.Serializable;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Represents an audit message that captures details of an audit event.
 *
 * <p>This class encapsulates the details of an audit event including information about the
 * initiator, target, IP address, browser, platform, date, tenant ID, user details, page, action,
 * and description. It implements {@link Serializable} to allow instances to be serialized.
 */
@Builder
@Getter
@Setter
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class AuditMessage implements Serializable {

  /** A tag associated with the audit event. */
  private String tag;

  /** The initiator of the audit event. */
  private String initiator;

  /** The target of the audit event. */
  private String target;

  /** The IP address from where the audit event was triggered. */
  private String ip;

  /** The browser used during the audit event. */
  private String browser;

  /** The platform used during the audit event. */
  private String platform;

  /** The date and time when the audit event occurred. */
  private ZonedDateTime date;

  /** The tenant ID associated with the audit event. */
  private int tenantId;

  /** The email of the user involved in the audit event. */
  private String userEmail;

  /** The name of the user involved in the audit event. */
  private String userName;

  /** The ID of the user involved in the audit event. */
  private String userId;

  /** The page associated with the audit event. */
  private String page;

  /** The action performed during the audit event, represented as an integer. */
  private int action;

  /** A description of the audit event. */
  private String description;
}
