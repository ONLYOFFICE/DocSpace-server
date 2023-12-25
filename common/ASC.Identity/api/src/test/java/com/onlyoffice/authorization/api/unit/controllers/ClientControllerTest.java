/**
 *
 */
package com.onlyoffice.authorization.api.unit.controllers;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.client.transfer.SettingsDTO;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.security.filters.CheckAscCookieCommonProcessor;
import com.onlyoffice.authorization.api.web.security.filters.CheckAuthAdminCookieFilter;
import com.onlyoffice.authorization.api.web.server.controllers.ClientController;
import com.onlyoffice.authorization.api.web.server.ports.services.client.ClientRetrieveService;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.PaginationDTO;
import jakarta.servlet.http.Cookie;
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

import java.net.URI;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;

/**
 *
 */
@ExtendWith(MockitoExtension.class)
public class ClientControllerTest {
    private MockMvc mvc;
    @Mock
    private ClientRetrieveService clientService;
    @Mock
    private APIClient apiClient;
    @Mock
    private ApplicationConfiguration applicationConfiguration;
    @InjectMocks
    private ClientController clientController;

    private JacksonTester<CreateClientDTO> jsonClient;

    @BeforeEach
    void setup() {
        JacksonTester.initFields(this, new ObjectMapper());
        mvc = MockMvcBuilders.standaloneSetup(clientController)
                .addFilter(new CheckAuthAdminCookieFilter(new CheckAscCookieCommonProcessor(apiClient)))
                .build();
    }

    @Test
    void canRetrieve() throws Exception {
        given(apiClient.getMe(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<PersonDTO>builder()
                        .statusCode(200)
                        .statusCode(200)
                        .response(PersonDTO
                                .builder()
                                .userName("Admin")
                                .firstName("Admin")
                                .lastName("Admin")
                                .email("admin@admin.com")
                                .avatarSmall("smallAvatar")
                                .avatar("avatar")
                                .isAdmin(true)
                                .build())
                        .build());
        given(apiClient.getProfile(URI.create("http://127.0.0.1"), "asc_auth_key=zxc", "admin@admin.com"))
                .willReturn(APIClientDTOWrapper
                        .<PersonDTO>builder()
                        .statusCode(200)
                        .statusCode(200)
                        .response(PersonDTO
                                .builder()
                                .userName("Admin")
                                .firstName("Admin")
                                .lastName("Admin")
                                .email("admin@admin.com")
                                .avatarSmall("smallAvatar")
                                .avatar("avatar")
                                .build())
                        .build());
        given(apiClient.getTenant(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<TenantDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(TenantDTO
                                .builder()
                                .tenantId(1)
                                .tenantAlias("tenant")
                                .name("tenant")
                                .build())
                        .build());
        given(apiClient.getSettings(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<SettingsDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(SettingsDTO
                                .builder()
                                .timezone("Europe/Moscow")
                                .build())
                        .build());
        given(clientService.getTenantClients(1, 0, 5))
                .willReturn(PaginationDTO.builder()
                        .data(List.of(ClientDTO.builder()
                                .modifiedBy("admin@admin.com")
                                .clientId("mock")
                                .createdOn(ZonedDateTime.now())
                                .modifiedOn(ZonedDateTime.now())
                                .tenant(1)
                                .build()))
                        .limit(5)
                        .page(0)
                        .build());
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.get("/api/2.0/clients")
                                .queryParam("page", "0")
                                .queryParam("limit", "5")
                                .cookie(new Cookie[]{
                                        new Cookie("asc_auth_key", "zxc"),
                                        new Cookie("x-docspace-address", "http://127.0.0.1")
                                })
                                .accept(MediaType.APPLICATION_JSON))
                .andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }

    @Test
    void canRetrieveByIdAndTenant() throws Exception {
        given(apiClient.getMe(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<PersonDTO>builder()
                        .statusCode(200)
                        .statusCode(200)
                        .response(PersonDTO
                                .builder()
                                .userName("Admin")
                                .firstName("Admin")
                                .lastName("Admin")
                                .email("admin@admin.com")
                                .avatarSmall("smallAvatar")
                                .avatar("avatar")
                                .isAdmin(true)
                                .build())
                        .build());
        given(apiClient.getTenant(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<TenantDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(TenantDTO
                                .builder()
                                .tenantId(1)
                                .tenantAlias("tenant")
                                .name("tenant")
                                .build())
                        .build());
        given(apiClient.getSettings(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<SettingsDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(SettingsDTO
                                .builder()
                                .timezone("Europe/Moscow")
                                .build())
                        .build());
        given(clientService.getClient("mock"))
                .willReturn(ClientDTO
                        .builder()
                        .clientId("mock")
                        .modifiedOn(ZonedDateTime.now())
                        .createdOn(ZonedDateTime.now())
                        .tenant(1)
                        .build());
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.get("/api/2.0/clients/mock")
                                .cookie(new Cookie[]{
                                        new Cookie("asc_auth_key", "zxc"),
                                        new Cookie("x-docspace-address", "http://127.0.0.1")
                                })
                                .accept(MediaType.APPLICATION_JSON))
                .andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }

    @Test
    void canCreateClient() throws Exception {
        given(apiClient.getMe(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<PersonDTO>builder()
                        .statusCode(200)
                        .statusCode(200)
                        .response(PersonDTO
                                .builder()
                                .userName("Admin")
                                .firstName("Admin")
                                .lastName("Admin")
                                .email("admin@admin.com")
                                .avatarSmall("smallAvatar")
                                .avatar("avatar")
                                .isAdmin(true)
                                .build())
                        .build());
        given(apiClient.getTenant(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<TenantDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(TenantDTO
                                .builder()
                                .tenantId(1)
                                .tenantAlias("tenant")
                                .name("tenant")
                                .build())
                        .build());
        given(apiClient.getSettings(URI.create("http://127.0.0.1"), "asc_auth_key=zxc"))
                .willReturn(APIClientDTOWrapper
                        .<SettingsDTO>builder()
                        .status(200)
                        .statusCode(200)
                        .response(SettingsDTO
                                .builder()
                                .timezone("Europe/Moscow")
                                .build())
                        .build());
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
                                                .logo("data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48IS0tIFVwbG9hZGVkIHRvOiBTVkcgUmVwbywgd3d3LnN2Z3JlcG8uY29tLCBHZW5lcmF0b3I6IFNWRyBSZXBvIE1peGVyIFRvb2xzIC0tPg0KPHN2ZyB3aWR0aD0iODAwcHgiIGhlaWdodD0iODAwcHgiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4NCjxwYXRoIGQ9Ik03IDEySDE3TTggOC41QzggOC41IDkgOSAxMCA5QzExLjUgOSAxMi41IDggMTQgOEMxNSA4IDE2IDguNSAxNiA4LjVNOCAxNS41QzggMTUuNSA5IDE2IDEwIDE2QzExLjUgMTYgMTIuNSAxNSAxNCAxNUMxNSAxNSAxNiAxNS41IDE2IDE1LjVNMjEgMTJDMjEgMTYuOTcwNiAxNi45NzA2IDIxIDEyIDIxQzcuMDI5NDQgMjEgMyAxNi45NzA2IDMgMTJDMyA3LjAyOTQ0IDcuMDI5NDQgMyAxMiAzQzE2Ljk3MDYgMyAyMSA3LjAyOTQ0IDIxIDEyWiIgc3Ryb2tlPSIjMDAwMDAwIiBzdHJva2Utd2lkdGg9IjIiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIgc3Ryb2tlLWxpbmVqb2luPSJyb3VuZCIvPg0KPC9zdmc+")
                                                .websiteUrl("https://example.com")
                                                .redirectUris(Set.of("https://example.com"))
                                                .logoutRedirectUri("https://example.com")
                                                .allowedOrigins(Set.of("https://example.com"))
                                                .scopes(Set.of("accounts:read"))
                                                .build()).
                                        getJson())
                                .cookie(new Cookie[]{
                                        new Cookie("asc_auth_key", "zxc"),
                                        new Cookie("x-docspace-address", "http://127.0.0.1")
                                })
                ).andReturn().getResponse();
        assertThat(response.getStatus()).isEqualTo(HttpStatus.BAD_REQUEST.value());
    }
}
