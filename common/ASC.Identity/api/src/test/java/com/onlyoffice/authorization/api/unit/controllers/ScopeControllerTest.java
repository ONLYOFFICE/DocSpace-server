/**
 *
 */
package com.onlyoffice.authorization.api.unit.controllers;

import com.onlyoffice.authorization.api.external.controllers.ScopeController;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockMvcRequestBuilders;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import static org.assertj.core.api.Assertions.assertThat;

/**
 *
 */
@ExtendWith(MockitoExtension.class)
public class ScopeControllerTest {
    private MockMvc mvc;
    @InjectMocks
    private ScopeController scopeController;

    @BeforeEach
    public void setup() {
        mvc = MockMvcBuilders.standaloneSetup(scopeController)
                .build();
    }

    @Test
    void shouldGetAllScopes() throws Exception {
        MockHttpServletResponse response = mvc.perform(
                        MockMvcRequestBuilders.get("/api/2.0/scopes")
                                .accept(MediaType.APPLICATION_JSON))
                .andReturn().getResponse();

        assertThat(response.getStatus()).isEqualTo(HttpStatus.OK.value());
    }
}
