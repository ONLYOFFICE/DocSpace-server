# Web Layer - Controllers & REST APIs

Conventions for HTTP APIs in Spring Boot: resource-oriented URLs, JSON payloads,
status codes, and problem responses. Prefer OpenAPI (API-first) and keep DTOs
aligned with these rules.

## Design rules (quick)

| Area | Do | Don't |
|------|----|-------|
| Base path | Resource paths at the host root (`/users`, `/order-items`) | `/api/...` as a base path |
| Resources | Plural nouns, kebab-case segments | Verbs in paths (`/getUser`, `/create`) |
| Path IDs | Opaque URL-safe ids (prefer UUID when needed) | Guessable sequential ids unless required |
| Query params | `snake_case` (`created_after`, `page_size`) | `camelCase` query names |
| JSON fields | `snake_case` | `camelCase` in public JSON |
| Enums | `UPPER_SNAKE_CASE` strings | Mixed-case enum literals in JSON |
| Dates | ISO-8601 date-time (`OffsetDateTime`) | Epoch millis / locale formats |
| Versioning | Compatible extensions; media-type versioning if unavoidable | URL versioning (`/v1/...`) |
| Errors | `application/problem+json` (`ProblemDetail`) | Ad-hoc error envelopes / stack traces |
| Success body | Wrap every success payload as `{ "data": …, "requestId": "<traceId>" }` | Bare resource/array at the root; omitting `requestId` |
| Collections | Cursor pagination + absolute link URIs | Offset/`Page` as the default public contract |
| Top-level body | Always a JSON **object** (the success envelope, or problem JSON) | Bare arrays or scalars as the root |

---

## Jackson: snake_case JSON

```java
@Configuration
public class JacksonConfig {
  @Bean
  Jackson2ObjectMapperBuilderCustomizer snakeCase() {
    return builder -> builder.propertyNamingStrategy(PropertyNamingStrategies.SNAKE_CASE);
  }
}
```

Java fields may stay camelCase; JSON wire format is snake_case. Document dates as
`date-time` (ISO-8601 with offset). Prefer `OffsetDateTime` over `LocalDateTime`
for API boundaries.

---

## REST controller pattern

```java
@RestController
@RequestMapping("/users")
@Validated
public class UserController {
  private final UserService userService;

  public UserController(UserService userService) {
    this.userService = userService;
  }

  @GetMapping(produces = MediaType.APPLICATION_JSON_VALUE)
  public ResponseEntity<UserPage> listUsers(
      @RequestParam(name = "cursor", required = false) String cursor,
      @RequestParam(name = "limit", defaultValue = "20") @Min(1) @Max(100) int limit,
      @RequestParam(name = "created_after", required = false)
          @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME)
          OffsetDateTime createdAfter) {
    return ResponseEntity.ok(userService.findPage(cursor, limit, createdAfter));
  }

  @GetMapping(path = "/{user_id}", produces = MediaType.APPLICATION_JSON_VALUE)
  public ResponseEntity<UserResponse> getUser(@PathVariable("user_id") UUID userId) {
    return ResponseEntity.ok(userService.findById(userId));
  }

  @PostMapping(
      consumes = MediaType.APPLICATION_JSON_VALUE,
      produces = MediaType.APPLICATION_JSON_VALUE)
  public ResponseEntity<UserResponse> createUser(
      @Valid @RequestBody UserCreateRequest request,
      @RequestHeader(value = "Idempotency-Key", required = false) String idempotencyKey) {
    UserResponse user = userService.create(request, idempotencyKey);
    URI location =
        ServletUriComponentsBuilder.fromCurrentRequest()
            .path("/{user_id}")
            .buildAndExpand(user.id())
            .toUri();
    return ResponseEntity.created(location).body(user);
  }

  @PutMapping(
      path = "/{user_id}",
      consumes = MediaType.APPLICATION_JSON_VALUE,
      produces = MediaType.APPLICATION_JSON_VALUE)
  public ResponseEntity<UserResponse> replaceUser(
      @PathVariable("user_id") UUID userId, @Valid @RequestBody UserReplaceRequest request) {
    return ResponseEntity.ok(userService.replace(userId, request));
  }

  @PatchMapping(
      path = "/{user_id}",
      consumes = MediaType.APPLICATION_JSON_VALUE,
      produces = MediaType.APPLICATION_JSON_VALUE)
  public ResponseEntity<UserResponse> patchUser(
      @PathVariable("user_id") UUID userId, @Valid @RequestBody UserPatchRequest request) {
    return ResponseEntity.ok(userService.patch(userId, request));
  }

  @DeleteMapping("/{user_id}")
  @ResponseStatus(HttpStatus.NO_CONTENT)
  public void deleteUser(@PathVariable("user_id") UUID userId) {
    userService.delete(userId);
  }
}
```

### HTTP method semantics

| Method | Use for | Success codes |
|--------|---------|---------------|
| `GET` | Read (safe, idempotent) | `200` |
| `POST` | Create / non-idempotent process | `201` + `Location`, or `202` if async |
| `PUT` | Full replace (idempotent) | `200` / `204` |
| `PATCH` | Partial update | `200` / `204` |
| `DELETE` | Remove (idempotent) | `204` (or `200` with body) |
| `HEAD` / `OPTIONS` | Metadata / CORS preflight | per standard |

Prefer designing `POST`/`PATCH` for idempotent retries (`Idempotency-Key` or natural
secondary keys). Document filtering that happens implicitly on list endpoints.

Avoid redirect status codes in APIs unless you truly relocate a resource permanently.

---

## Request / response DTOs

```java
public record UserCreateRequest(
    @NotBlank @Email String email,
    @NotBlank @Size(min = 8, max = 100) String password,
    @NotBlank @Size(min = 3, max = 50) String username,
    @Min(18) @Max(120) Integer age) {}

public record UserReplaceRequest(
    @NotBlank @Email String email,
    @NotBlank @Size(min = 3, max = 50) String username,
    @NotNull Boolean active) {}

/** Partial update: absent vs null share the same semantics — omit means “no change”
 *  only if you document and implement sparse PATCH carefully; prefer explicit fields. */
public record UserPatchRequest(
    @Email String email, @Size(min = 3, max = 50) String username, Boolean active) {}

public record UserResponse(
    UUID id,
    String email,
    String username,
    Integer age,
    Boolean active,
    UserStatus status,
    OffsetDateTime created_at,
    OffsetDateTime updated_at) {}

public enum UserStatus {
  ACTIVE,
  SUSPENDED,
  DELETED
}
```

**Null rules:** treat `null` and absent properties with the **same** semantics;
do not use `null` for booleans; prefer `[]` over `null` for empty arrays;
pluralize array property names (`items`, not `item`).

Always return a **JSON object** at the top level via the success envelope (below),
or problem JSON on errors. Nested list/page payloads live under `data`.

---

## Success response envelope (`data` + `requestId`)

Every **successful** JSON response must be wrapped as:

```json
{
  "data": { },
  "requestId": "<trace-id>"
}
```

- `data` — the resource, collection page, or command result the controller returns
- `requestId` — the current distributed **trace id** (same value exposed on `X-Request-Id`)

Example:

```json
{
  "data": {
    "jobId": "d2f517be-d6ca-4d86-98d4-9fd85eb117a6",
    "type": "EXTRACTION",
    "status": "completed",
    "attempts": 1,
    "createdAt": "2026-08-11T10:52:44.794828Z",
    "updatedAt": "2026-08-11T10:53:27.589906Z"
  },
  "requestId": "893cf5f046b56255ccb303a14d2b564e"
}
```

Controllers return the inner payload only; a global `ResponseBodyAdvice` wraps it.
Do **not** wrap `ProblemDetail` / `application/problem+json` error bodies.

```java
/** Generic success envelope — requestId is the active trace id. */
public record ApiResponseEnvelope<T>(T data, String requestId) {}

@ControllerAdvice
public class TraceableResponseBodyAdvice implements ResponseBodyAdvice<Object> {
  @Override
  public boolean supports(
      MethodParameter returnType, Class<? extends HttpMessageConverter<?>> converterType) {
    return AbstractJacksonHttpMessageConverter.class.isAssignableFrom(converterType);
  }

  @Override
  public @Nullable Object beforeBodyWrite(
      @Nullable Object body,
      MethodParameter returnType,
      MediaType selectedContentType,
      Class<? extends HttpMessageConverter<?>> selectedConverterType,
      ServerHttpRequest request,
      ServerHttpResponse response) {
    if (body instanceof ApiResponseEnvelope<?> || body instanceof ProblemDetail) {
      return body;
    }
    if (MediaType.APPLICATION_PROBLEM_JSON.isCompatibleWith(selectedContentType)) {
      return body;
    }
    String requestId = response.getHeaders().getFirst("X-Request-Id");
    return new ApiResponseEnvelope<>(body, requestId == null ? "" : requestId);
  }
}
```

Set `X-Request-Id` from the active OTel / Micrometer trace id (filter) so the
header and `requestId` field stay aligned:

```java
response.setHeader("X-Request-Id", currentTraceId().orElse(""));
```

OpenAPI: document success schemas as the envelope (`data` + `requestId`), not the bare DTO.

---

## Cursor pagination

Prefer opaque cursors over offset/`Pageable` for public list APIs.

```java
public record UserPage(
    List<UserResponse> items,
    String self,
    String first,
    String prev,
    String next,
    String last) {}
```

- `cursor` / `limit` (or documented aliases) as query params — `snake_case`
- `self` / `first` / `prev` / `next` / `last` are **absolute** URIs (or opaque cursors
  documented as such); clients must not parse cursor contents
- Avoid requiring a total count unless the product truly needs it
- Example list JSON (still under the success envelope):

```json
{
  "data": {
    "items": [ { "id": "...", "email": "a@example.com" } ],
    "self": "https://example.com/users?cursor=abc&limit=20",
    "next": "https://example.com/users?cursor=def&limit=20"
  },
  "requestId": "893cf5f046b56255ccb303a14d2b564e"
}
```

---

## Problem JSON errors

Use RFC 9457 / Spring `ProblemDetail` and media type `application/problem+json`.
Never expose stack traces to clients.

```java
@RestControllerAdvice
public class GlobalExceptionHandler {

  @ExceptionHandler(ResourceNotFoundException.class)
  public ProblemDetail handleNotFound(ResourceNotFoundException ex) {
    ProblemDetail problem = ProblemDetail.forStatusAndDetail(HttpStatus.NOT_FOUND, ex.getMessage());
    problem.setTitle("Not Found");
    problem.setType(URI.create("https://example.com/problems/not-found"));
    problem.setProperty("trace_id", MDC.get("traceId"));
    return problem;
  }

  @ExceptionHandler(MethodArgumentNotValidException.class)
  public ProblemDetail handleValidation(MethodArgumentNotValidException ex) {
    ProblemDetail problem =
        ProblemDetail.forStatusAndDetail(HttpStatus.BAD_REQUEST, "Request validation failed");
    problem.setTitle("Bad Request");
    problem.setType(URI.create("https://example.com/problems/validation-failed"));
    problem.setProperty(
        "violations",
        ex.getBindingResult().getFieldErrors().stream()
            .map(
                fe ->
                    Map.of(
                        "field", fe.getField(),
                        "message",
                            fe.getDefaultMessage() != null ? fe.getDefaultMessage() : "Invalid value"))
            .toList());
    return problem;
  }

  @ExceptionHandler(DataIntegrityViolationException.class)
  public ProblemDetail handleConflict(DataIntegrityViolationException ex) {
    ProblemDetail problem =
        ProblemDetail.forStatusAndDetail(
            HttpStatus.CONFLICT, "Resource state conflict — it may already exist");
    problem.setTitle("Conflict");
    problem.setType(URI.create("https://example.com/problems/conflict"));
    return problem;
  }

  @ExceptionHandler(Exception.class)
  public ProblemDetail handleUnexpected(Exception ex) {
    // Log full exception server-side; clients get a generic detail only.
    ProblemDetail problem =
        ProblemDetail.forStatusAndDetail(
            HttpStatus.INTERNAL_SERVER_ERROR, "An unexpected error occurred");
    problem.setTitle("Internal Server Error");
    problem.setType(URI.create("https://example.com/problems/internal"));
    return problem;
  }
}
```

Ensure controllers/`produces` and error paths can return `application/problem+json`
(clients should include it in `Accept` when they want rich errors).

### Common status codes

| Code | When |
|------|------|
| `200` | Successful read / update with body |
| `201` | Created — set `Location` to the new resource URI |
| `202` | Accepted for asynchronous processing |
| `204` | Success with no body |
| `207` | Multi-status for batch/bulk item results |
| `400` | Malformed / invalid request |
| `401` / `403` | Authentication / authorization |
| `404` | Resource missing |
| `409` | Conflict (duplicate secondary key, state conflict) |
| `412` | Precondition failed (`If-Match`) |
| `429` | Rate limited — include rate-limit headers |
| `500` / `503` | Server / dependency failure |

Use the **most specific** applicable code; document success and error responses in OpenAPI.

---

## Headers

```java
// Idempotent create/update retries (optional but recommended for POST)
@RequestHeader(value = "Idempotency-Key", required = false) String idempotencyKey

// Optimistic concurrency
// If-Match / If-None-Match with ETag on the resource

// Prefer header may influence async vs sync processing when supported
```

- Prefer `Location` for newly created resources (not `Content-Location`)
- Use standard `Content-*` headers correctly (`Content-Type`, `Content-Encoding`, …)
- Propagate inbound correlation / flow identifiers to downstream calls and logs (MDC)
- Header names: kebab-case with conventional capitalization (`Idempotency-Key`)

---

## Custom validation

```java
@Target({ElementType.FIELD, ElementType.PARAMETER})
@Retention(RetentionPolicy.RUNTIME)
@Constraint(validatedBy = UniqueEmailValidator.class)
public @interface UniqueEmail {
  String message() default "Email already exists";

  Class<?>[] groups() default {};

  Class<? extends Payload>[] payload() default {};
}

@Component
public class UniqueEmailValidator implements ConstraintValidator<UniqueEmail, String> {
  private final UserRepository userRepository;

  public UniqueEmailValidator(UserRepository userRepository) {
    this.userRepository = userRepository;
  }

  @Override
  public boolean isValid(String email, ConstraintValidatorContext context) {
    if (email == null) {
      return true;
    }
    return !userRepository.existsByEmail(email);
  }
}
```

---

## WebClient for outbound HTTP

```java
@Configuration
public class WebClientConfig {
  @Bean
  WebClient webClient(WebClient.Builder builder) {
    return builder
        .defaultHeader(HttpHeaders.ACCEPT, MediaType.APPLICATION_JSON_VALUE)
        .defaultHeader(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE)
        .filter(propagateCorrelation())
        .build();
  }

  private ExchangeFilterFunction propagateCorrelation() {
    return (request, next) -> {
      String traceId = MDC.get("traceId");
      ClientRequest.Builder builder = ClientRequest.from(request);
      if (traceId != null && !traceId.isBlank()) {
        builder.header("X-Request-Id", traceId);
      }
      return next.exchange(builder.build());
    };
  }
}

@Service
public class ExternalApiService {
  private final WebClient webClient;

  public ExternalApiService(WebClient webClient) {
    this.webClient = webClient;
  }

  public Mono<ExternalDataResponse> fetchData(UUID id) {
    return webClient
        .get()
        .uri("https://example.com/data/{id}", id)
        .retrieve()
        .onStatus(
            HttpStatusCode::is4xxClientError,
            response -> Mono.error(new ResourceNotFoundException("External resource not found")))
        .onStatus(
            HttpStatusCode::is5xxServerError,
            response -> Mono.error(new ServiceUnavailableException("External service unavailable")))
        .bodyToMono(ExternalDataResponse.class)
        .timeout(Duration.ofSeconds(5));
  }
}
```

Honor the peer’s problem+json when present; do not assume every JSON error body is yours.

---

## CORS

```java
@Configuration
public class WebConfig implements WebMvcConfigurer {
  @Override
  public void addCorsMappings(CorsRegistry registry) {
    registry
        .addMapping("/**")
        .allowedOrigins("https://example.com")
        .allowedMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .allowedHeaders("*")
        .exposedHeaders("Location", "Deprecation", "Sunset")
        .allowCredentials(true)
        .maxAge(3600);
  }
}
```

---

## Compatibility & deprecation

- Do not break existing clients: additive fields only; keep null/absent semantics stable
- Clients must ignore unknown JSON properties (open for extension)
- Prefer deprecation headers (`Deprecation`, `Sunset`) over sudden removal
- Publish and keep OpenAPI in sync with the running API

---

## Quick reference

| Annotation / type | Purpose |
|-------------------|---------|
| `@RestController` | JSON REST controller |
| `@RequestMapping` | Resource base path (plural, kebab-case, no `/api`) |
| `@GetMapping` / `@PostMapping` / … | Method-specific mappings |
| `@PathVariable` | Path segment id (`user_id`) |
| `@RequestParam` | Query params (`snake_case` names) |
| `@RequestBody` + `@Valid` | JSON body validation |
| `@RequestHeader("Idempotency-Key")` | Idempotent POST/PATCH retries |
| `ProblemDetail` | RFC 9457 problem responses (not wrapped in `data`) |
| `ApiResponseEnvelope` / `ResponseBodyAdvice` | Wrap success bodies as `{ data, requestId }` with trace id |
| `OffsetDateTime` | API date-time fields |
| `PropertyNamingStrategies.SNAKE_CASE` | JSON property naming |
| `@RestControllerAdvice` | Global exception → problem JSON |
| `@ResponseStatus` | Success status when not using `ResponseEntity` |
