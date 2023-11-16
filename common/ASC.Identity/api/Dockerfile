FROM maven:3.8.3-openjdk-17 AS MAVEN_BUILD
WORKDIR /build/
COPY . .
WORKDIR /build/api
RUN mvn clean package -DskipTests=true

FROM openjdk:17
WORKDIR /app
COPY --from=MAVEN_BUILD /build/api/target/api-0.0.1.jar /app/
ENTRYPOINT ["java", "-jar", "api-0.0.1.jar"]