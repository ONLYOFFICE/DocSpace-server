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
// including the rights table: a Member cannot edit, only the owner or a
// DocSpace admin can delete, and so on. Only the project-specific routes are
// mocked: pin, mute, share, invitation links, leave, owner change, duplicate,
// logo removal and history go through the Files API (`files/rooms/{id}/...`),
// which does not know the mock's ids, so they do not work on mock projects.

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
  INSTRUCTIONS,
  KNOWLEDGE,
  newProjectId,
  SCENARIOS,
} from "./data.js";
import type {
  MockFile,
  MockProject,
  MockRoom,
  MockUser,
  MockWorld,
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

/* ------------------------------ Validation ------------------------------- */

const nowIso = () => new Date().toISOString().replace("Z", "+00:00");

function touch(p: MockProject) {
  p.updated = nowIso();
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
  };
  world.projects.set(p.id, p);
  registerMockProject(p.id, p.title);
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
    p.title = title;
    registerMockProject(p.id, p.title);
  }
  if (typeof body["color"] === "string" && body["color"]) {
    p.color = body["color"];
  }
  if (typeof body["cover"] === "string") {
    p.cover = body["cover"] || null;
  }
  if (isObject(body["logo"])) {
    p.hasLogo = true;
  }
  let added: number[] = [];
  if (patch) {
    if (patch.profileIdSet && patch.profileId !== p.profileId) {
      p.profileId = patch.profileId;
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

/** A delete file operation the client stops polling at once: it is already finished. */
function finishedOperation() {
  return {
    id: guid(Math.floor(Math.random() * 1e9), 4),
    Operation: 2,
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
  return ok(finishedOperation());
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
