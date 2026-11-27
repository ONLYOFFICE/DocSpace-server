// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
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

const CATCH_HANDLER = "CallExpression[callee.property.name='catch'] > :function.arguments";
const CONSTANTS = [
  "Literal",
  "ArrayExpression[elements.length=0]",
  "ObjectExpression[properties.length=0]",
  "Identifier[name='undefined']",
];

/**
 * A `.catch()` handler, arrow or function, that answers a constant: `() => null`, a block that
 * returns `undefined`, `null`, `false`, `""`, `0`, `[]` or `{}` (a log line before it changes
 * nothing), a bare `return;`, or a block that neither returns nor throws. A block that throws
 * as well (the status that means absent answers a constant, the rest is rethrown) is left
 * alone. A handler made by a factory (`.catch(tolerate(...))`) is not seen.
 */
const swallowingCatch = [
  ...CONSTANTS.map((value) => `${CATCH_HANDLER} > ${value}.body`),
  ...CONSTANTS.map(
    (value) =>
      `${CATCH_HANDLER} > BlockStatement:not(:has(ThrowStatement)) ReturnStatement > ${value}.argument`,
  ),
  `${CATCH_HANDLER} > BlockStatement:not(:has(ThrowStatement)) ReturnStatement[argument=null]`,
  // A handler that neither returns nor throws, where the value of `.catch()` is read: one on a
  // side branch (`p.catch(() => { cache = undefined; })`) only keeps the rejection handled.
  `:not(ExpressionStatement) > ${CATCH_HANDLER} > BlockStatement.body:not(:has(ReturnStatement)):not(:has(ThrowStatement))`,
].map((selector) => ({ selector, message: SWALLOWING_CATCH }));

const processEnv = {
  selector: "MemberExpression[object.name='process'][property.name='env']",
  message:
    "Read the environment in config/index.ts, through a reader with both bounds and a default (ts-ai-style.md, Config).",
};

const unboundedFanOut = {
  selector:
    "CallExpression[callee.object.name='Promise'][callee.property.name=/^all(Settled)?$/] > CallExpression.arguments[callee.property.name='map']",
  message:
    "Promise.all over a mapped list starts every call at once. Calls made in a loop for one request go through pooled(items, limit, run) (ts-ai-style.md, Upstream calls).",
};

export default tseslint.config(
  {
    ignores: [
      "node_modules/**",
      ".yarn/**",
      "app/generated/**",
      "templates/**",
      "skills/**",
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
      "no-restricted-syntax": ["error", ...swallowingCatch, processEnv, unboundedFanOut],
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
    rules: {
      "no-console": "off",
      "no-restricted-syntax": ["error", ...swallowingCatch, unboundedFanOut],
    },
  },
  {
    // The clients of the AI service, the Files API and the People API are the places a bare
    // fetch belongs: each owns the timeout, the envelope and the error class of its upstream.
    files: [
      "app/storage/httpClient.ts",
      "app/storage/docspaceFilesApi.ts",
      "app/storage/docspacePeopleApi.ts",
    ],
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
