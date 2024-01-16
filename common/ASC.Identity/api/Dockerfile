FROM openjdk:17-jdk-slim as build

COPY .mvn .mvn
COPY mvnw .
COPY pom.xml .
COPY src src

RUN ./mvnw -B -Dmaven.test.skip clean package

FROM openjdk:17-jdk-slim

COPY --from=build target/identity-api.jar .
ENTRYPOINT ["java", "-jar", "identity-api.jar"]