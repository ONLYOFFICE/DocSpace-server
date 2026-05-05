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
