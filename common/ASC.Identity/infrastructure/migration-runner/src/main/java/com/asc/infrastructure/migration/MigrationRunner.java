package com.asc.infrastructure.migration;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class MigrationRunner {
  public static void main(String[] args) {
    SpringApplication.run(MigrationRunner.class, args);
  }
}
