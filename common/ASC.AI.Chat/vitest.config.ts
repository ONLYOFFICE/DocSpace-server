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
      // What the suite reached when it was written, rounded down. The rest is code no input
      // gets to (see the per-file notes in ts-ai-tests.md §9); a drop means untested new code.
      thresholds: { lines: 99.5, statements: 99.5, functions: 100, branches: 98.5 },
    },
  },
});
