// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
@ToString
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
  private long tenantId;

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
