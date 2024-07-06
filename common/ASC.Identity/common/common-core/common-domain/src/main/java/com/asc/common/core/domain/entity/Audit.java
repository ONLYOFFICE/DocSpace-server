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

package com.asc.common.core.domain.entity;

import com.asc.common.core.domain.exception.AuditDomainException;
import com.asc.common.core.domain.value.enums.AuditCode;

/**
 * This class represents an audit event in the system. It contains information about the event, such
 * as the audit code, initiator, target, IP address, browser, platform, tenant ID, user email, user
 * name, user ID, page, and description.
 */
public class Audit extends BaseEntity<Integer> {

  /** The audit code for the event. */
  private final AuditCode auditCode;

  /** The initiator of the event. This is an optional field. */
  private final String initiator;

  /** The target of the event. This is an optional field. */
  private final String target;

  /** The IP address of the user who initiated the event. */
  private final String ip;

  /** The browser used by the user who initiated the event. */
  private final String browser;

  /** The platform used by the user who initiated the event. */
  private final String platform;

  /** The ID of the tenant associated with the event. */
  private final int tenantId;

  /** The email of the user who initiated the event. */
  private final String userEmail;

  /** The name of the user who initiated the event. */
  private final String userName;

  /** The ID of the user who initiated the event. */
  private final String userId;

  /** The page where the event occurred. */
  private final String page;

  /** A description of the event. This is an optional field. */
  private final String description;

  /**
   * Constructs a new Audit object with the given parameters.
   *
   * @param builder The builder object containing the parameters for the Audit object.
   */
  private Audit(Builder builder) {
    super.setId(0);
    auditCode = builder.auditCode;
    initiator = builder.initiator;
    target = builder.target;
    ip = builder.ip;
    browser = builder.browser;
    platform = builder.platform;
    tenantId = builder.tenantId;
    userEmail = builder.userEmail;
    userName = builder.userName;
    userId = builder.userId;
    page = builder.page;
    description = builder.description;
    validate();
  }

  /**
   * Returns the audit code for the event.
   *
   * @return The audit code.
   */
  public AuditCode getAuditCode() {
    return auditCode;
  }

  /**
   * Returns the initiator of the event.
   *
   * @return The initiator.
   */
  public String getInitiator() {
    return initiator;
  }

  /**
   * Returns the target of the event.
   *
   * @return The target.
   */
  public String getTarget() {
    return target;
  }

  /**
   * Returns the IP address of the user who initiated the event.
   *
   * @return The IP address.
   */
  public String getIp() {
    return ip;
  }

  /**
   * Returns the browser used by the user who initiated the event.
   *
   * @return The browser.
   */
  public String getBrowser() {
    return browser;
  }

  /**
   * Returns the platform used by the user who initiated the event.
   *
   * @return The platform.
   */
  public String getPlatform() {
    return platform;
  }

  /**
   * Returns the ID of the tenant associated with the event.
   *
   * @return The tenant ID.
   */
  public int getTenantId() {
    return tenantId;
  }

  /**
   * Returns the email of the user who initiated the event.
   *
   * @return The user email.
   */
  public String getUserEmail() {
    return userEmail;
  }

  /**
   * Returns the name of the user who initiated the event.
   *
   * @return The user name.
   */
  public String getUserName() {
    return userName;
  }

  /**
   * Returns the ID of the user who initiated the event.
   *
   * @return The user ID.
   */
  public String getUserId() {
    return userId;
  }

  /**
   * Returns the page where the event occurred.
   *
   * @return The page.
   */
  public String getPage() {
    return page;
  }

  /**
   * Returns the description of the event.
   *
   * @return The description.
   */
  public String getDescription() {
    return description;
  }

  /**
   * Validates the Audit object to ensure that all required fields are present. If any required
   * field is missing, an AuditDomainException is thrown.
   */
  private void validate() {
    if (ip == null || ip.isBlank()) throw new AuditDomainException("Sender must have ip");
    if (browser == null || browser.isBlank())
      throw new AuditDomainException("Sender must have browser or user agent");
    if (platform == null || platform.isBlank())
      throw new AuditDomainException("Sender must have platform");
    if (tenantId < 1) throw new AuditDomainException("Sender must have a valid tenant id");
    if (userEmail == null || userEmail.isBlank())
      throw new AuditDomainException("Sender must have a valid email");
    if (userName == null || userName.isBlank())
      throw new AuditDomainException("Sender must have a valid name");
    if (userId == null || userId.isBlank())
      throw new AuditDomainException("Sender must have a valid user id");
    if (page == null || page.isBlank())
      throw new AuditDomainException("Sender must have a valid page");
    if (auditCode == null) throw new AuditDomainException("Sender must provide audit code");
  }

  /** The builder class for constructing Audit objects. */
  public static final class Builder {
    private AuditCode auditCode;
    private String initiator;
    private String target;
    private String ip;
    private String browser;
    private String platform;
    private int tenantId;
    private String userEmail;
    private String userName;
    private String userId;
    private String page;
    private String description;

    /** Constructs a new Builder object. */
    private Builder() {}

    /**
     * Returns a new Builder object.
     *
     * @return The new Builder object.
     */
    public static Builder builder() {
      return new Builder();
    }

    /**
     * Sets the audit code for the event.
     *
     * @param val The audit code.
     * @return The Builder object.
     */
    public Builder auditCode(AuditCode val) {
      auditCode = val;
      return this;
    }

    /**
     * Sets the initiator of the event.
     *
     * @param val The initiator.
     * @return The Builder object.
     */
    public Builder initiator(String val) {
      initiator = val;
      return this;
    }

    /**
     * Sets the target of the event.
     *
     * @param val The target.
     * @return The Builder object.
     */
    public Builder target(String val) {
      target = val;
      return this;
    }

    /**
     * Sets the IP address of the user who initiated the event.
     *
     * @param val The IP address.
     * @return The Builder object.
     */
    public Builder ip(String val) {
      ip = val;
      return this;
    }

    /**
     * Sets the browser used by the user who initiated the event.
     *
     * @param val The browser.
     * @return The Builder object.
     */
    public Builder browser(String val) {
      browser = val;
      return this;
    }

    /**
     * Sets the platform used by the user who initiated the event.
     *
     * @param val The platform.
     * @return The Builder object.
     */
    public Builder platform(String val) {
      platform = val;
      return this;
    }

    /**
     * Sets the ID of the tenant associated with the event.
     *
     * @param val The tenant ID.
     * @return The Builder object.
     */
    public Builder tenantId(int val) {
      tenantId = val;
      return this;
    }

    /**
     * Sets the email of the user who initiated the event.
     *
     * @param val The user email.
     * @return The Builder object.
     */
    public Builder userEmail(String val) {
      userEmail = val;
      return this;
    }

    /**
     * Sets the name of the user who initiated the event.
     *
     * @param val The user name.
     * @return The Builder object.
     */
    public Builder userName(String val) {
      userName = val;
      return this;
    }

    /**
     * Sets the ID of the user who initiated the event.
     *
     * @param val The user ID.
     * @return The Builder object.
     */
    public Builder userId(String val) {
      userId = val;
      return this;
    }

    /**
     * Sets the page where the event occurred.
     *
     * @param val The page.
     * @return The Builder object.
     */
    public Builder page(String val) {
      page = val;
      return this;
    }

    /**
     * Sets the description of the event.
     *
     * @param val The description.
     * @return The Builder object.
     */
    public Builder description(String val) {
      description = val;
      return this;
    }

    /**
     * Constructs a new Audit object with the parameters set in the Builder object.
     *
     * @return The new Audit object.
     */
    public Audit build() {
      return new Audit(this);
    }
  }
}
