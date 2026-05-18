BASE_IMAGE = identity-base:latest
REGISTRATION_IMAGE = registration-service:latest
AUTHORIZATION_IMAGE = authorization-service:latest
MINIFIED_IMAGE = identity-minified:latest
MAVEN_NATIVE_PROFILES = $(if $(strip $(SPRING_PROFILES_ACTIVE)),-Dspring.profiles.active=$(SPRING_PROFILES_ACTIVE))
MAVEN_FMT_SKIP = -Dfmt.skip=true
MAVEN_BATCH = -B

REGISTRATION_JAR = registration-container.jar
AUTHORIZATION_JAR = authorization-container.jar
TEMP_BUILDER_REG = temp-jar-builder-reg:latest
TEMP_BUILDER_AUTH = temp-jar-builder-auth:latest

.PHONY: all test integration-tests minified-tests test-all docs check-profile base registration authorization minified native-binary native-binary-registration native-binary-authorization clean prune summary sizes help

all: base registration authorization sizes

test:
	@echo "Running unit tests..."
	mvn $(MAVEN_BATCH) test $(MAVEN_FMT_SKIP)

integration-tests:
	@echo "Running integration tests..."
	mvn $(MAVEN_BATCH) test $(MAVEN_FMT_SKIP) -DRUN_INTEGRATION_TESTS=true -Dtest='!SpringDocsIT,*IT' -Dsurefire.failIfNoSpecifiedTests=false

minified-tests:
	@echo "Building dependencies..."
	@mvn $(MAVEN_BATCH) install -pl minified -am -P minified $(MAVEN_FMT_SKIP) -DskipTests -q
	@echo "Running minified integration tests..."
	cd minified && mvn $(MAVEN_BATCH) test $(MAVEN_FMT_SKIP) -DRUN_INTEGRATION_TESTS=true

test-all: test integration-tests minified-tests
	@echo "All tests completed."

docs:
	@echo "Generating authorization OpenAPI JSON"
	mvn $(MAVEN_BATCH) -pl authorization/authorization-application -am test $(MAVEN_FMT_SKIP) -Dtest=SpringDocsIT -DRUN_DOCS_GENERATION=true -Dsurefire.failIfNoSpecifiedTests=false
	@echo "Generating registration OpenAPI JSON"
	mvn $(MAVEN_BATCH) -pl registration/registration-application -am test $(MAVEN_FMT_SKIP) -Dtest=SpringDocsIT -DRUN_DOCS_GENERATION=true -Dsurefire.failIfNoSpecifiedTests=false
	@echo "OpenAPI files generated:"
	@echo " docs/identity-authorization-openapi.json"
	@echo " docs/identity-registration-openapi.json"

check-profile:
	@if [ -z "$(SPRING_PROFILES_ACTIVE)" ]; then \
		echo "ERROR: SPRING_PROFILES_ACTIVE is required and must include 'server' or 'saas'."; \
		echo "Example: SPRING_PROFILES_ACTIVE=stage,server make native-binary-authorization"; \
		exit 1; \
	fi
	@case ",$(SPRING_PROFILES_ACTIVE)," in \
		*,server,*|*,saas,*) ;; \
		*) \
			echo "ERROR: SPRING_PROFILES_ACTIVE='$(SPRING_PROFILES_ACTIVE)' must include 'server' or 'saas'."; \
			exit 1 ;; \
	esac

base: $(REGISTRATION_JAR) $(AUTHORIZATION_JAR)
	@echo "Building shared base image..."
	docker build \
		--build-arg REGISTRATION_JAR=$(REGISTRATION_JAR) \
		--build-arg AUTHORIZATION_JAR=$(AUTHORIZATION_JAR) \
		-t $(BASE_IMAGE) \
		-f Dockerfile.shared .
	@echo "Base image built: $(BASE_IMAGE)"
	@docker images $(BASE_IMAGE) --format "Size: {{.Size}}"
	@$(MAKE) clean --no-print-directory

$(REGISTRATION_JAR):
	@echo "Building registration JAR..."
	docker build \
		--build-arg MODULE=registration/registration-container \
		-t $(TEMP_BUILDER_REG) \
		-f Dockerfile .
	@echo "Extracting registration JAR..."
	@CONTAINER_ID=$$(docker create $(TEMP_BUILDER_REG)) && \
		docker cp $$CONTAINER_ID:/app/app.jar ./$(REGISTRATION_JAR) && \
		docker rm $$CONTAINER_ID && \
		docker rmi $(TEMP_BUILDER_REG) 2>/dev/null || true

$(AUTHORIZATION_JAR):
	@echo "Building authorization JAR..."
	docker build \
		--build-arg MODULE=authorization/authorization-container \
		-t $(TEMP_BUILDER_AUTH) \
		-f Dockerfile .
	@echo "Extracting authorization JAR..."
	@CONTAINER_ID=$$(docker create $(TEMP_BUILDER_AUTH)) && \
		docker cp $$CONTAINER_ID:/app/app.jar ./$(AUTHORIZATION_JAR) && \
		docker rm $$CONTAINER_ID && \
		docker rmi $(TEMP_BUILDER_AUTH) 2>/dev/null || true

registration:
	@echo "Building registration service..."
	docker build \
		--build-arg BASE_IMAGE=$(BASE_IMAGE) \
		--build-arg MODULE=registration/registration-container \
		-t $(REGISTRATION_IMAGE) \
		-f Dockerfile.service .
	@echo "Registration image built: $(REGISTRATION_IMAGE)"
	@docker images $(REGISTRATION_IMAGE) --format "Size: {{.Size}}"

authorization:
	@echo "Building authorization service..."
	docker build \
		--build-arg BASE_IMAGE=$(BASE_IMAGE) \
		--build-arg MODULE=authorization/authorization-container \
		-t $(AUTHORIZATION_IMAGE) \
		-f Dockerfile.service .
	@echo "Authorization image built: $(AUTHORIZATION_IMAGE)"
	@docker images $(AUTHORIZATION_IMAGE) --format "Size: {{.Size}}"

minified:
	@echo "Building minified identity service..."
	docker build \
		-t $(MINIFIED_IMAGE) \
		-f Dockerfile.minified .
	@echo "Minified image built: $(MINIFIED_IMAGE)"
	@docker images $(MINIFIED_IMAGE) --format "Size: {{.Size}}"

sizes:
	@BASE_BYTES=$$(docker inspect --format='{{.Size}}' $(BASE_IMAGE) 2>/dev/null || echo "0"); \
	REG_BYTES=$$(docker inspect --format='{{.Size}}' $(REGISTRATION_IMAGE) 2>/dev/null || echo "0"); \
	AUTH_BYTES=$$(docker inspect --format='{{.Size}}' $(AUTHORIZATION_IMAGE) 2>/dev/null || echo "0"); \
	if [ "$$BASE_BYTES" != "0" ]; then \
		BASE_MB=$$(($$BASE_BYTES / 1048576)); \
		REG_UNIQUE_KB=$$(( ($$REG_BYTES - $$BASE_BYTES) / 1024 )); \
		AUTH_UNIQUE_KB=$$(( ($$AUTH_BYTES - $$BASE_BYTES) / 1024 )); \
		TOTAL_MB=$$(($$BASE_BYTES / 1048576 + ($$REG_BYTES - $$BASE_BYTES) / 1048576 + ($$AUTH_BYTES - $$BASE_BYTES) / 1048576)); \
		VIRTUAL_MB=$$(( ($$BASE_BYTES + $$REG_BYTES + $$AUTH_BYTES) / 1048576 )); \
		echo "  Base image (shared):      $$BASE_MB MB"; \
		echo "  Registration unique:      $$REG_UNIQUE_KB KB"; \
		echo "  Authorization unique:     $$AUTH_UNIQUE_KB KB"; \
		echo "  ---------------------------------"; \
		echo "  Total real disk usage:    $$TOTAL_MB MB"; \
		echo "  Virtual (if not shared):  $$VIRTUAL_MB MB"; \
		SAVED=$$(($$VIRTUAL_MB - $$TOTAL_MB)); \
		echo "  Disk saved by sharing:    $$SAVED MB"; \
	else \
		echo "  Images not found. Run 'make all' first."; \
	fi

native-binary: check-profile native-binary-registration native-binary-authorization

native-binary-registration: check-profile
	@echo "Installing shared modules..."
	mvn $(MAVEN_BATCH) install -DskipTests $(MAVEN_FMT_SKIP) \
		-pl infrastructure/infrastructure-configuration,registration/registration-data -am -q
	@echo "Compiling registration native binary..."
	mvn $(MAVEN_BATCH) -Pnative package -DskipTests $(MAVEN_FMT_SKIP) $(MAVEN_NATIVE_PROFILES) \
		-pl registration/registration-container -am
	mvn $(MAVEN_BATCH) -Pnative native:compile -DskipTests $(MAVEN_FMT_SKIP) $(MAVEN_NATIVE_PROFILES) \
		-pl registration/registration-container
	@mkdir -p bin
	@BINARY=$$(find registration/registration-container/target -maxdepth 1 -type f -perm +0111 ! -name "*.jar" | head -1); \
	if [ -z "$$BINARY" ]; then echo "ERROR: native binary not found in registration/registration-container/target/"; exit 1; fi; \
	cp "$$BINARY" bin/registration-container && echo "Binary: bin/registration-container"

native-binary-authorization: check-profile
	@echo "Installing shared modules..."
	mvn $(MAVEN_BATCH) install -DskipTests $(MAVEN_FMT_SKIP) \
		-pl infrastructure/infrastructure-configuration -am -q
	@echo "Compiling authorization native binary..."
	mvn $(MAVEN_BATCH) -Pnative package -DskipTests $(MAVEN_FMT_SKIP) $(MAVEN_NATIVE_PROFILES) \
		-pl authorization/authorization-container -am
	mvn $(MAVEN_BATCH) -Pnative native:compile -DskipTests $(MAVEN_FMT_SKIP) $(MAVEN_NATIVE_PROFILES) \
		-pl authorization/authorization-container
	@mkdir -p bin
	@BINARY=$$(find authorization/authorization-container/target -maxdepth 1 -type f -perm +0111 ! -name "*.jar" | head -1); \
	if [ -z "$$BINARY" ]; then echo "ERROR: native binary not found in authorization/authorization-container/target/"; exit 1; fi; \
	cp "$$BINARY" bin/authorization-container && echo "Binary: bin/authorization-container"

clean:
	@rm -f $(REGISTRATION_JAR) $(AUTHORIZATION_JAR)
	@rm -rf bin/
	@docker rmi $(TEMP_BUILDER_REG) $(TEMP_BUILDER_AUTH) 2>/dev/null || true

prune: clean
	@echo "Removing all built images..."
	docker rmi $(BASE_IMAGE) $(REGISTRATION_IMAGE) $(AUTHORIZATION_IMAGE) 2>/dev/null || true
	@echo "Images removed"
