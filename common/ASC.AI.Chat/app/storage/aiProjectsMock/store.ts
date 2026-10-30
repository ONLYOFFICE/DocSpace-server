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

// The state and the behaviour of the AI projects mock. Each caller (the
// account behind the forwarded cookie) gets a world of their own, built from
// the current scenario the first time they call, so two testers never see
// each other's edits and "me" is always the real signed-in user. Every
// answer is shaped as the C# AiProjectsController answers (see
// client/packages/client/src/pages/AiProjects/spec/AiProjects-API-for-CSharp.md),
// including the rights table: a Member cannot edit, an admin outside the
// project cannot mute, the owner cannot leave, and so on.

import { ActionType, AssignmentsEngine, CapabilitiesUI } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { proxyBaseUrl, withTimeout } from "../httpClient.js";
import { getForwardedHeaders } from "../../requestContext.js";
import { getFolderInfo } from "../docspaceFilesApi.js";
import { getRoomSkills } from "../roomSkills.js";
import { storage } from "../index.js";
import { getArray, getBoolean, getNumber, getObject, getString, isObject } from "../../narrow.js";
import type { JsonObject } from "../../narrow.js";
import logger from "../../log.js";
import {
  buildWorld,
  DELETED_PROFILE_ID,
  guid,
  historyEvent,
  INSTRUCTIONS,
  KNOWLEDGE,
  newProjectId,
  SCENARIOS,
} from "./data.js";
import type {
  MockFile,
  MockHistoryEvent,
  MockLink,
  MockMember,
  MockProject,
  MockRoom,
  MockUser,
  MockWorld,
  MyStanding,
  Role,
  Scenario,
} from "./data.js";
import { MOCK_PROJECTS_ROOT_ID, registerMockProject, unregisterMockProject } from "./registry.js";

/** What a mock route answers: an HTTP status and the JSON body. */
export interface MockReply {
  status: number;
  body: unknown;
}

const MAX_TITLE = 170;
const MAX_ROOMS_PER_KIND = 20;
const MAX_PINNED = 10;
const DEFAULT_COUNT = 100;

const LOGO_SVG =
  "data:image/svg+xml;utf8," +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96"><rect width="96" height="96" rx="12" fill="#4781D1"/><path d="M24 64 48 24l24 40z" fill="#fff"/></svg>',
  );
const BOOKMARK_COVER =
  '<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><path d="M10 6h12v20l-6-4-6 4z"/></svg>';

let scenario: Scenario = (() => {
  const fromEnv = process.env["AI_PROJECTS_MOCK_SCENARIO"];
  return (SCENARIOS as readonly string[]).includes(fromEnv ?? "")
    ? (fromEnv as Scenario)
    : "default";
})();

const DELAY_MS = Math.max(
  0,
  Number.parseInt(process.env["AI_PROJECTS_MOCK_DELAY_MS"] ?? "0", 10) || 0,
);

const worlds = new Map<string, MockWorld>();

const assignments = new AssignmentsEngine({ storage });

const ok = (response: unknown, extra: Record<string, unknown> = {}): MockReply => ({
  status: 200,
  body: { response, ...extra, status: 0, statusCode: 200 },
});

const fail = (status: number, error: string): MockReply => ({ status, body: { error } });

const NOT_FOUND = fail(404, "The AI project is not found");

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

/* -------------------------------- Caller -------------------------------- */

/** The signed-in account, read from the portal with the caller's own cookie. */
async function currentUser(): Promise<MockUser> {
  const url = `${proxyBaseUrl}/api/2.0/people/@self`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      throw Object.assign(new Error(`people/@self answered ${res.status}`), {
        status: res.status === 401 ? 401 : 502,
        expose: true,
      });
    }
    const json: unknown = await res.json();
    const raw = isObject(json) ? getObject(json, "response") : undefined;
    const id = raw ? getString(raw, "id") : undefined;
    if (!raw || !id) {
      throw new Error("people/@self: unparseable response");
    }
    return {
      id,
      displayName: getString(raw, "displayName") ?? "Me",
      email: getString(raw, "email") ?? "",
      isOwner: getBoolean(raw, "isOwner") === true,
      isAdmin: getBoolean(raw, "isAdmin") === true,
      isRoomAdmin: getBoolean(raw, "isRoomAdmin") === true,
      isCollaborator: getBoolean(raw, "isCollaborator") === true,
      isVisitor: getBoolean(raw, "isVisitor") === true,
      activationStatus: 1,
    };
  } finally {
    cancel();
  }
}

async function realProfiles(): Promise<Profile[]> {
  try {
    return (await storage.profiles.readAll()).filter(
      (p) => typeof p.capabilities !== "number" || (p.capabilities & CapabilitiesUI.Chat) !== 0,
    );
  } catch (err) {
    logger.warn(
      `AI projects mock: profiles not read: ${err instanceof Error ? err.message : String(err)}`,
    );
    return [];
  }
}

/** The caller and their world, built on the first call. */
async function context(): Promise<{ world: MockWorld; profiles: Profile[] }> {
  if (DELAY_MS > 0) {
    await sleep(DELAY_MS);
  }
  const me = await currentUser();
  const profiles = await realProfiles();
  let world = worlds.get(me.id);
  if (!world) {
    const first = profiles[0];
    world = buildWorld(me, scenario, {
      real: first ? { id: first.id, name: first.name } : undefined,
    });
    for (const p of world.projects.values()) {
      registerMockProject(p.id, p.title);
    }
    worlds.set(me.id, world);
    logger.info(
      `AI projects mock: built the "${scenario}" world for ${me.id} (${world.projects.size} projects)`,
    );
  }
  return { world, profiles };
}

/** Drops every world and starts the next calls from `next`. */
export function resetMock(next: string | undefined): MockReply {
  if (next !== undefined && !(SCENARIOS as readonly string[]).includes(next)) {
    return fail(400, `scenario must be one of ${SCENARIOS.join(", ")}`);
  }
  for (const world of worlds.values()) {
    for (const id of world.projects.keys()) {
      unregisterMockProject(id);
    }
  }
  worlds.clear();
  if (next) {
    scenario = next as Scenario;
  }
  return ok({ scenario, scenarios: SCENARIOS });
}

export function describeMock(): MockReply {
  return ok({ scenario, scenarios: SCENARIOS, delayMs: DELAY_MS, worlds: worlds.size });
}

/* --------------------------------- Rights -------------------------------- */

function role(p: MockProject): Role {
  return p.me.kind === "owner" ? 1 : p.me.kind === "member" ? p.me.access : 0;
}

const canCreate = (me: MockUser) => me.isOwner || me.isAdmin || me.isRoomAdmin;

/** Visible to the caller: a member of any kind, or a DocSpace admin outside it. */
function visible(world: MockWorld, p: MockProject): boolean {
  return p.me.kind !== "outside" || world.me.isAdmin || world.me.isOwner;
}

function security(p: MockProject) {
  const r = role(p);
  const owner = r === 1;
  const manager = r === 1 || r === 9;
  const inside = r !== 0;
  return {
    Read: true,
    Rename: manager,
    EditRoom: manager,
    EditAccess: manager,
    ChangeOwner: owner || !inside,
    Delete: owner || !inside,
    Pin: true,
    Mute: inside,
    Duplicate: manager || !inside,
    CopyLink: inside,
    UseChat: inside,
  };
}

/** The project when the caller may see it, else the 404 every route shares. */
function find(world: MockWorld, id: string): MockProject | MockReply {
  const p = world.projects.get(Number(id));
  if (!p || !visible(world, p)) {
    return NOT_FOUND;
  }
  if (p.failWith) {
    return fail(p.failWith, `Mock failure for "${p.title}" (HTTP ${p.failWith})`);
  }
  return p;
}

const isReply = (v: MockProject | MockReply): v is MockReply => "status" in v && "body" in v;

const titleTrigger = (title: string): MockReply | undefined => {
  if (title.includes("#400")) {
    return fail(
      400,
      "The project could not be saved: the title contains a test trigger. This message is deliberately long so that the toast has to wrap over several lines and still stay readable on a narrow screen.",
    );
  }
  if (title.includes("#403")) return fail(403, "You do not have rights to edit this AI project");
  if (title.includes("#500")) return fail(500, "Internal server error");
  return undefined;
};

/* -------------------------------- Rendering ------------------------------- */

function userRef(u: MockUser) {
  return {
    id: u.id,
    displayName: u.displayName,
    email: u.email,
    avatar: "",
    avatarSmall: "",
    avatarMedium: "",
    avatarMax: "",
    avatarOriginal: "",
    hasAvatar: false,
    isAnonim: false,
    profileUrl: "",
    isOwner: u.isOwner,
    isAdmin: u.isAdmin,
    isRoomAdmin: u.isRoomAdmin,
    isCollaborator: u.isCollaborator,
    isVisitor: u.isVisitor,
    activationStatus: u.activationStatus,
    status: 1,
  };
}

function roomLogo(r: MockRoom) {
  return { original: "", large: "", medium: "", small: "", color: r.color, cover: null };
}

function indexed(files: MockFile[]) {
  return files.filter((f) => f.vectorizationStatus === 1).length;
}

function sourceDto(world: MockWorld, id: number, kind: typeof INSTRUCTIONS | typeof KNOWLEDGE) {
  const r = world.rooms.get(id);
  if (!r || !r.available) {
    return { id, title: null, roomType: kind, available: false, archived: false, logo: null };
  }
  const base = {
    id,
    title: r.title,
    roomType: r.roomType,
    available: true,
    archived: r.archived,
    logo: roomLogo(r),
  };
  if (kind === INSTRUCTIONS) {
    return { ...base, skillsCount: r.real ? null : r.skills.length };
  }
  const total = r.totalFiles ?? r.files.length;
  const ratio = r.files.length ? indexed(r.files) / r.files.length : 0;
  return {
    ...base,
    totalFiles: r.real ? null : total,
    indexedFiles: r.real ? null : Math.round(total * ratio),
  };
}

function profileDto(p: MockProject, profiles: Profile[]) {
  if (!p.profileId) {
    return null;
  }
  const real = profiles.find((x) => x.id === p.profileId);
  return real
    ? { id: real.id, name: real.name, provider: String(real.providerType ?? ""), available: true }
    : { id: p.profileId, name: null, provider: null, available: false };
}

function folderDto(world: MockWorld, profiles: Profile[], p: MockProject, warnings?: JsonObject[]) {
  const sec = security(p);
  const r = role(p);
  return {
    id: p.id,
    title: p.title,
    roomType: 12,
    type: 40,
    parentId: MOCK_PROJECTS_ROOT_ID,
    rootFolderId: MOCK_PROJECTS_ROOT_ID,
    rootFolderType: 41,
    created: p.created,
    updated: p.updated,
    createdBy: userRef(p.owner),
    updatedBy: userRef(p.owner),
    pinned: p.pinned,
    mute: p.mute,
    inRoom: r !== 0,
    access: r,
    filesCount: 0,
    foldersCount: 0,
    isFavorite: false,
    new: 0,
    logo: {
      original: p.hasLogo ? LOGO_SVG : "",
      large: p.hasLogo ? LOGO_SVG : "",
      medium: p.hasLogo ? LOGO_SVG : "",
      small: p.hasLogo ? LOGO_SVG : "",
      color: p.color,
      cover: p.cover ? { id: p.cover, data: BOOKMARK_COVER } : null,
    },
    security: sec,
    aiProject: {
      profile: profileDto(p, profiles),
      instructionsRooms: p.instructionsRoomIds.map((id) => sourceDto(world, id, INSTRUCTIONS)),
      knowledgeRooms: p.knowledgeRoomIds.map((id) => sourceDto(world, id, KNOWLEDGE)),
      canEditSettings: sec.EditRoom,
      ...(warnings && warnings.length ? { warnings } : {}),
    },
  };
}

function memberDto(m: MockMember, project: MockProject) {
  if (m.subject.kind === "group") {
    return {
      access: m.access,
      isOwner: false,
      isLocked: false,
      canEditAccess: security(project).EditAccess,
      subjectType: 2,
      sharedTo: {
        id: m.subject.group.id,
        name: m.subject.group.name,
        manager: null,
        isSystem: false,
        membersCount: m.subject.group.membersCount,
      },
    };
  }
  return {
    access: m.access,
    isOwner: false,
    isLocked: false,
    canEditAccess: security(project).EditAccess,
    subjectType: 0,
    sharedTo: userRef(m.subject.user),
  };
}

/** Owner first, then the caller (when a member), then the rest. */
function shareList(world: MockWorld, p: MockProject) {
  const list: JsonObject[] = [
    {
      access: 1,
      isOwner: true,
      isLocked: true,
      canEditAccess: false,
      subjectType: 0,
      sharedTo: userRef(p.owner),
    },
  ];
  if (p.me.kind === "member" && !p.me.viaGroup) {
    list.push(memberDto({ subject: { kind: "user", user: world.me }, access: p.me.access }, p));
  }
  const order = (m: MockMember) =>
    m.subject.kind === "group"
      ? 1
      : m.subject.user.activationStatus === 2
        ? 3
        : m.access === 9
          ? 0
          : 2;
  const rest = [...p.members].sort((a, b) => order(a) - order(b));
  for (const m of rest) {
    list.push(memberDto(m, p));
  }
  return list;
}

function linkDto(l: MockLink) {
  const expired = l.expirationDate !== null && Date.parse(l.expirationDate) < Date.now();
  return {
    access: 11,
    isOwner: false,
    isLocked: false,
    canEditAccess: true,
    subjectType: 4,
    sharedTo: {
      id: l.id,
      title: l.title,
      shareLink: `${proxyBaseUrl}/s/${l.requestToken}`,
      expirationDate: l.expirationDate,
      linkType: 0,
      isExpired: expired,
      primary: false,
      requestToken: l.requestToken,
      maxUseCount: l.maxUseCount,
      currentUseCount: l.currentUseCount,
      internal: false,
      denyDownload: false,
    },
  };
}

function eventDto(e: MockHistoryEvent | MockHistoryEvent["related"][number]) {
  const data = Object.fromEntries(
    Object.entries(e.data).map(([k, v]) =>
      isObject(v) && typeof v["displayName"] === "string"
        ? [k, userRef(v as unknown as MockUser)]
        : [k, v],
    ),
  );
  return {
    id: e.id,
    action: { id: e.actionId, key: e.key },
    initiator: userRef(e.initiator),
    date: e.date,
    data,
  };
}

/* ------------------------------ Validation ------------------------------- */

const nowIso = () => new Date().toISOString().replace("Z", "+00:00");

function touch(p: MockProject) {
  p.updated = nowIso();
}

function logEvent(
  world: MockWorld,
  p: MockProject,
  key: string,
  actionId: number,
  data: Record<string, unknown>,
) {
  p.history.unshift(
    historyEvent(key, actionId, world.me, nowIso(), { id: p.id, title: p.title, ...data }),
  );
}

function titleOf(raw: unknown, required: boolean): string | MockReply | undefined {
  if (raw === undefined && !required) {
    return undefined;
  }
  if (typeof raw !== "string" || raw.trim().length === 0) {
    return fail(400, "The project name is required");
  }
  const title = raw.trim();
  if (title.length > MAX_TITLE) {
    return fail(400, `The project name must be at most ${MAX_TITLE} characters`);
  }
  return titleTrigger(title) ?? title;
}

/** A real room of the portal picked in the dialog, read with the caller's rights. */
async function adoptRealRoom(world: MockWorld, id: number): Promise<MockRoom | undefined> {
  const info = await getFolderInfo(String(id)).catch(() => undefined);
  if (!info || (info.roomType !== INSTRUCTIONS && info.roomType !== KNOWLEDGE)) {
    return undefined;
  }
  const r: MockRoom = {
    id,
    title: info.title ?? `Room ${id}`,
    roomType: info.roomType,
    color: "6191F2",
    available: true,
    archived: false,
    real: true,
    skills: [],
    files: [],
  };
  world.rooms.set(id, r);
  return r;
}

/** Checks one list of linked rooms (the V6–V13 rules of the contract). */
async function roomList(
  world: MockWorld,
  raw: unknown,
  kind: typeof INSTRUCTIONS | typeof KNOWLEDGE,
  label: string,
): Promise<number[] | MockReply | undefined> {
  if (raw === undefined || raw === null) {
    return undefined;
  }
  if (!Array.isArray(raw)) {
    return fail(400, `${label} must be an array of room ids`);
  }
  const ids: number[] = [];
  for (const v of raw) {
    if (typeof v !== "number" || !Number.isInteger(v) || v <= 0) {
      return fail(400, "Room ids must be positive integers");
    }
    if (!ids.includes(v)) ids.push(v);
  }
  if (ids.length > MAX_ROOMS_PER_KIND) {
    return fail(
      400,
      `A project can link at most ${MAX_ROOMS_PER_KIND} ${kind === INSTRUCTIONS ? "Instructions" : "Knowledge"} rooms`,
    );
  }
  for (const id of ids) {
    const r = world.rooms.get(id) ?? (await adoptRealRoom(world, id));
    if (!r || !r.available) {
      return fail(400, `Room ${id} does not exist or is not accessible`);
    }
    if (r.roomType !== kind) {
      return fail(
        400,
        `Room ${id} is not ${kind === INSTRUCTIONS ? "an Instructions" : "a Knowledge"} room`,
      );
    }
  }
  return ids;
}

interface SettingsPatch {
  profileIdSet: boolean;
  profileId: string | null;
  instructions?: number[];
  knowledge?: number[];
}

async function settingsPatch(world: MockWorld, raw: unknown): Promise<SettingsPatch | MockReply> {
  const s = isObject(raw) ? raw : {};
  const instructions = await roomList(
    world,
    s["instructionsRoomIds"],
    INSTRUCTIONS,
    "instructionsRoomIds",
  );
  if (instructions && !Array.isArray(instructions)) return instructions;
  const knowledge = await roomList(world, s["knowledgeRoomIds"], KNOWLEDGE, "knowledgeRoomIds");
  if (knowledge && !Array.isArray(knowledge)) return knowledge;
  if (instructions && knowledge && instructions.some((id) => knowledge.includes(id))) {
    return fail(400, "A room cannot be linked as both Instructions and Knowledge");
  }
  const profileIdSet = "profileId" in s;
  const profileId = typeof s["profileId"] === "string" ? s["profileId"] : null;
  return { profileIdSet, profileId, instructions, knowledge };
}

function grantWarnings(world: MockWorld, ids: number[]): JsonObject[] {
  return ids
    .map((id) => world.rooms.get(id))
    .filter((r): r is MockRoom => !!r?.grantFails)
    .map((r) => ({
      code: "grantSkipped",
      roomId: r.id,
      subjectId: null,
      message: `Viewer access to the room "${r.title}" could not be granted to every project member`,
    }));
}

const ROOM_FIELDS = new Set(["title", "color", "cover", "logo", "aiProject"]);

function unknownField(body: JsonObject): MockReply | undefined {
  const extra = Object.keys(body).find((k) => !ROOM_FIELDS.has(k));
  return extra
    ? fail(400, `The JSON property "${extra}" could not be mapped to any .NET member`)
    : undefined;
}

/* -------------------------------- Routes --------------------------------- */

export async function listProjects(query: Record<string, string>): Promise<MockReply> {
  const { world, profiles } = await context();
  const text = (query["filterValue"] ?? "").trim().toLowerCase();
  const roleFilter = Number(query["role"] ?? 0);
  const linked = query["linkedRoomId"] ? Number(query["linkedRoomId"]) : undefined;
  const instructionsRoom = query["instructionsRoomId"] ? Number(query["instructionsRoomId"]) : undefined;
  const knowledgeRoom = query["knowledgeRoomId"] ? Number(query["knowledgeRoomId"]) : undefined;
  let list = [...world.projects.values()].filter((p) => visible(world, p));
  if (text) list = list.filter((p) => p.title.toLowerCase().includes(text));
  if (roleFilter) list = list.filter((p) => role(p) === roleFilter);
  if (query["hasInstructions"] === "true")
    list = list.filter((p) => p.instructionsRoomIds.length > 0);
  if (query["hasKnowledge"] === "true") list = list.filter((p) => p.knowledgeRoomIds.length > 0);
  if (linked)
    list = list.filter(
      (p) => p.instructionsRoomIds.includes(linked) || p.knowledgeRoomIds.includes(linked),
    );
  if (instructionsRoom) list = list.filter((p) => p.instructionsRoomIds.includes(instructionsRoom));
  if (knowledgeRoom) list = list.filter((p) => p.knowledgeRoomIds.includes(knowledgeRoom));
  if (query["subjectId"]) list = list.filter((p) => p.owner.id === query["subjectId"]);
  const sortBy = query["sortBy"] ?? "DateAndTime";
  if (!["DateAndTime", "AZ", "DateAndTimeCreation"].includes(sortBy)) {
    return fail(400, "sortBy must be DateAndTime, AZ or DateAndTimeCreation");
  }
  const dir = query["sortOrder"] === "ascending" ? 1 : -1;
  list.sort((a, b) => {
    if (a.pinned !== b.pinned) return a.pinned ? -1 : 1;
    const cmp =
      sortBy === "AZ"
        ? a.title.localeCompare(b.title)
        : sortBy === "DateAndTimeCreation"
          ? a.created.localeCompare(b.created)
          : a.updated.localeCompare(b.updated);
    return cmp * dir;
  });
  const startIndex = Math.max(0, Number(query["startIndex"] ?? 0) || 0);
  const count = Math.min(
    1000,
    Math.max(1, Number(query["count"] ?? DEFAULT_COUNT) || DEFAULT_COUNT),
  );
  const page = list.slice(startIndex, startIndex + count);
  return ok({
    files: [],
    folders: page.map((p) => folderDto(world, profiles, p)),
    current: {
      id: MOCK_PROJECTS_ROOT_ID,
      title: "AI Projects",
      rootFolderType: 41,
      parentId: 0,
      filesCount: 0,
      foldersCount: list.length,
    },
    pathParts: [{ id: MOCK_PROJECTS_ROOT_ID, title: "AI Projects" }],
    startIndex,
    count: page.length,
    total: list.length,
    new: 0,
  });
}

export async function getProject(id: string): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  return isReply(p) ? p : ok(folderDto(world, profiles, p));
}

export async function createProject(body: JsonObject): Promise<MockReply> {
  const { world, profiles } = await context();
  if (!canCreate(world.me)) {
    return fail(403, "Only administrators and room administrators can create AI projects");
  }
  const bad = unknownField(body);
  if (bad) return bad;
  const title = titleOf(body["title"], true);
  if (typeof title !== "string") return title as MockReply;
  const patch = await settingsPatch(world, body["aiProject"]);
  if ("status" in patch) return patch;
  const p: MockProject = {
    id: newProjectId(),
    title,
    color: typeof body["color"] === "string" && body["color"] ? body["color"] : "6191F2",
    cover: typeof body["cover"] === "string" && body["cover"] ? body["cover"] : null,
    hasLogo: isObject(body["logo"]),
    owner: world.me,
    me: { kind: "owner" },
    created: nowIso(),
    updated: nowIso(),
    pinned: false,
    mute: false,
    profileId: patch.profileId,
    instructionsRoomIds: patch.instructions ?? [],
    knowledgeRoomIds: patch.knowledge ?? [],
    members: [],
    link: null,
    history: [],
  };
  world.projects.set(p.id, p);
  registerMockProject(p.id, p.title);
  logEvent(world, p, "RoomCreated", 5001, {});
  return ok(
    folderDto(
      world,
      profiles,
      p,
      grantWarnings(world, [...p.instructionsRoomIds, ...p.knowledgeRoomIds]),
    ),
  );
}

export async function updateProject(id: string, body: JsonObject): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).EditRoom) return fail(403, "You do not have rights to edit this AI project");
  const bad = unknownField(body);
  if (bad) return bad;
  const title = titleOf(body["title"], false);
  if (title !== undefined && typeof title !== "string") return title;
  const settings = isObject(body["aiProject"]) ? body["aiProject"] : undefined;
  if (
    title === undefined &&
    body["color"] === undefined &&
    body["cover"] === undefined &&
    body["logo"] === undefined &&
    !settings
  ) {
    return fail(400, "Nothing to update");
  }
  const triggered = titleTrigger(p.title);
  if (triggered) return triggered;
  const patch = settings ? await settingsPatch(world, settings) : undefined;
  if (patch && "status" in patch) return patch;

  if (title !== undefined && title !== p.title) {
    logEvent(world, p, "RoomRenamed", 5003, { oldTitle: p.title, newTitle: title });
    p.title = title;
    registerMockProject(p.id, p.title);
  }
  if (typeof body["color"] === "string" && body["color"]) {
    p.color = body["color"];
    logEvent(world, p, "RoomColorChanged", 5100, {});
  }
  if (typeof body["cover"] === "string") {
    p.cover = body["cover"] || null;
    logEvent(world, p, "RoomCoverChanged", 5101, {});
  }
  if (isObject(body["logo"])) {
    p.hasLogo = true;
    logEvent(world, p, "RoomLogoCreated", 5102, {});
  }
  let added: number[] = [];
  if (patch) {
    if (patch.profileIdSet && patch.profileId !== p.profileId) {
      p.profileId = patch.profileId;
      logEvent(world, p, "AiProjectModelChanged", 5167, {});
    }
    if (patch.instructions) {
      added = added.concat(patch.instructions.filter((x) => !p.instructionsRoomIds.includes(x)));
      p.instructionsRoomIds = patch.instructions;
    }
    if (patch.knowledge) {
      added = added.concat(patch.knowledge.filter((x) => !p.knowledgeRoomIds.includes(x)));
      p.knowledgeRoomIds = patch.knowledge;
    }
  }
  touch(p);
  return ok(folderDto(world, profiles, p, grantWarnings(world, added)));
}

/** A file operation the client stops polling at once: it is already finished. */
function finishedOperation(type: 2 | 7) {
  return {
    id: guid(Math.floor(Math.random() * 1e9), 4),
    Operation: type,
    progress: 100,
    error: "",
    processed: "1",
    finished: true,
  };
}

export async function deleteProject(id: string): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).Delete)
    return fail(403, "Only the project owner or a DocSpace admin can delete the AI project");
  world.projects.delete(p.id);
  unregisterMockProject(p.id);
  return ok(finishedOperation(2));
}

export async function duplicateProject(id: string): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).Duplicate || !canCreate(world.me))
    return fail(403, "You cannot duplicate this AI project");
  const copy: MockProject = {
    ...p,
    id: newProjectId(),
    title: `${p.title} (1)`.slice(0, MAX_TITLE),
    owner: world.me,
    me: { kind: "owner" },
    created: nowIso(),
    updated: nowIso(),
    pinned: false,
    mute: false,
    members: [],
    link: null,
    history: [],
    instructionsRoomIds: p.instructionsRoomIds.filter((x) => world.rooms.get(x)?.available),
    knowledgeRoomIds: p.knowledgeRoomIds.filter((x) => world.rooms.get(x)?.available),
    failWith: undefined,
  };
  world.projects.set(copy.id, copy);
  registerMockProject(copy.id, copy.title);
  logEvent(world, copy, "RoomCreated", 5001, {});
  return ok(finishedOperation(7));
}

export async function deleteLogo(id: string): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (p.hasLogo && !security(p).EditRoom)
    return fail(403, "You do not have rights to edit this AI project");
  p.hasLogo = false;
  touch(p);
  return ok(folderDto(world, profiles, p));
}

export async function setPinned(id: string, pinned: boolean): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (pinned && !p.pinned) {
    const count = [...world.projects.values()].filter((x) => x.pinned && visible(world, x)).length;
    if (count >= MAX_PINNED) return fail(403, `You can pin at most ${MAX_PINNED} AI projects`);
  }
  p.pinned = pinned;
  return ok(folderDto(world, profiles, p));
}

export async function setMute(id: string, body: JsonObject): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (typeof body["mute"] !== "boolean") return fail(400, "mute must be true or false");
  if (!security(p).Mute) return fail(403, "You are not a member of this AI project");
  p.mute = body["mute"];
  return ok(folderDto(world, profiles, p));
}

export async function getHistory(id: string, query: Record<string, string>): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  const from = query["fromDate"] ? Date.parse(query["fromDate"]) : -Infinity;
  const to = query["toDate"] ? Date.parse(query["toDate"]) : Infinity;
  const all = p.history.filter((e) => {
    const t = Date.parse(e.date);
    return t >= from && t <= to;
  });
  const startIndex = Math.max(0, Number(query["startIndex"] ?? 0) || 0);
  const count = Math.max(1, Number(query["count"] ?? DEFAULT_COUNT) || DEFAULT_COUNT);
  const page = all.slice(startIndex, startIndex + count).map((e) => ({
    ...eventDto(e),
    related: e.related.map(eventDto),
  }));
  return ok(page, { count: page.length, total: all.length });
}

export async function getMembers(id: string, query: Record<string, string>): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  const filterType = Number(query["filterType"] ?? 0);
  if (filterType !== 0 && filterType !== 1) return fail(400, "filterType must be 0 or 1");
  if (filterType === 1) {
    const links = p.link && security(p).EditAccess ? [linkDto(p.link)] : [];
    return ok(links, { count: links.length, total: links.length });
  }
  const text = (query["filterValue"] ?? "").trim().toLowerCase();
  let list = shareList(world, p);
  if (text) {
    list = list.filter((m) => {
      const to = getObject(m, "sharedTo");
      const name = (to && (getString(to, "displayName") ?? getString(to, "name"))) ?? "";
      const email = (to && getString(to, "email")) ?? "";
      return name.toLowerCase().includes(text) || email.toLowerCase().includes(text);
    });
  }
  const startIndex = Math.max(0, Number(query["startIndex"] ?? 0) || 0);
  const count = Math.max(1, Number(query["count"] ?? DEFAULT_COUNT) || DEFAULT_COUNT);
  const page = list.slice(startIndex, startIndex + count);
  return ok(page, { count: page.length, total: list.length });
}

export async function setMembers(id: string, body: JsonObject): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).EditAccess)
    return fail(403, "You do not have rights to manage the members of this AI project");
  const allowed = new Set(["invitations", "notify", "message", "culture", "force"]);
  const extra = Object.keys(body).find((k) => !allowed.has(k));
  if (extra)
    return fail(400, `The JSON property "${extra}" could not be mapped to any .NET member`);
  const invitations = getArray(body, "invitations") ?? [];
  if (invitations.length === 0) return fail(400, "At least one invitation is required");
  for (const inv of invitations) {
    if (!isObject(inv)) return fail(400, "An invitation must be an object");
    const hasId = typeof inv["id"] === "string";
    const hasEmail = typeof inv["email"] === "string";
    if (hasId === hasEmail) return fail(400, "An invitation needs either an id or an e-mail");
    const access = getNumber(inv, "access");
    if (access !== 0 && access !== 9 && access !== 11)
      return fail(400, "The role must be Project manager or Member");
    if (hasEmail && access === 9) return fail(400, "An e-mail invitation can only make a Member");
    if (inv["id"] === p.owner.id)
      return fail(400, "The project owner is not in the member list; use Change owner");
  }
  const changed: MockMember[] = [];
  for (const inv of invitations as JsonObject[]) {
    const access = getNumber(inv, "access") as 0 | 9 | 11;
    const email = getString(inv, "email");
    const subjectId = getString(inv, "id");
    if (subjectId === world.me.id) {
      if (access === 0) p.me = { kind: "outside" };
      else p.me = { kind: "member", access };
      continue;
    }
    const index = p.members.findIndex((m) =>
      m.subject.kind === "user"
        ? m.subject.user.id === subjectId || (!!email && m.subject.user.email === email)
        : m.subject.group.id === subjectId,
    );
    if (access === 0) {
      if (index >= 0) {
        const [removed] = p.members.splice(index, 1);
        if (removed?.subject.kind === "user")
          logEvent(world, p, "RoomRemoveUser", 5072, { user: removed.subject.user, access: 11 });
      }
      continue;
    }
    if (index >= 0) {
      const m = p.members[index] as MockMember;
      if (
        m.subject.kind === "user" &&
        access === 9 &&
        (m.subject.user.isVisitor || m.subject.user.isCollaborator)
      ) {
        return fail(403, "This role is not available for this user");
      }
      m.access = access;
      changed.push(m);
      if (m.subject.kind === "user")
        logEvent(world, p, "RoomUpdateAccessForUser", 5071, {
          user: m.subject.user,
          access,
          oldAccess: 11,
        });
      continue;
    }
    const group = world.groups.find((g) => g.id === subjectId);
    const known = world.users.find((u) => u.id === subjectId);
    const member: MockMember = group
      ? { subject: { kind: "group", group }, access: 11 }
      : {
          subject: {
            kind: "user",
            user:
              known ??
              ({
                id: subjectId ?? guid(Math.floor(Math.random() * 1e9), 5),
                displayName: email ?? `User ${subjectId?.slice(0, 8)}`,
                email: email ?? "",
                isOwner: false,
                isAdmin: false,
                isRoomAdmin: access === 9,
                isCollaborator: false,
                isVisitor: !!email,
                activationStatus: email ? 2 : 1,
              } satisfies MockUser),
          },
          access,
        };
    if (
      member.subject.kind === "user" &&
      access === 9 &&
      (member.subject.user.isVisitor || member.subject.user.isCollaborator)
    ) {
      return fail(403, "This role is not available for this user");
    }
    p.members.push(member);
    changed.push(member);
    if (member.subject.kind === "user")
      logEvent(world, p, "RoomCreateUser", 5070, { user: member.subject.user, access });
    else
      logEvent(world, p, "RoomGroupAdded", 5073, {
        group: { id: member.subject.group.id, name: member.subject.group.name },
        access,
      });
  }
  touch(p);
  const warnings = grantWarnings(world, [...p.instructionsRoomIds, ...p.knowledgeRoomIds]).map(
    (w) => ({ ...w, code: "grantSkipped" }),
  );
  return ok({
    members: changed.map((m) => memberDto(m, p)),
    warning: null,
    error: 0,
    ...(changed.length && warnings.length ? { aiProjectWarnings: warnings } : {}),
  });
}

export async function resend(id: string, body: JsonObject): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).EditRoom)
    return fail(403, "You do not have rights to manage the members of this AI project");
  const ids = getArray(body, "usersIds");
  if (ids && ids.length > 1000) return fail(400, "At most 1000 users at a time");
  logEvent(world, p, "RoomInviteResend", 5074, {});
  return ok(null);
}

export async function leave(id: string): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (p.me.kind === "owner")
    return fail(400, "The project owner cannot leave the project. Change the owner first.");
  if (p.me.kind === "outside") return fail(400, "You are not a member of this project");
  if (p.me.viaGroup)
    return fail(400, "You are a member through a group; ask a Project manager to remove the group");
  p.me = { kind: "outside" };
  logEvent(world, p, "RoomRemoveUser", 5072, { user: world.me, access: 11 });
  return ok(null);
}

export async function changeOwner(id: string, body: JsonObject): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).ChangeOwner)
    return fail(403, "Only the project owner or a DocSpace admin can change the owner");
  const userId = getString(body, "userId");
  if (!userId || userId === "00000000-0000-0000-0000-000000000000")
    return fail(400, "userId is required");
  if (userId === p.owner.id) return fail(400, "This user already owns the project");
  const keep = body["keepAsManager"] !== false;
  const target =
    world.users.find((u) => u.id === userId) ??
    ({
      id: userId,
      displayName: `User ${userId.slice(0, 8)}`,
      email: "",
      isOwner: false,
      isAdmin: false,
      isRoomAdmin: true,
      isCollaborator: false,
      isVisitor: false,
      activationStatus: 1,
    } satisfies MockUser);
  if (target.isVisitor || target.isCollaborator) {
    return fail(403, "The new owner must be a DocSpace admin or a room admin");
  }
  const oldOwner = p.owner;
  const wasMe: MyStanding = p.me;
  p.members = p.members.filter(
    (m) => !(m.subject.kind === "user" && m.subject.user.id === target.id),
  );
  p.owner = target;
  if (target.id === world.me.id) {
    p.me = { kind: "owner" };
  } else if (wasMe.kind === "owner") {
    p.me = keep ? { kind: "member", access: 9 } : { kind: "outside" };
  } else if (keep) {
    p.members.unshift({ subject: { kind: "user", user: oldOwner }, access: 9 });
  }
  logEvent(world, p, "RoomChangeOwner", 5075, { owner: target, oldOwner });
  touch(p);
  return ok(folderDto(world, profiles, p));
}

export async function getLinks(id: string): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  const links = p.link && security(p).EditAccess ? [linkDto(p.link)] : [];
  return ok(links, { count: links.length });
}

export async function setLink(id: string, body: JsonObject): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (!security(p).EditAccess)
    return fail(403, "You do not have rights to manage the invitation link of this AI project");
  const access = getNumber(body, "access");
  if (access !== 0 && access !== 11) return fail(400, "The invitation link can only make Members");
  const linkId = getString(body, "linkId");
  if (access === 0) {
    if (!linkId) return fail(400, "linkId is required to remove the link");
    p.link = null;
    return ok(null);
  }
  if (!linkId && p.link) return fail(403, "The project already has an invitation link");
  const maxUseCount = body["maxUseCount"];
  if (
    maxUseCount !== undefined &&
    maxUseCount !== null &&
    (typeof maxUseCount !== "number" || maxUseCount < 1 || maxUseCount > 1000)
  ) {
    return fail(400, "maxUseCount must be between 1 and 1000");
  }
  const expirationDate = typeof body["expirationDate"] === "string" ? body["expirationDate"] : null;
  if (expirationDate && Date.parse(expirationDate) < Date.now() - 60_000) {
    return fail(400, "The expiration date is in the past");
  }
  const current = p.link;
  p.link = {
    id: current?.id ?? guid(Math.floor(Math.random() * 1e9), 3),
    title: getString(body, "title") || current?.title || "Invite",
    expirationDate,
    maxUseCount: typeof maxUseCount === "number" ? maxUseCount : null,
    currentUseCount: current?.currentUseCount ?? 0,
    requestToken: current?.requestToken ?? `mock-token-${p.id}-${Date.now()}`,
  };
  return ok(linkDto(p.link));
}

/** The skills of one linked room: generated for a mock room, read live for a real one. */
async function roomSkills(r: MockRoom) {
  if (!r.real) {
    return r.skills;
  }
  try {
    return (await getRoomSkills(String(r.id))).map((s) => ({
      id: String(s.id),
      name: s.name,
      description: s.description ?? "",
    }));
  } catch {
    return [];
  }
}

export async function getSkills(id: string): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (p.slowMs) await sleep(p.slowMs);
  const rooms = [];
  for (const roomId of p.instructionsRoomIds) {
    const r = world.rooms.get(roomId);
    if (!r || !r.available) {
      rooms.push({ id: roomId, title: null, logo: null, available: false, skills: [] });
      continue;
    }
    rooms.push({
      id: r.id,
      title: r.title,
      logo: roomLogo(r),
      available: true,
      skills: await roomSkills(r),
    });
  }
  return ok({ rooms });
}

/** The files of one real Knowledge room, read through the Files API (root level, first page). */
async function realFiles(r: MockRoom): Promise<MockFile[]> {
  const url = `${proxyBaseUrl}/api/2.0/files/${r.id}?count=1000&filterType=0`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) return [];
    const json: unknown = await res.json();
    const content = isObject(json) ? getObject(json, "response") : undefined;
    const list = content ? (getArray(content, "files") ?? []) : [];
    return list.filter(isObject).map((f) => {
      const status = getNumber(f, "vectorizationStatus");
      const security = getObject(f, "security");
      return {
        id: getNumber(f, "id") ?? 0,
        title: getString(f, "title") ?? "",
        fileExst: getString(f, "fileExst") ?? "",
        vectorizationStatus: status === 0 || status === 1 || status === 2 ? status : null,
        folderId: r.id,
        // The caller's right to queue the file again, as the Files API reports it.
        canRetry: security ? (getBoolean(security, "Vectorization") ?? false) : false,
      };
    });
  } catch {
    return [];
  } finally {
    cancel();
  }
}

export async function getKnowledge(id: string, query: Record<string, string>): Promise<MockReply> {
  const { world } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  if (p.slowMs) await sleep(p.slowMs);
  const only = query["roomId"] ? Number(query["roomId"]) : undefined;
  if (only !== undefined && !p.knowledgeRoomIds.includes(only)) {
    return fail(400, `Room ${only} is not a Knowledge room linked to this project`);
  }
  const rooms = [];
  for (const roomId of p.knowledgeRoomIds) {
    if (only !== undefined && roomId !== only) continue;
    const r = world.rooms.get(roomId);
    if (!r || !r.available) {
      rooms.push({
        id: roomId,
        title: null,
        logo: null,
        available: false,
        totalFiles: 0,
        indexedFiles: 0,
        truncated: false,
        files: [],
      });
      continue;
    }
    const list = r.real ? await realFiles(r) : r.files;
    const total = r.totalFiles ?? list.length;
    const ratio = list.length ? indexed(list) / list.length : 0;
    // In a mock room the project's managers may retry a failed file, as a
    // Content creator of a real room may; a real room's files carry the right.
    const manager = role(p) === 1 || role(p) === 9;
    rooms.push({
      id: r.id,
      title: r.title,
      logo: roomLogo(r),
      available: true,
      totalFiles: total,
      indexedFiles: r.totalFiles ? Math.round(total * ratio) : indexed(list),
      failedFiles: list.filter((f) => f.vectorizationStatus === 2).length,
      truncated: total > list.length,
      files: list.map(({ canRetry, ...f }) => ({
        ...f,
        indexed: f.vectorizationStatus === 1,
        security: { Vectorization: f.vectorizationStatus === 2 && (canRetry ?? manager) },
      })),
    });
  }
  return ok({ rooms });
}

/* ------------------------------ Chat binding ------------------------------ */

/** Method 21 for a mock project: only real linked rooms can serve a chat round. */
export async function chatContext(id: string): Promise<MockReply> {
  const { world, profiles } = await context();
  const p = find(world, id);
  if (isReply(p)) return p;
  const usable = (ids: number[]) =>
    ids.filter((x) => world.rooms.get(x)?.real && world.rooms.get(x)?.available);
  const unusable = [...p.instructionsRoomIds, ...p.knowledgeRoomIds].filter(
    (x) => !world.rooms.get(x)?.real || !world.rooms.get(x)?.available,
  );
  // The portal default, resolved as a global round resolves it.
  let effective: string | null = p.profileId;
  if (!effective) {
    const resolved = await assignments
      .tryResolveForAction(ActionType.Chat, undefined)
      .catch(() => undefined);
    effective = resolved?.profile?.id ?? null;
  }
  const available =
    effective !== null &&
    effective !== DELETED_PROFILE_ID &&
    profiles.some((x) => x.id === effective);
  return ok({
    projectId: p.id,
    title: p.title,
    myRole: role(p),
    canChat: role(p) !== 0,
    profileId: p.profileId,
    effectiveProfileId: effective,
    profileAvailable: available,
    instructionsRoomIds: usable(p.instructionsRoomIds),
    knowledgeRoomIds: usable(p.knowledgeRoomIds),
    unavailableRoomIds: unusable,
    version: Date.parse(p.updated) * 10_000,
  });
}

/** Method 19 for the chat round of a mock project (the round reads only real rooms). */
export async function chatSkills(id: string): Promise<MockReply> {
  return getSkills(id);
}
