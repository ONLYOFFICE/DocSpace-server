# Stage 1: Build the application
FROM maven:3.8.8-eclipse-temurin-21-alpine as builder
WORKDIR /app

# Copy the root pom.xml and all necessary module pom.xml files
COPY pom.xml .

COPY authorization/pom.xml authorization/
COPY authorization/authorization-application/pom.xml authorization/authorization-application/
COPY authorization/authorization-container/pom.xml authorization/authorization-container/
COPY authorization/authorization-data/pom.xml authorization/authorization-data/
COPY authorization/authorization-messaging/pom.xml authorization/authorization-messaging/

COPY common/pom.xml common/
COPY common/common-application/pom.xml common/common-application/
COPY common/common-core/pom.xml common/common-core/
COPY common/common-core/common-domain/pom.xml common/common-core/common-domain/
COPY common/common-core/common-service/pom.xml common/common-core/common-service/
COPY common/common-data/pom.xml common/common-data/
COPY common/common-messaging/pom.xml common/common-messaging/
COPY common/common-utilities/pom.xml common/common-utilities/

COPY infrastructure/pom.xml infrastructure/
COPY infrastructure/infrastructure-benchmark/pom.xml infrastructure/infrastructure-benchmark/
COPY infrastructure/infrastructure-migration-runner/pom.xml infrastructure/infrastructure-migration-runner/
COPY infrastructure/infrastructure-configuration/pom.xml infrastructure/infrastructure-configuration/

COPY registration/pom.xml registration/
COPY registration/registration-application/pom.xml registration/registration-application/
COPY registration/registration-container/pom.xml registration/registration-container/
COPY registration/registration-data/pom.xml registration/registration-data/
COPY registration/registration-messaging/pom.xml registration/registration-messaging/
COPY registration/registration-core/pom.xml registration/registration-core/
COPY registration/registration-core/registration-domain/pom.xml registration/registration-core/registration-domain/
COPY registration/registration-core/registration-service/pom.xml registration/registration-core/registration-service/

# Download the project dependencies without running tests
RUN mvn dependency:go-offline

# Copy the source code
COPY authorization authorization/
COPY common common/
COPY infrastructure infrastructure/
COPY registration registration/

# Build the project based on the specified module
ARG MODULE
RUN mvn clean package -DskipTests -pl ${MODULE} -am

# Stage 2: Create the runtime image
FROM eclipse-temurin:21-jre-alpine

# Set the working directory inside the container
WORKDIR /app

# Copy the jar file from the builder stage
ARG MODULE
COPY --from=builder /app/${MODULE}/target/*.jar app.jar

# Define the entry point
ENTRYPOINT ["java", "-jar", "app.jar"]