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

// The data of the AI projects mock: its shapes and the scenarios it is seeded
// with. A scenario is built once per caller (see store.ts), so every user who
// opens the page gets the same world with themselves in the roles the
// scenario gives "me". The default scenario is a catalogue of what can break
// the UI — long, unbroken, right-to-left and markup-like titles, the most
// rooms a project may link, rooms with hundreds of skills or thousands of
// files, gone and archived rooms, a deleted model, hundreds of members, an
// expired invitation link, a long history — rather than a realistic portal.

import { MOCK_PROJECT_ID_BASE, MOCK_ROOM_ID_BASE } from "./registry.js";

export const INSTRUCTIONS = 10;
export const KNOWLEDGE = 11;

/** `FileShare` values a project uses. */
export const Access = { None: 0, Owner: 1, ProjectManager: 9, Member: 11 } as const;
export type Role = 0 | 1 | 9 | 11;

export interface MockUser {
  id: string;
  displayName: string;
  email: string;
  isOwner: boolean;
  isAdmin: boolean;
  isRoomAdmin: boolean;
  isCollaborator: boolean;
  isVisitor: boolean;
  /** 1 activated, 2 pending (invited by e-mail, never signed in). */
  activationStatus: 1 | 2;
}

export interface MockGroup {
  id: string;
  name: string;
  membersCount: number;
}

export interface MockSkill {
  id: string;
  name: string;
  description: string;
}

export interface MockFile {
  id: number;
  title: string;
  fileExst: string;
  /** 0 in progress, 1 completed, 2 failed, null never queued. */
  vectorizationStatus: 0 | 1 | 2 | null;
  folderId: number;
  /** A real room's file: the caller may queue it again (`security.Vectorization`). */
  canRetry?: boolean;
}

export interface MockRoom {
  id: number;
  title: string;
  roomType: typeof INSTRUCTIONS | typeof KNOWLEDGE;
  color: string;
  /** Gone, wrong type or unreadable for the caller: `available: false`, `title: null`. */
  available: boolean;
  archived: boolean;
  /** A real portal room picked in the dialog; its skills and files are read live. */
  real: boolean;
  /** Granting Viewer on it is reported as skipped (`grantSkipped`). */
  grantFails?: boolean;
  skills: MockSkill[];
  files: MockFile[];
  /** More files than one listing returns (`truncated`), for the totals. */
  totalFiles?: number;
}

export interface MockMember {
  subject: { kind: "user"; user: MockUser } | { kind: "group"; group: MockGroup };
  access: 9 | 11;
}

export interface MockLink {
  id: string;
  title: string;
  expirationDate: string | null;
  maxUseCount: number | null;
  currentUseCount: number;
  requestToken: string;
}

export interface MockHistoryEvent {
  id: number;
  key: string;
  actionId: number;
  date: string;
  initiator: MockUser;
  data: Record<string, unknown>;
  related: {
    id: number;
    key: string;
    actionId: number;
    date: string;
    initiator: MockUser;
    data: Record<string, unknown>;
  }[];
}

/** What the caller is in a project: the owner, a member with a role, or an admin outside it. */
export type MyStanding =
  { kind: "owner" } | { kind: "member"; access: 9 | 11; viaGroup?: boolean } | { kind: "outside" };

export interface MockProject {
  id: number;
  title: string;
  color: string;
  cover: string | null;
  /** An uploaded logo (sizes point at an inline image). */
  hasLogo: boolean;
  owner: MockUser;
  me: MyStanding;
  created: string;
  updated: string;
  pinned: boolean;
  mute: boolean;
  /** `null` = the portal default model. */
  profileId: string | null;
  instructionsRoomIds: number[];
  knowledgeRoomIds: number[];
  /** Everyone but the owner and the caller. */
  members: MockMember[];
  link: MockLink | null;
  history: MockHistoryEvent[];
  /** Answer every read of this project with this status (error paths). */
  failWith?: 400 | 403 | 500;
  /** Delay every read of this project's sources by this many ms (loaders). */
  slowMs?: number;
}

/** The chat profiles a scenario may use: real ones of the portal, read when the world is built. */
export interface ProfileChoice {
  /** A real chat profile, or `undefined` when the portal has none. */
  real?: { id: string; name: string };
}

export const DELETED_PROFILE_ID = "00000000-dead-4bad-8000-00000000d00d";

export const SCENARIOS = ["default", "many", "empty", "one"] as const;
export type Scenario = (typeof SCENARIOS)[number];

export interface MockWorld {
  me: MockUser;
  users: MockUser[];
  groups: MockGroup[];
  rooms: Map<number, MockRoom>;
  projects: Map<number, MockProject>;
}

/* ------------------------------ Generators ------------------------------- */

let nextProjectId = MOCK_PROJECT_ID_BASE + 1;
let nextRoomId = MOCK_ROOM_ID_BASE + 1;
let nextFileId = 970_000_001;
let nextEventId = 1;

export const newProjectId = (): number => nextProjectId++;
const newRoomId = (): number => nextRoomId++;
const newFileId = (): number => nextFileId++;
export const newEventId = (): number => nextEventId++;

/** A deterministic generator, so every world built from a scenario is the same. */
function prng(seed: number): () => number {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 0x100000000;
  };
}

const pick = <T>(rnd: () => number, list: readonly T[]): T =>
  list[Math.floor(rnd() * list.length)] as T;

// The portal's room logo palette (`files:logocolors` in appsettings.json),
// the colours a room gets when nobody picks one.
const COLORS = [
  "FF6680",
  "FF8F40",
  "F2D230",
  "61C059",
  "70E096",
  "1FCECB",
  "5CC3F7",
  "6191F2",
  "7757D9",
  "B679F2",
  "FF7FD4",
];

const FIRST = [
  "Anna",
  "Pavel",
  "Maria",
  "John",
  "Li",
  "Fatima",
  "Olga",
  "Kenji",
  "Søren",
  "Zoë",
  "Ahmed",
  "Chloé",
  "Ivan",
  "Priya",
  "Lucas",
];
const LAST = [
  "Smith",
  "Ivanova",
  "García",
  "Nakamura",
  "Müller",
  "O'Connor",
  "Dubois",
  "Kowalski",
  "Rossi",
  "Hansen",
  "Al-Farsi",
  "Chen",
  "Petrov",
];

export function guid(n: number, tag = 0): string {
  const hex = (v: number, len: number) => v.toString(16).padStart(len, "0").slice(-len);
  return `${hex(0xa1f0c3b2 + tag, 8)}-0000-4000-8000-${hex(n, 12)}`;
}

function user(n: number, over: Partial<MockUser> = {}): MockUser {
  const rnd = prng(n * 7919);
  const displayName = `${pick(rnd, FIRST)} ${pick(rnd, LAST)}`;
  return {
    id: guid(n, 1),
    displayName,
    email: `${displayName.toLowerCase().replace(/[^a-z]+/g, ".")}.${n}@example.com`,
    isOwner: false,
    isAdmin: false,
    isRoomAdmin: false,
    isCollaborator: false,
    isVisitor: false,
    activationStatus: 1,
    ...over,
  };
}

/** The pool of people the members, owners and history draw from. */
function buildUsers(): MockUser[] {
  const users: MockUser[] = [];
  users.push(user(1, { displayName: "Portal Owner", isOwner: true, isAdmin: true }));
  users.push(user(2, { displayName: "Anna Admin", isAdmin: true }));
  users.push(user(3, { displayName: "Pavel Room-Admin", isRoomAdmin: true }));
  users.push(
    user(4, {
      displayName: "Maximilian Alexander Friedrich Wilhelm von Hohenzollern-Sigmaringen der Dritte",
      email:
        "maximilian.alexander.friedrich.wilhelm.von.hohenzollern-sigmaringen@very-long-subdomain.example-company.com",
      isRoomAdmin: true,
    }),
  );
  users.push(user(5, { displayName: "محمد عبد الرحمن", isRoomAdmin: true }));
  users.push(user(6, { displayName: "李小龙", isCollaborator: true }));
  users.push(user(7, { displayName: "<script>alert(1)</script>", isCollaborator: true }));
  users.push(user(8, { displayName: "Guest Gabriela", isVisitor: true }));
  users.push(
    user(9, {
      displayName: "pending.invitee.with.a.really.long.address@example.com",
      email: "pending.invitee.with.a.really.long.address@example.com",
      isVisitor: true,
      activationStatus: 2,
    }),
  );
  users.push(user(10, { displayName: "😀 Emoji Person 🎉", isCollaborator: true }));
  for (let n = 11; n <= 320; n += 1) {
    const kind = n % 10;
    users.push(
      user(n, {
        isRoomAdmin: kind === 0 || kind === 1,
        isCollaborator: kind >= 2 && kind <= 6,
        isVisitor: kind >= 7,
        activationStatus: kind === 9 ? 2 : 1,
      }),
    );
  }
  return users;
}

function buildGroups(): MockGroup[] {
  return [
    { id: guid(1, 2), name: "Sales", membersCount: 12 },
    {
      id: guid(2, 2),
      name: "Marketing & Communications (EMEA, APAC and the Americas) — extended team",
      membersCount: 148,
    },
    { id: guid(3, 2), name: "Δ Research", membersCount: 3 },
    { id: guid(4, 2), name: "Empty group", membersCount: 0 },
  ];
}

const LONG_WORD = "Supercalifragilisticexpialidocious".repeat(5).slice(0, 170);
const LONG_TITLE = (
  "Quarterly marketing performance review and customer research synthesis for the EMEA, " +
  "APAC and Americas regions including competitive landscape analysis and next steps"
).slice(0, 170);

const SKILL_NAMES = [
  "tone-of-voice",
  "glossary",
  "press-release",
  "legal-disclaimer",
  "seo-headlines",
  "translate-to-german",
  "summarise-meeting",
  "brand_guidelines_v2_final_FINAL",
];

function skills(rnd: () => number, count: number, prefix = ""): MockSkill[] {
  const list: MockSkill[] = [];
  for (let i = 0; i < count; i += 1) {
    const base = SKILL_NAMES[i % SKILL_NAMES.length] as string;
    const name = `${prefix}${base}${i >= SKILL_NAMES.length ? `-${i}` : ""}`;
    const description =
      i % 7 === 3
        ? ""
        : i % 7 === 5
          ? "A very long description that keeps going: " +
            "use the brand voice, check the glossary, cite sources and never invent numbers. ".repeat(
              6,
            )
          : `Writes ${base.replace(/[-_]/g, " ")} in the company style. Use **bold** for terms; \`code\` stays as is.`;
    list.push({ id: String(980_000_000 + Math.floor(rnd() * 9_000_000)), name, description });
  }
  return list;
}

const EXTENSIONS = [
  ".docx",
  ".xlsx",
  ".pptx",
  ".pdf",
  ".md",
  ".txt",
  ".csv",
  ".png",
  ".zip",
  ".odt",
  ".epub",
  ".weird",
];

function files(rnd: () => number, roomId: number, count: number): MockFile[] {
  const list: MockFile[] = [];
  for (let i = 0; i < count; i += 1) {
    const ext = EXTENSIONS[i % EXTENSIONS.length] as string;
    const title =
      i % 17 === 4
        ? `Annual report — final version approved by the board of directors after the third review round (copy ${i})${ext}`
        : i % 17 === 9
          ? `Отчёт_${i}${ext}`
          : `Document ${String(i + 1).padStart(4, "0")}${ext}`;
    const r = rnd();
    list.push({
      id: newFileId(),
      title,
      fileExst: ext,
      vectorizationStatus: r < 0.7 ? 1 : r < 0.85 ? 0 : r < 0.93 ? 2 : null,
      folderId: i % 5 === 0 ? roomId + 100_000 : roomId,
    });
  }
  return list;
}

interface RoomSpec {
  title: string;
  skills?: number;
  files?: number;
  totalFiles?: number;
  available?: boolean;
  archived?: boolean;
  grantFails?: boolean;
}

function room(
  world: MockWorld,
  rnd: () => number,
  kind: typeof INSTRUCTIONS | typeof KNOWLEDGE,
  spec: RoomSpec,
): number {
  const id = newRoomId();
  world.rooms.set(id, {
    id,
    title: spec.title,
    roomType: kind,
    color: pick(rnd, COLORS),
    available: spec.available ?? true,
    archived: spec.archived ?? false,
    real: false,
    grantFails: spec.grantFails,
    skills: kind === INSTRUCTIONS ? skills(rnd, spec.skills ?? 3) : [],
    files: kind === KNOWLEDGE ? files(rnd, id, spec.files ?? 6) : [],
    totalFiles: spec.totalFiles,
  });
  return id;
}

function iso(daysAgo: number, minutes = 0): string {
  const d = new Date(Date.UTC(2026, 9, 28, 9, 0, 0) - daysAgo * 86_400_000 - minutes * 60_000);
  return d.toISOString().replace("Z", "+00:00");
}

function members(
  world: MockWorld,
  from: number,
  count: number,
  opts: { groups?: boolean } = {},
): MockMember[] {
  const list: MockMember[] = [];
  const pool = world.users.slice(1);
  for (let i = 0; i < count && from + i < pool.length; i += 1) {
    const u = pool[from + i] as MockUser;
    const canManage = u.isAdmin || u.isRoomAdmin || u.isOwner;
    list.push({ subject: { kind: "user", user: u }, access: canManage && i % 3 === 0 ? 9 : 11 });
  }
  if (opts.groups) {
    for (const group of world.groups) {
      list.push({ subject: { kind: "group", group }, access: 11 });
    }
  }
  return list;
}

const HISTORY_KEYS: [string, number][] = [
  ["RoomCreated", 5001],
  ["RoomRenamed", 5003],
  ["RoomCreateUser", 5070],
  ["RoomUpdateAccessForUser", 5071],
  ["RoomRemoveUser", 5072],
  ["RoomGroupAdded", 5073],
  ["RoomColorChanged", 5100],
  ["RoomCoverChanged", 5101],
  ["RoomLogoCreated", 5102],
  ["RoomInviteResend", 5074],
  ["RoomChangeOwner", 5075],
  // Project events the client does not draw yet: they must be skipped, not crash.
  ["AiProjectModelChanged", 5167],
  ["AiProjectRoomLinked", 5168],
  ["AiProjectRoomUnlinked", 5169],
];

export function historyEvent(
  key: string,
  actionId: number,
  initiator: MockUser,
  date: string,
  data: Record<string, unknown>,
): MockHistoryEvent {
  return { id: newEventId(), key, actionId, date, initiator, data, related: [] };
}

function history(world: MockWorld, project: MockProject, count: number): MockHistoryEvent[] {
  const rnd = prng(project.id);
  const events: MockHistoryEvent[] = [];
  for (let i = 0; i < count; i += 1) {
    const [key, actionId] = HISTORY_KEYS[i % HISTORY_KEYS.length] as [string, number];
    const initiator = pick(rnd, world.users.slice(0, 12));
    const target = pick(rnd, world.users.slice(10));
    const data: Record<string, unknown> =
      key === "RoomRenamed"
        ? {
            oldTitle: `Old title ${i}`,
            newTitle: i % 4 === 0 ? LONG_TITLE : project.title,
            id: project.id,
          }
        : key === "RoomCreateUser" || key === "RoomRemoveUser" || key === "RoomInviteResend"
          ? { user: target, access: 11, id: project.id, title: project.title }
          : key === "RoomUpdateAccessForUser"
            ? { user: target, access: 9, oldAccess: 11, id: project.id, title: project.title }
            : key === "RoomGroupAdded"
              ? {
                  group: { id: world.groups[1]?.id, name: world.groups[1]?.name },
                  access: 11,
                  id: project.id,
                  title: project.title,
                }
              : key === "RoomChangeOwner"
                ? {
                    owner: project.owner,
                    oldOwner: initiator,
                    id: project.id,
                    title: project.title,
                  }
                : { id: project.id, title: project.title };
    const event = historyEvent(key, actionId, initiator, iso(Math.floor(i / 3), i * 7), data);
    // A burst of invitations collapses into one entry with related ones.
    if (key === "RoomCreateUser" && i % 28 === 2) {
      for (let r = 0; r < 12; r += 1) {
        event.related.push({
          id: newEventId(),
          key,
          actionId,
          date: event.date,
          initiator,
          data: { user: world.users[20 + r], access: 11, id: project.id, title: project.title },
        });
      }
    }
    events.push(event);
  }
  return events;
}

function link(over: Partial<MockLink> = {}): MockLink {
  return {
    id: guid(newEventId(), 3),
    title: "Invite",
    expirationDate: iso(-7),
    maxUseCount: null,
    currentUseCount: 0,
    requestToken: `mock-token-${newEventId()}`,
    ...over,
  };
}

interface ProjectSpec {
  title: string;
  me: MyStanding;
  owner?: MockUser;
  instructions?: number[];
  knowledge?: number[];
  profile?: "real" | "default" | "deleted";
  pinned?: boolean;
  mute?: boolean;
  cover?: string | null;
  hasLogo?: boolean;
  members?: MockMember[];
  link?: MockLink | null;
  historyCount?: number;
  createdDaysAgo?: number;
  failWith?: 400 | 403 | 500;
  slowMs?: number;
}

function project(world: MockWorld, profiles: ProfileChoice, spec: ProjectSpec): MockProject {
  const id = newProjectId();
  const rnd = prng(id);
  const owner = spec.me.kind === "owner" ? world.me : (spec.owner ?? (world.users[1] as MockUser));
  const created = iso(spec.createdDaysAgo ?? Math.floor(rnd() * 120) + 1);
  const p: MockProject = {
    id,
    title: spec.title,
    color: pick(rnd, COLORS),
    cover: spec.cover === undefined ? (rnd() < 0.5 ? "bookmark" : null) : spec.cover,
    hasLogo: spec.hasLogo ?? false,
    owner,
    me: spec.me,
    created,
    updated: iso(Math.floor(rnd() * 3), Math.floor(rnd() * 600)),
    pinned: spec.pinned ?? false,
    mute: spec.mute ?? false,
    profileId:
      spec.profile === "deleted"
        ? DELETED_PROFILE_ID
        : spec.profile === "real" && profiles.real
          ? profiles.real.id
          : null,
    instructionsRoomIds: spec.instructions ?? [],
    knowledgeRoomIds: spec.knowledge ?? [],
    members: spec.members ?? members(world, Math.floor(rnd() * 40) + 10, 3),
    link: spec.link === undefined ? link() : spec.link,
    history: [],
    failWith: spec.failWith,
    slowMs: spec.slowMs,
  };
  p.history = history(world, p, spec.historyCount ?? 6);
  world.projects.set(id, p);
  return p;
}

/* ------------------------------- Scenarios ------------------------------- */

function edgeCases(world: MockWorld, profiles: ProfileChoice): void {
  const rnd = prng(42);
  const ins = (spec: RoomSpec) => room(world, rnd, INSTRUCTIONS, spec);
  const kn = (spec: RoomSpec) => room(world, rnd, KNOWLEDGE, spec);
  const admin = world.users[1] as MockUser;
  const roomAdmin = world.users[2] as MockUser;
  const longNameAdmin = world.users[3] as MockUser;

  const brand = ins({ title: "Brand voice", skills: 4 });
  const legal = ins({ title: "Legal style", skills: 2 });
  const noSkills = ins({ title: "Instructions without any skills", skills: 0 });
  const catalogue = kn({ title: "Product catalogue", files: 24 });
  const emptyKnowledge = kn({ title: "Empty knowledge room", files: 0 });

  project(world, profiles, {
    title: "Marketing copy",
    me: { kind: "owner" },
    instructions: [brand, legal, noSkills],
    knowledge: [catalogue, emptyKnowledge],
    profile: "real",
    pinned: true,
    historyCount: 40,
    members: members(world, 10, 8, { groups: true }),
  });
  project(world, profiles, {
    title: LONG_TITLE,
    me: { kind: "member", access: 9 },
    owner: longNameAdmin,
    instructions: [ins({ title: LONG_TITLE, skills: 5 })],
    knowledge: [kn({ title: `${LONG_TITLE} (knowledge)`, files: 8 })],
    profile: "real",
    pinned: true,
  });
  project(world, profiles, {
    title: LONG_WORD,
    me: { kind: "member", access: 11 },
    owner: roomAdmin,
    instructions: [ins({ title: LONG_WORD, skills: 2 })],
    knowledge: [kn({ title: LONG_WORD.toLowerCase(), files: 3 })],
    pinned: true,
  });
  project(world, profiles, {
    title: "🚀 Launch 🎉 Q4 ✨ — 👩‍💻🧑‍🚀",
    me: { kind: "owner" },
    instructions: [ins({ title: "😀 Emoji instructions 🎯", skills: 3 })],
    cover: null,
  });
  project(world, profiles, {
    title: "مشروع التسويق الرقمي للربع الرابع",
    me: { kind: "member", access: 11 },
    owner: world.users[4] as MockUser,
    instructions: [ins({ title: "تعليمات الكتابة", skills: 2 })],
    knowledge: [kn({ title: "قاعدة المعرفة", files: 5 })],
  });
  project(world, profiles, {
    title: `<b>Bold</b> & "quotes" 'single' \`code\` **md** <img src=x onerror=alert(1)>`,
    me: { kind: "owner" },
    instructions: [ins({ title: "<i>markup</i> & ampersand <room>", skills: 1 })],
  });
  project(world, profiles, { title: "A", me: { kind: "owner" }, cover: null, link: null });
  project(world, profiles, {
    title: "人工智能项目 — 季度报告与客户研究",
    me: { kind: "member", access: 9 },
    owner: admin,
    knowledge: [kn({ title: "知识库", files: 4 })],
  });

  // The limits: 20 rooms of each kind, each with a long title.
  const manyIns: number[] = [];
  const manyKn: number[] = [];
  for (let i = 1; i <= 20; i += 1) {
    manyIns.push(
      ins({
        title: `Instructions room number ${i} with a fairly long descriptive title`,
        skills: (i % 4) + 1,
      }),
    );
    manyKn.push(
      kn({
        title: `Knowledge base ${i}: contracts, invoices, specifications and meeting notes`,
        files: (i % 5) * 3,
      }),
    );
  }
  project(world, profiles, {
    title: "20 Instructions + 20 Knowledge rooms (the limit)",
    me: { kind: "owner" },
    instructions: manyIns,
    knowledge: manyKn,
    profile: "real",
    pinned: true,
  });

  project(world, profiles, {
    title: "150 skills in one room",
    me: { kind: "member", access: 9 },
    owner: admin,
    instructions: [
      ins({ title: "Skill library", skills: 150 }),
      ins({ title: "Second library", skills: 60 }),
    ],
  });
  project(world, profiles, {
    title: "Huge knowledge room (truncated listing)",
    me: { kind: "owner" },
    knowledge: [
      kn({ title: "Archive of everything", files: 1000, totalFiles: 2400 }),
      kn({ title: "Medium room", files: 180 }),
    ],
  });
  project(world, profiles, {
    title: "Every linked room is gone or unreadable",
    me: { kind: "member", access: 11 },
    owner: roomAdmin,
    instructions: [
      ins({ title: "Deleted instructions", available: false }),
      ins({ title: "No access", available: false }),
    ],
    knowledge: [kn({ title: "Deleted knowledge", available: false })],
  });
  project(world, profiles, {
    title: "Archived and gone rooms mixed with live ones",
    me: { kind: "owner" },
    instructions: [
      brand,
      ins({ title: "Archived instructions", archived: true, skills: 2 }),
      ins({ title: "Gone", available: false }),
    ],
    knowledge: [kn({ title: "Archived knowledge", archived: true, files: 4 }), catalogue],
  });
  project(world, profiles, {
    title: "Model was deleted (chat answers 409)",
    me: { kind: "owner" },
    instructions: [legal],
    profile: "deleted",
  });
  project(world, profiles, {
    title: "Portal default model",
    me: { kind: "member", access: 11 },
    owner: admin,
    profile: "default",
    knowledge: [catalogue],
  });
  project(world, profiles, {
    title: "No rooms linked at all",
    me: { kind: "owner" },
    link: null,
    members: [],
  });
  project(world, profiles, {
    title: "Only instructions",
    me: { kind: "member", access: 11 },
    owner: admin,
    instructions: [brand],
  });
  project(world, profiles, {
    title: "Only knowledge",
    me: { kind: "member", access: 9 },
    owner: roomAdmin,
    knowledge: [catalogue],
  });
  project(world, profiles, {
    title: "Muted project",
    me: { kind: "member", access: 11 },
    owner: admin,
    mute: true,
    instructions: [brand],
  });
  project(world, profiles, {
    title: "Finance (I am an admin, not a member)",
    me: { kind: "outside" },
    owner: roomAdmin,
    instructions: [legal],
    knowledge: [catalogue],
  });
  project(world, profiles, {
    title: "260 members, groups, guests and pending invites",
    me: { kind: "owner" },
    members: members(world, 0, 260, { groups: true }),
    historyCount: 120,
    instructions: [brand],
  });
  project(world, profiles, {
    title: "Expired invitation link",
    me: { kind: "member", access: 9 },
    owner: admin,
    link: link({ expirationDate: iso(3), title: "Old link" }),
  });
  project(world, profiles, {
    title: "Invitation link used up (25 of 25)",
    me: { kind: "owner" },
    link: link({
      maxUseCount: 25,
      currentUseCount: 25,
      title: "A link with a very long name that should be truncated somewhere in the panel",
    }),
  });
  project(world, profiles, {
    title: "Uploaded logo",
    me: { kind: "owner" },
    hasLogo: true,
    instructions: [brand],
  });
  project(world, profiles, { title: "Cover and colour", me: { kind: "owner" }, cover: "bookmark" });
  project(world, profiles, {
    title: "Member through a group only (cannot leave)",
    me: { kind: "member", access: 11, viaGroup: true },
    owner: admin,
  });
  project(world, profiles, {
    title: "Grant fails on a linked room",
    me: { kind: "owner" },
    instructions: [ins({ title: "Restricted room", grantFails: true, skills: 1 })],
  });
  project(world, profiles, {
    title: "Broken: every read answers 500",
    me: { kind: "owner" },
    failWith: 500,
    instructions: [brand],
  });
  project(world, profiles, {
    title: "Slow sources (4 s)",
    me: { kind: "owner" },
    slowMs: 4000,
    instructions: [brand],
    knowledge: [catalogue],
  });
  project(world, profiles, {
    title: "Very old project",
    me: { kind: "member", access: 11 },
    owner: admin,
    createdDaysAgo: 9000,
  });
  project(world, profiles, {
    title: "Pinned 5",
    me: { kind: "member", access: 11 },
    owner: admin,
    pinned: true,
  });
  project(world, profiles, {
    title: "Pinned 6",
    me: { kind: "member", access: 11 },
    owner: admin,
    pinned: true,
  });
  project(world, profiles, { title: "Pinned 7", me: { kind: "owner" }, pinned: true });
  project(world, profiles, { title: "Pinned 8", me: { kind: "owner" }, pinned: true });
  project(world, profiles, {
    title: "Pinned 9 (one more pin is allowed, the 11th is refused)",
    me: { kind: "owner" },
    pinned: true,
  });
  project(world, profiles, {
    title: "Error triggers: rename to #400, #403 or #500",
    me: { kind: "owner" },
    instructions: [brand],
  });
}

function filler(world: MockWorld, profiles: ProfileChoice, count: number): void {
  const rnd = prng(7);
  const shared = room(world, rnd, INSTRUCTIONS, { title: "Shared instructions", skills: 2 });
  for (let i = 1; i <= count; i += 1) {
    const standing: MyStanding =
      i % 3 === 0 ? { kind: "owner" } : { kind: "member", access: i % 3 === 1 ? 9 : 11 };
    project(world, profiles, {
      title: `Project ${String(i).padStart(4, "0")} ${pick(rnd, ["alpha", "beta", "gamma", "delta", "release notes", "support"])}`,
      me: standing,
      owner: world.users[1 + (i % 3)] as MockUser,
      instructions: i % 2 === 0 ? [shared] : [],
      profile: i % 5 === 0 ? "real" : "default",
      historyCount: 2,
    });
  }
}

/** Builds the world of one caller for a scenario. */
export function buildWorld(me: MockUser, scenario: Scenario, profiles: ProfileChoice): MockWorld {
  const world: MockWorld = {
    me,
    users: [],
    groups: buildGroups(),
    rooms: new Map(),
    projects: new Map(),
  };
  world.users = [me, ...buildUsers().filter((u) => u.id !== me.id)];
  if (scenario === "default") {
    edgeCases(world, profiles);
  } else if (scenario === "many") {
    edgeCases(world, profiles);
    filler(world, profiles, 300);
  } else if (scenario === "one") {
    const rnd = prng(1);
    project(world, profiles, {
      title: "My first project",
      me: { kind: "owner" },
      instructions: [room(world, rnd, INSTRUCTIONS, { title: "Brand voice", skills: 3 })],
      knowledge: [room(world, rnd, KNOWLEDGE, { title: "Product catalogue", files: 5 })],
      profile: "real",
    });
  }
  return world;
}
