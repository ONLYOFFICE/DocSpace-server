// SPDX-License-Identifier: AGPL-3.0-only
//
// Each rule here stands for a class of defect this service has shipped; the class and the
// commits behind it are in .claude/rules/ts-ai-failure-paths.md and ts-ai-style.md. Style is
// prettier's, not ESLint's: no formatting rule belongs in this file.
//
// Code that predates the rules is listed in eslint-suppressions.json (a count per file and
// rule). The count can only go down: a new violation in a listed file fails as well.

import tseslint from "typescript-eslint";

const SWALLOWING_CATCH =
  "A .catch() that answers a constant reads a failed call as 'absent' or 'empty'. Rethrow " +
  "everything but the status that means absent (ts-ai-failure-paths.md §1).";

/** `.catch(() => undefined | null | false | "" | 0 | [] | {})`, with or without a parameter. */
const swallowingCatch = [
  "Literal",
  "ArrayExpression[elements.length=0]",
  "ObjectExpression[properties.length=0]",
  "Identifier[name='undefined']",
].map((body) => ({
  selector: `CallExpression[callee.property.name='catch'] > ArrowFunctionExpression.arguments > ${body}.body`,
  message: SWALLOWING_CATCH,
}));

const processEnv = {
  selector: "MemberExpression[object.name='process'][property.name='env']",
  message:
    "Read the environment in config/index.ts, through a reader with both bounds and a default (ts-ai-style.md, Config).",
};

export default tseslint.config(
  {
    ignores: [
      "node_modules/**",
      ".yarn/**",
      "app/generated/**",
      "templates/**",
      "coverage/**",
      "server.js",
    ],
  },
  {
    files: ["**/*.ts"],
    languageOptions: {
      parser: tseslint.parser,
      parserOptions: {
        projectService: { allowDefaultProject: ["vitest.config.ts"] },
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: { "@typescript-eslint": tseslint.plugin },
    linterOptions: { reportUnusedDisableDirectives: "error" },
    rules: {
      // A promise nobody awaits: its rejection ends the process (Node >= 15) or is lost.
      "@typescript-eslint/no-floating-promises": "error",
      "@typescript-eslint/no-misused-promises": "error",
      // `return promise` inside try/finally lets the finally run before the promise settles:
      // the timeout was cancelled while the body was still being read (5bc3aae36e).
      "@typescript-eslint/return-await": ["error", "error-handling-correctness-only"],
      "@typescript-eslint/only-throw-error": "error",
      "@typescript-eslint/switch-exhaustiveness-check": "error",
      "@typescript-eslint/no-explicit-any": "error",
      "@typescript-eslint/no-non-null-assertion": "error",
      "@typescript-eslint/ban-ts-comment": "error",
      "@typescript-eslint/consistent-type-imports": [
        "error",
        { fixStyle: "separate-type-imports", disallowTypeAnnotations: false },
      ],
      "no-empty": ["error", { allowEmptyCatch: false }],
      "no-console": "error",
      "no-restricted-syntax": ["error", ...swallowingCatch, processEnv],
      // Every upstream has one client that owns the timeout, the envelope and the error class.
      "no-restricted-globals": [
        "error",
        {
          name: "fetch",
          message:
            "Call the upstream through its client (aiService.* / the Files API helpers); a bare fetch has no timeout and no error class (ts-ai-style.md, Upstream calls).",
        },
      ],
    },
  },
  {
    // A request body is narrowed field by field, never cast: `toolNames` given as a string
    // was once spread into characters (eafd5d3872).
    files: ["app/controllers/**/*.ts"],
    ignores: ["**/__tests__/**"],
    rules: {
      "@typescript-eslint/consistent-type-assertions": ["error", { assertionStyle: "never" }],
    },
  },
  {
    // The one place that reads the environment; it runs before the logger exists.
    files: ["config/**/*.ts"],
    rules: { "no-console": "off", "no-restricted-syntax": ["error", ...swallowingCatch] },
  },
  {
    // The AI service client is the place a bare fetch belongs.
    files: ["app/storage/httpClient.ts"],
    rules: { "no-restricted-globals": "off" },
  },
  {
    files: ["scripts/**/*.ts"],
    rules: { "no-console": "off", "no-restricted-syntax": "off", "no-restricted-globals": "off" },
  },
  {
    // The one place that wraps console.log, to drop the two lines config/index.ts prints.
    files: ["app/__tests__/setup.ts"],
    rules: { "no-console": "off" },
  },
  {
    files: ["**/__tests__/**/*.ts"],
    rules: {
      "@typescript-eslint/no-non-null-assertion": "off",
      "@typescript-eslint/no-explicit-any": "off",
      "no-restricted-syntax": "off",
      "no-restricted-globals": "off",
    },
  },
);
