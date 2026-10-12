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

// Minimal ambient stubs for optional peer dependencies of @onlyoffice/ai-chat
// that the server side never installs (UI-only peers).

declare module "date-and-time" {
  interface DateAndTime {
    format(date: Date, pattern: string): string;
    parse(input: string, pattern: string): Date;
    addDays(date: Date, n: number): Date;
    addMonths(date: Date, n: number): Date;
    addYears(date: Date, n: number): Date;
    addHours(date: Date, n: number): Date;
    addMinutes(date: Date, n: number): Date;
    addSeconds(date: Date, n: number): Date;
    isSameDay(a: Date, b: Date): boolean;
    isValid(input: string, pattern: string): boolean;
    subtract(a: Date, b: Date): { toDays(): number; toHours(): number; toMinutes(): number; toSeconds(): number };
  }
  const date: DateAndTime;
  export default date;
}

declare module "@assistant-ui/react" {
  export type ThreadMessageLike = Record<string, unknown> & {
    id?: string;
    createdAt?: Date | number;
  };
}

declare module "@assistant-ui/react-markdown" {}
declare module "@codemirror/lang-json" {}
declare module "@codemirror/state" {}
declare module "@codemirror/view" {}
declare module "@radix-ui/react-dialog" {}
declare module "@radix-ui/react-dropdown-menu" {}
declare module "@radix-ui/react-slot" {}
declare module "@radix-ui/react-switch" {}
declare module "@radix-ui/react-tabs" {}
declare module "@radix-ui/react-tooltip" {}
declare module "assistant-stream" {}
declare module "class-variance-authority" {}
declare module "clsx" {}
declare module "codemirror" {}
declare module "framer-motion" {}
declare module "i18next" {}
declare module "react" {}
declare module "react-dom" {}
declare module "react-i18next" {}
declare module "react-shiki" {}
declare module "react-svg" {}
declare module "remark-gfm" {}
declare module "tailwind-merge" {}
declare module "zustand" {}
