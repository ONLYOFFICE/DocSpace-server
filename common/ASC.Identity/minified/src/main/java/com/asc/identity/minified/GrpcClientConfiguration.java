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

package com.asc.identity.minified;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.ClientRegistrationServiceGrpc;
import net.devh.boot.grpc.client.channelfactory.GrpcChannelFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for setting up gRPC clients for the minified deployment.
 *
 * <p>This configuration provides blocking stubs for inter-service communication via gRPC using
 * in-process channels.
 */
@Configuration
public class GrpcClientConfiguration {
  private final GrpcChannelFactory grpcChannelFactory;

  /**
   * Creates a new gRPC client configuration for the minified.
   *
   * @param grpcChannelFactory factory responsible for creating managed gRPC channels
   */
  public GrpcClientConfiguration(GrpcChannelFactory grpcChannelFactory) {
    this.grpcChannelFactory = grpcChannelFactory;
  }

  /**
   * Provides a blocking stub for the authorization gRPC service.
   *
   * @return {@link AuthorizationServiceGrpc.AuthorizationServiceBlockingStub} connected to the
   *     {@code authorizationService} channel
   */
  @Bean
  public AuthorizationServiceGrpc.AuthorizationServiceBlockingStub
      authorizationServiceBlockingStub() {
    var managedChannel = grpcChannelFactory.createChannel("authorizationService");
    return AuthorizationServiceGrpc.newBlockingStub(managedChannel);
  }

  /**
   * Provides a blocking stub for the client registration gRPC service.
   *
   * @return {@link ClientRegistrationServiceGrpc.ClientRegistrationServiceBlockingStub} connected
   *     to the {@code registrationService} channel
   */
  @Bean
  public ClientRegistrationServiceGrpc.ClientRegistrationServiceBlockingStub
      clientRegistrationServiceBlockingStub() {
    var managedChannel = grpcChannelFactory.createChannel("registrationService");
    return ClientRegistrationServiceGrpc.newBlockingStub(managedChannel);
  }
}
