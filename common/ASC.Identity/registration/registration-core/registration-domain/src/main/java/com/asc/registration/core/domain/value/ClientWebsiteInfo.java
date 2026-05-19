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

package com.asc.registration.core.domain.value;

/**
 * ClientWebsiteInfo is a value object that holds information about the client's website, including
 * the website URL, terms of service URL, and privacy policy URL.
 */
public class ClientWebsiteInfo {
  private final String websiteUrl;
  private final String termsUrl;
  private final String policyUrl;

  private ClientWebsiteInfo(Builder builder) {
    this.websiteUrl = builder.websiteUrl;
    this.termsUrl = builder.termsUrl;
    this.policyUrl = builder.policyUrl;
  }

  /**
   * Returns the URL of the client's website.
   *
   * @return the website URL
   */
  public String getWebsiteUrl() {
    return this.websiteUrl;
  }

  /**
   * Returns the URL of the client's terms of service.
   *
   * @return the terms of service URL
   */
  public String getTermsUrl() {
    return this.termsUrl;
  }

  /**
   * Returns the URL of the client's privacy policy.
   *
   * @return the privacy policy URL
   */
  public String getPolicyUrl() {
    return this.policyUrl;
  }

  /** Builder class for constructing instances of {@link ClientWebsiteInfo}. */
  public static final class Builder {
    private String websiteUrl;
    private String termsUrl;
    private String policyUrl;

    private Builder() {}

    /**
     * Creates a new Builder instance.
     *
     * @return a new Builder
     */
    public static Builder builder() {
      return new Builder();
    }

    /**
     * Sets the website URL.
     *
     * @param val the website URL
     * @return the Builder instance
     */
    public Builder websiteUrl(String val) {
      this.websiteUrl = val;
      return this;
    }

    /**
     * Sets the terms of service URL.
     *
     * @param val the terms of service URL
     * @return the Builder instance
     */
    public Builder termsUrl(String val) {
      this.termsUrl = val;
      return this;
    }

    /**
     * Sets the privacy policy URL.
     *
     * @param val the privacy policy URL
     * @return the Builder instance
     */
    public Builder policyUrl(String val) {
      this.policyUrl = val;
      return this;
    }

    /**
     * Builds and returns a new {@link ClientWebsiteInfo} instance.
     *
     * @return a new ClientWebsiteInfo instance
     */
    public ClientWebsiteInfo build() {
      return new ClientWebsiteInfo(this);
    }
  }
}
