FROM maven:3.9.6-amazoncorretto-17-al2023 as build

COPY .mvn .mvn
COPY pom.xml .
COPY src src

RUN --mount=type=cache,target=/root/.m2 mvn -B -Dmaven.test.skip clean package

FROM maven:3.9.6-amazoncorretto-17-al2023

COPY --from=build target/identity-authorization.jar .
ENTRYPOINT ["java", "-jar", "identity-authorization.jar"]