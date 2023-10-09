package com.onlyoffice.authorization.api.unit.controllers;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.controllers.ClientController;
import com.onlyoffice.authorization.api.dto.request.CreateClientDTO;
import com.onlyoffice.authorization.api.dto.response.ClientDTO;
import com.onlyoffice.authorization.api.dto.response.PaginationDTO;
import com.onlyoffice.authorization.api.services.ClientService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.test.json.JacksonTester;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockMvcRequestBuilders;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.util.List;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;

@ExtendWith(MockitoExtension.class)
public class ClientControllerTest {
    private MockMvc mvc;
    @Mock
    private ClientService clientService;
    @Mock
    private ApplicationConfiguration applicationConfiguration;
    @InjectMocks
    private ClientController clientController;

    private JacksonTester<CreateClientDTO> jsonClient;

    @BeforeEach
    void setup() {
        JacksonTester.initFields(this, new ObjectMapper());
        mvc = MockMvcBuilders.standaloneSetup(clientController)
                .build();
    }

    @Test
    void canRetrieve() throws Exception {
        given(clientService.getTenantClients(1, 0, 5))
                .willReturn(PaginationDTO.builder()
                        .data(List.of(ClientDTO.builder().clientId("mock").tenant(1).build()))
                        .limit(5)
                        .page(0)
                        .build());
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.get("/api/2.0/clients")
                                .header("X-Tenant", 1)
                                .queryParam("page", "0")
                                .queryParam("limit", "5")
                                .accept(MediaType.APPLICATION_JSON))
                .andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }

    @Test
    void canRetrieveByIdAndTenant() throws Exception {
        given(clientService.getClient("mock", 1))
                .willReturn(ClientDTO.builder().clientId("mock").tenant(1).build());
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.get("/api/2.0/clients/mock")
                                .header("X-Tenant", 1)
                                .accept(MediaType.APPLICATION_JSON))
                .andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }

    @Test
    void canCreateClient() throws Exception {
        given(clientService.clientAsyncCreationTask(CreateClientDTO
                .builder()
                        .name("mock")
                .build(), 1)).willReturn(ClientDTO
                .builder()
                        .name("mock")
                .build());
        given(applicationConfiguration.getScopes())
                .willReturn(List.of(new ApplicationConfiguration
                        .ScopeConfiguration("mock", "mock")));
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.post("/api/2.0/clients")
                                .contentType(MediaType.APPLICATION_JSON)
                                .content(jsonClient.
                                        write(CreateClientDTO
                                                .builder()
                                                .name("mock")
                                                .description("mockmockmockmockmockmockmockmockmockmock")
                                                .termsUrl("https://example.com")
                                                .policyUrl("https://example.com")
                                                .logoUrl("https://example.com")
                                                .authenticationMethod("mock")
                                                .redirectUri("https://example.com")
                                                .logoutRedirectUri("https://example.com")
                                                .scopes(Set.of("mock"))
                                                .build()).
                                        getJson())
                                .header("X-Tenant", 1)
                ).andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }
}
