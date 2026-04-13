// (c) Copyright Ascensio System SIA 2009-2026
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

package com.asc.registration.core.domain;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientWebsiteInfo;
import java.util.Set;
import java.util.function.BiConsumer;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.mockito.ArgumentCaptor;

class CoreClientDomainServiceTest {
  private final String USER_ID = "userId";
  private CoreClientDomainService service;
  private Audit audit;
  private Client client;

  @BeforeEach
  void setUp() {
    service = new CoreClientDomainService();
    audit = mock(Audit.class);
    client = mock(Client.class);

    when(audit.getUserId()).thenReturn(USER_ID);
  }

  @FunctionalInterface
  interface ClientCommand {
    ClientEvent execute(CoreClientDomainService service, Audit audit, Client client);
  }

  static Stream<Arguments> clientCommandCases() {
    var clientInfo = new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");
    var clientInfo2 =
        new ClientInfo("Updated Client 2", "Updated Description 2", "Updated Logo URL 2");
    var clientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.website")
            .termsUrl("http://updated.terms")
            .policyUrl("http://updated.policy")
            .build();
    var clientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect"),
            Set.of("http://updated.origin"),
            Set.of("http://updated.logout"));
    var method = AuthenticationMethod.DEFAULT_AUTHENTICATION;
    var pkceMethod = AuthenticationMethod.PKCE_AUTHENTICATION;
    var addedScope = "newScope";
    var removedScope = "existingScope";
    var addedScope2 = "adminScope";
    var removedScope2 = "revokedScope";

    return Stream.of(
        Arguments.of(
            (ClientCommand) CoreClientDomainService::enableClient,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).enable(captor.capture())),
        Arguments.of(
            (ClientCommand) CoreClientDomainService::disableClient,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).disable(captor.capture())),
        Arguments.of(
            (ClientCommand) CoreClientDomainService::deleteClient,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).disable(captor.capture())),
        Arguments.of(
            (ClientCommand) CoreClientDomainService::regenerateClientSecret,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).regenerateSecret(captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.updateClientInfo(audit, client, clientInfo),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).updateClientInfo(eq(clientInfo), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.updateClientInfo(audit, client, clientInfo2),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).updateClientInfo(eq(clientInfo2), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) ->
                    service.updateClientWebsiteInfo(audit, client, clientWebsiteInfo),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client)
                        .updateClientWebsiteInfo(eq(clientWebsiteInfo), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) ->
                    service.updateClientRedirectInfo(audit, client, clientRedirectInfo),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client)
                        .updateClientRedirectInfo(eq(clientRedirectInfo), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.addAuthenticationMethod(audit, client, method),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).addAuthenticationMethod(eq(method), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) ->
                    service.addAuthenticationMethod(audit, client, pkceMethod),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).addAuthenticationMethod(eq(pkceMethod), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) ->
                    service.removeAuthenticationMethod(audit, client, method),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).removeAuthenticationMethod(eq(method), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) ->
                    service.removeAuthenticationMethod(audit, client, pkceMethod),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).removeAuthenticationMethod(eq(pkceMethod), captor.capture())),
        Arguments.of(
            (ClientCommand) (service, audit, client) -> service.addScope(audit, client, addedScope),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).addScope(eq(addedScope), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.addScope(audit, client, addedScope2),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).addScope(eq(addedScope2), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.removeScope(audit, client, removedScope),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) -> verify(client).removeScope(eq(removedScope), captor.capture())),
        Arguments.of(
            (ClientCommand)
                (service, audit, client) -> service.removeScope(audit, client, removedScope2),
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).removeScope(eq(removedScope2), captor.capture())),
        Arguments.of(
            (ClientCommand) CoreClientDomainService::makeClientPublic,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client).changeVisibility(eq(ClientVisibility.PUBLIC), captor.capture())),
        Arguments.of(
            (ClientCommand) CoreClientDomainService::makeClientPrivate,
            (BiConsumer<Client, ArgumentCaptor<UserId>>)
                (client, captor) ->
                    verify(client)
                        .changeVisibility(eq(ClientVisibility.PRIVATE), captor.capture())));
  }

  @Test
  void whenClientIsCreated_thenEventIsGenerated() {
    var captor = ArgumentCaptor.forClass(UserId.class);
    var event = service.createClient(audit, client);

    verify(client).initialize(captor.capture());

    assertEquals(USER_ID, captor.getValue().getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }

  @ParameterizedTest
  @MethodSource("clientCommandCases")
  void whenClientCommandIsExecuted_thenEventIsGenerated(
      ClientCommand command, BiConsumer<Client, ArgumentCaptor<UserId>> verifier) {
    var captor = ArgumentCaptor.forClass(UserId.class);
    var event = command.execute(service, audit, client);
    verifier.accept(client, captor);

    assertEquals(USER_ID, captor.getValue().getValue());
    assertNotNull(event);
    assertEquals(client, event.getClient());
    assertEquals(audit, event.getAudit());
    assertNotNull(event.getEventAt());
  }
}
