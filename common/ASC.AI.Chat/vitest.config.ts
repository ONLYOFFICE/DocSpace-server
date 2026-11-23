import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["app/**/__tests__/**/*.test.ts"],
    setupFiles: ["app/__tests__/setup.ts"],
    restoreMocks: true,
    coverage: {
      provider: "v8",
      include: ["app/**/*.ts", "config/**/*.ts"],
      exclude: ["app/**/__tests__/**", "app/generated/**", "app/**/*.d.ts"],
      reporter: ["text-summary", "html"],
      reportsDirectory: "coverage",
    },
  },
});
