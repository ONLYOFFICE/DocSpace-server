FROM maven:3.8.3-openjdk-17 AS MAVEN_BUILD
WORKDIR /build/
COPY . .
WORKDIR /build/authorization
RUN mvn clean package -DskipTests=true

FROM openjdk:17
WORKDIR /app
COPY --from=MAVEN_BUILD /build/authorization/target/authorization-0.0.1.jar /app/
ENTRYPOINT ["java", "-jar", "authorization-0.0.1.jar"]