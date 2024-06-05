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
    websiteUrl = builder.websiteUrl;
    termsUrl = builder.termsUrl;
    policyUrl = builder.policyUrl;
  }

  /**
   * Returns the URL of the client's website.
   *
   * @return the website URL
   */
  public String getWebsiteUrl() {
    return websiteUrl;
  }

  /**
   * Returns the URL of the client's terms of service.
   *
   * @return the terms of service URL
   */
  public String getTermsUrl() {
    return termsUrl;
  }

  /**
   * Returns the URL of the client's privacy policy.
   *
   * @return the privacy policy URL
   */
  public String getPolicyUrl() {
    return policyUrl;
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
      websiteUrl = val;
      return this;
    }

    /**
     * Sets the terms of service URL.
     *
     * @param val the terms of service URL
     * @return the Builder instance
     */
    public Builder termsUrl(String val) {
      termsUrl = val;
      return this;
    }

    /**
     * Sets the privacy policy URL.
     *
     * @param val the privacy policy URL
     * @return the Builder instance
     */
    public Builder policyUrl(String val) {
      policyUrl = val;
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
