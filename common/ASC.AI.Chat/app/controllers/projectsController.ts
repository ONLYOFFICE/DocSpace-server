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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { ActionType, CapabilitiesUI } from "@onlyoffice/ai-chat/core";

import type { Response } from "express";
import logger from "../log.js";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { isObject, getString, getBoolean, getArray, getObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import {
  Access,
  CURRENT_USER_ID,
  INSTRUCTIONS_ROOM_TYPE,
  KNOWLEDGE_ROOM_TYPE,
  PROJECT_FOLDER_TYPE,
  PROJECT_ROOM_TYPE,
  PROJECTS_ROOT_FOLDER_TYPE,
  PROJECTS_ROOT_ID,
  allowedInviteAccess,
  canHoldManagerRole,
  canReadRoom,
  createPendingGuest,
  createProject,
  deleteProject,
  ensureRoom,
  ensureUser,
  findProfile,
  findProject,
  findRoom,
  findUser,
  findUserByEmail,
  grantViewerToMembers,
  isPortalAdmin,
  listProfiles,
  listProjects,
  listRooms,
  listUsers,
  roomLogo,
  touch,
} from "../storage/aiProjectsMockStorage.js";
import type {
  AccessValue,
  MockProject,
  MockRoom,
  MockUser,
} from "../storage/aiProjectsMockStorage.js";

/**
 * AI Projects — mock implementation.
 *
 * Every handler answers in the DocSpace envelope (`{ response, status,
 * statusCode }`) with the room DTO shapes the Files API uses, so the client
 * written against this facade keeps working when the routes start
 * delegating to `files/rooms` (room type 12) the way `agentsController`
 * delegates agents. State lives in `aiProjectsMockStorage`; the caller is
 * always `CURRENT_USER_ID` there.
 */

const UUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

const DEFAULT_PAGE_SIZE = 25;
const MAX_PAGE_SIZE = 100;
const MAX_TITLE_LENGTH = 170;

function httpError(status: number, message: string): never {
  throw Object.assign(new Error(message), { status, expose: true });
}

function badRequest(message: string): never {
  httpError(400, message);
}

function envelope(res: Response, response: unknown, extra: JsonObject = {}): void {
  res.json({ response, ...extra, status: 0, statusCode: 200 });
}

function caller(): MockUser {
  const user = findUser(CURRENT_USER_ID);
  if (!user) {
    throw new Error("mock caller is missing from the seed");
  }
  return user;
}

function projectIdParam(value: string | undefined): number {
  if (!value || !/^\d+$/.test(value)) {
    badRequest("project id must be a positive integer");
  }
  return Number(value);
}

function accessOf(user: MockUser, project: MockProject): AccessValue {
  return project.members.get(user.id) ?? Access.None;
}

/** Admins see every project; everyone else the projects they are in. */
function canSee(user: MockUser, project: MockProject): boolean {
  return isPortalAdmin(user) || project.members.has(user.id);
}

function canEditSettings(user: MockUser, project: MockProject): boolean {
  const access = accessOf(user, project);
  return access === Access.Owner || access === Access.ProjectManager;
}

function requireProject(user: MockUser, id: number): MockProject {
  const project = findProject(id);
  if (!project || !canSee(user, project)) {
    httpError(404, `project ${id} does not exist or is not accessible`);
  }
  return project;
}

function requireSettingsAccess(user: MockUser, project: MockProject): void {
  if (!canEditSettings(user, project)) {
    httpError(403, "only the project owner or a project manager can do this");
  }
}

function requireOwner(user: MockUser, project: MockProject): void {
  if (accessOf(user, project) !== Access.Owner) {
    httpError(403, "only the project owner can do this");
  }
}

/* ------------------------------- DTOs ---------------------------------- */

function userDto(user: MockUser): JsonObject {
  return {
    id: user.id,
    displayName: user.displayName,
    email: user.email,
    avatarSmall: "",
    hasAvatar: false,
    isOwner: user.isOwner,
    isAdmin: user.employeeType === 3,
    isRoomAdmin: user.employeeType === 1,
    isCollaborator: user.employeeType === 4,
    isVisitor: user.employeeType === 2,
    status: user.status,
    avatarColor: user.avatarColor,
  };
}

/**
 * The logo object of a room DTO. As for a project, the cover's SVG is not the
 * mock's to give: `data` stays empty and the client fills it from
 * `GET files/rooms/covers`.
 */
function logoDto(color: string, cover: string | null): JsonObject {
  return {
    original: "",
    large: "",
    medium: "",
    small: "",
    color,
    cover: cover ? { id: cover, data: "" } : undefined,
  };
}

function roomLogoDto(room: MockRoom): JsonObject {
  const { color, cover } = roomLogo(room);
  return logoDto(color, cover);
}

function sourceDto(room: MockRoom): JsonObject {
  const base: JsonObject = { id: room.id, title: room.title, roomType: room.roomType, logo: roomLogoDto(room) };
  if (room.roomType === INSTRUCTIONS_ROOM_TYPE) {
    base["skillsCount"] = room.skills.length;
  } else {
    base["totalFiles"] = room.files.length;
    base["indexedFiles"] = room.files.filter((f) => f.indexed).length;
  }
  return base;
}

function profileDto(profileId: string | null): JsonObject | null {
  if (profileId === null) {
    return null;
  }
  const profile = findProfile(profileId);
  if (profile) {
    return {
      id: profile.id,
      name: profile.name,
      provider: profile.provider,
      available: profile.enabled,
    };
  }
  const real = resolvedProfiles.get(profileId);
  return real
    ? { id: profileId, name: real.name, provider: real.providerType, available: true }
    : { id: profileId, name: null, provider: null, available: false };
}

/** Real portal profiles a project was bound to, by id, read once per process. */
const resolvedProfiles = new Map<string, { name: string; providerType: string }>();

/** Reads the real profiles the given projects use, so `profileDto` can name them. */
async function resolveProfiles(projects: MockProject[]): Promise<void> {
  const ids = new Set(
    projects
      .map((p) => p.profileId)
      .filter((id): id is string => id !== null && !findProfile(id) && !resolvedProfiles.has(id)),
  );
  await Promise.all(
    [...ids].map(async (id) => {
      const profile = await storage.profiles.readById(id).catch(() => null);
      if (profile) {
        resolvedProfiles.set(id, { name: profile.name, providerType: profile.providerType });
      }
    }),
  );
}

function roomsOf(ids: number[]): MockRoom[] {
  return ids.map((id) => findRoom(id)).filter((r): r is MockRoom => r !== undefined);
}

function securityDto(user: MockUser, project: MockProject): JsonObject {
  const access = accessOf(user, project);
  const owner = access === Access.Owner;
  const manager = owner || access === Access.ProjectManager;
  return {
    Read: true,
    Rename: manager,
    EditRoom: manager,
    EditAccess: manager,
    ChangeOwner: owner,
    Delete: owner,
    Pin: project.members.has(user.id),
    Leave: project.members.has(user.id) && !owner,
    // A project holds no files: nothing can be created, moved or copied into it.
    Create: false,
    CopyTo: false,
    MoveTo: false,
    Download: false,
  };
}

function projectDto(user: MockUser, project: MockProject): JsonObject {
  const creator = findUser(project.createdBy);
  return {
    id: project.id,
    parentId: PROJECTS_ROOT_ID,
    rootFolderId: PROJECTS_ROOT_ID,
    rootFolderType: PROJECTS_ROOT_FOLDER_TYPE,
    type: PROJECT_FOLDER_TYPE,
    roomType: PROJECT_ROOM_TYPE,
    title: project.title,
    created: project.created,
    updated: project.updated,
    createdBy: creator ? userDto(creator) : null,
    filesCount: 0,
    foldersCount: 0,
    new: 0,
    pinned: project.pinnedBy.has(user.id),
    inRoom: project.members.has(user.id),
    access: accessOf(user, project),
    logo: logoDto(project.logoColor, project.logoCover),
    tags: [],
    security: securityDto(user, project),
    aiProject: {
      profile: profileDto(project.profileId),
      instructionsRooms: roomsOf(project.instructionsRoomIds).map(sourceDto),
      knowledgeRooms: roomsOf(project.knowledgeRoomIds).map(sourceDto),
      canEditSettings: canEditSettings(user, project),
    },
  };
}

function memberDto(user: MockUser, project: MockProject, viewer: MockUser): JsonObject {
  const access = accessOf(user, project);
  const isOwner = access === Access.Owner;
  return {
    access,
    sharedTo: userDto(user),
    isOwner,
    isLocked: false,
    canEditAccess: !isOwner && canEditSettings(viewer, project) && user.id !== viewer.id,
    subjectType: 0,
  };
}

function linkDto(project: MockProject): JsonObject {
  const link = project.inviteLink;
  return {
    access: Access.Member,
    sharedTo: {
      id: link.id,
      title: link.title,
      shareLink: `/rooms/share?key=${link.requestToken}`,
      linkType: 1,
      primary: true,
      internal: false,
      requestToken: link.requestToken,
      isExpired: false,
      expirationDate: null,
      password: null,
      denyDownload: false,
    },
    isLocked: false,
    isOwner: false,
    canEditAccess: false,
    subjectType: 1,
  };
}

/* ------------------------------ Parsing -------------------------------- */

function parseTitle(body: JsonObject, required: boolean): string | undefined {
  const title = getString(body, "title");
  if (title === undefined) {
    if (required) {
      badRequest("title is required and must be a string");
    }
    return undefined;
  }
  const trimmed = title.trim();
  if (trimmed.length === 0 || trimmed.length > MAX_TITLE_LENGTH) {
    badRequest(`title must be 1 to ${MAX_TITLE_LENGTH} characters`);
  }
  return trimmed;
}

function parseLogoColor(body: JsonObject): string | undefined {
  const logo = getObject(body, "logo");
  const color = logo ? getString(logo, "color") : undefined;
  if (color !== undefined && !/^#[0-9a-fA-F]{6}$/.test(color)) {
    badRequest("logo.color must be a #rrggbb value");
  }
  return color;
}

/**
 * `logo.cover` of the body, as a room request carries it: the id of a
 * built-in cover, "" to go back to the initials, `undefined` when absent.
 * The ids are the portal's (`GET files/rooms/covers`); the mock cannot
 * check them and keeps the id as sent.
 */
function parseLogoCover(body: JsonObject): string | null | undefined {
  const logo = getObject(body, "logo");
  const id = logo ? getString(logo, "cover") : undefined;
  if (id === undefined) {
    return undefined;
  }
  if (id === "") {
    return null;
  }
  if (!/^[a-z0-9_]+$/i.test(id)) {
    badRequest("logo.cover must be the id of a built-in cover");
  }
  return id;
}

function parseRoomIds(
  settings: JsonObject,
  key: "instructionsRoomIds" | "knowledgeRoomIds",
): number[] | undefined {
  const raw = settings[key];
  if (raw === undefined) {
    return undefined;
  }
  if (!Array.isArray(raw)) {
    badRequest(`aiProject.${key} must be an array of room ids`);
  }
  const ids: number[] = [];
  for (const entry of raw) {
    const id = typeof entry === "number" ? entry : Number(entry);
    if (!Number.isInteger(id) || id <= 0) {
      badRequest(`aiProject.${key} must contain positive integer room ids`);
    }
    if (!ids.includes(id)) {
      ids.push(id);
    }
  }
  return ids;
}

interface ParsedSettings {
  profileId?: string | null;
  instructionsRoomIds?: number[];
  knowledgeRoomIds?: number[];
}

/**
 * `aiProject` of the create / update body, mirroring
 * `AiProjectSettingsRequestDto`: an absent field is "keep", `profileId: null`
 * is "use the portal default". Rooms are checked for type and for the
 * caller's read access, as the .NET validation will.
 */
function parseSettings(user: MockUser, body: JsonObject): ParsedSettings {
  const settings = getObject(body, "aiProject");
  if (!settings) {
    return {};
  }
  const out: ParsedSettings = {};
  if ("profileId" in settings) {
    const profileId = settings["profileId"];
    if (profileId === null) {
      out.profileId = null;
    } else if (typeof profileId !== "string" || !UUID_PATTERN.test(profileId)) {
      badRequest("aiProject.profileId must be a UUID or null");
    } else {
      const profile = findProfile(profileId);
      if (profile && !profile.enabled) {
        badRequest(`AI profile "${profileId}" is disabled`);
      }
      // A profile the mock does not list is a real one of the portal; it is
      // resolved by name when the project is read (see `resolveProfiles`).
      out.profileId = profileId;
    }
  }
  const instructions = parseRoomIds(settings, "instructionsRoomIds");
  if (instructions) {
    assertRooms(user, instructions, INSTRUCTIONS_ROOM_TYPE, "aiProject.instructionsRoomIds");
    out.instructionsRoomIds = instructions;
  }
  const knowledge = parseRoomIds(settings, "knowledgeRoomIds");
  if (knowledge) {
    assertRooms(user, knowledge, KNOWLEDGE_ROOM_TYPE, "aiProject.knowledgeRoomIds");
    out.knowledgeRoomIds = knowledge;
  }
  return out;
}

function assertRooms(user: MockUser, ids: number[], roomType: number, field: string): void {
  for (const id of ids) {
    // A room the mock does not list is a real one of the portal, picked in
    // the client's room selector; it joins the mock as an empty room.
    const room = ensureRoom(id, roomType as MockRoom["roomType"]);
    if (!canReadRoom(user, room)) {
      badRequest(`${field}: room ${id} does not exist or is not accessible`);
    }
    if (room.roomType !== roomType) {
      badRequest(`${field}: room ${id} is not of room type ${roomType}`);
    }
  }
}

function queryString(query: Record<string, unknown>, key: string): string | undefined {
  const value = query[key];
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function queryInt(query: Record<string, unknown>, key: string, fallback: number): number {
  const raw = queryString(query, key);
  if (raw === undefined) {
    return fallback;
  }
  const value = Number(raw);
  if (!Number.isInteger(value) || value < 0) {
    badRequest(`${key} must be a non-negative integer`);
  }
  return value;
}

function compareProjects(
  sortBy: string,
  descending: boolean,
): (a: MockProject, b: MockProject) => number {
  const direction = descending ? -1 : 1;
  const byTitle = (a: MockProject, b: MockProject): number =>
    a.title.localeCompare(b.title, undefined, { sensitivity: "base" });
  switch (sortBy) {
    case "AZ":
    case "Title":
      return (a, b) => direction * byTitle(a, b);
    case "DateAndTimeCreation":
      return (a, b) => direction * a.created.localeCompare(b.created);
    case "DateAndTime":
    default:
      return (a, b) => direction * a.updated.localeCompare(b.updated);
  }
}

/* ---------------------------- Membership -------------------------------- */

interface Invitation {
  user: MockUser;
  access: AccessValue;
}

function parseInvitations(project: MockProject, body: JsonObject): Invitation[] {
  const raw = getArray(body, "invitations");
  if (!raw || raw.length === 0) {
    badRequest("invitations must be a non-empty array");
  }
  const parsed: Array<{ user?: MockUser; pendingEmail?: string; access: AccessValue }> = [];
  for (const entry of raw) {
    if (!isObject(entry)) {
      badRequest("each invitation must be an object");
    }
    const access = entry["access"];
    if (typeof access !== "number") {
      badRequest("invitation.access must be a number");
    }
    const id = getString(entry, "id");
    const email = getString(entry, "email");
    // Nothing is written (no pending guest) until the whole batch is valid:
    // a rejected request must leave the users list as it was.
    let user: MockUser | undefined;
    let pendingEmail: string | undefined;
    if (id !== undefined) {
      if (!UUID_PATTERN.test(id)) {
        badRequest(`invitation: user ${id} does not exist`);
      }
      user = ensureUser(id);
    } else if (email !== undefined) {
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        badRequest(`invitation: "${email}" is not an email address`);
      }
      user = findUserByEmail(email);
      if (!user) {
        pendingEmail = email;
      }
    } else {
      badRequest("invitation needs an id or an email");
    }
    if (access === Access.None) {
      if (user && accessOf(user, project) === Access.Owner) {
        badRequest("the project owner cannot be removed; change the owner first");
      }
      if (user) {
        parsed.push({ user, access: Access.None });
      }
      continue;
    }
    if (user && accessOf(user, project) === Access.Owner) {
      badRequest(`invitation: ${user.displayName} is the project owner; use change-owner`);
    }
    // A new guest may only be a member, like any guest.
    const allowed = user ? allowedInviteAccess(user) : [Access.Member];
    if (!allowed.includes(access as AccessValue)) {
      const who = user ? user.displayName : (pendingEmail ?? "");
      badRequest(`invitation: ${who} can only be given access ${allowed.join(" or ")}`);
    }
    parsed.push({ user, pendingEmail, access: access as AccessValue });
  }
  return parsed.map((item) => ({
    user: item.user ?? createPendingGuest(item.pendingEmail ?? ""),
    access: item.access,
  }));
}

function logGrants(project: MockProject, granted: Array<[string, number]>): void {
  if (granted.length === 0) {
    return;
  }
  const lines = granted.map(([userId, roomId]) => `${userId} → room ${roomId}`);
  logger.info(`project ${project.id}: Viewer granted on linked rooms: ${lines.join(", ")}`);
}

/* ------------------------------ Handlers -------------------------------- */

export const projectsController = {
  // GET projects — the projects the caller may see, pinned first, as a
  // FolderContentDto of the projects root. Takes the room-list query:
  // `filterValue` (title substring), `sortBy` (DateAndTime | AZ |
  // DateAndTimeCreation), `sortOrder` (ascending | descending), `startIndex`,
  // `count`.
  // `role` (1 owner, 9 project manager, 11 member) keeps the projects the
  // caller holds that role in; `hasInstructions` / `hasKnowledge` (`true`)
  // keep the projects with at least one room of that kind.
  getProjects: asyncHandler(async (req, res) => {
    const user = caller();
    const query = req.query as Record<string, unknown>;
    const filter = queryString(query, "filterValue")?.toLowerCase();
    const sortBy = queryString(query, "sortBy") ?? "DateAndTime";
    const descending = (queryString(query, "sortOrder") ?? "descending") !== "ascending";
    const startIndex = queryInt(query, "startIndex", 0);
    const count = Math.min(queryInt(query, "count", DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);

    const role = queryInt(query, "role", 0);
    const hasInstructions = queryString(query, "hasInstructions") === "true";
    const hasKnowledge = queryString(query, "hasKnowledge") === "true";

    const visible = listProjects()
      .filter((p) => canSee(user, p))
      .filter((p) => filter === undefined || p.title.toLowerCase().includes(filter))
      .filter((p) => role === 0 || accessOf(user, p) === role)
      .filter((p) => !hasInstructions || p.instructionsRoomIds.length > 0)
      .filter((p) => !hasKnowledge || p.knowledgeRoomIds.length > 0);
    const compare = compareProjects(sortBy, descending);
    visible.sort((a, b) => {
      const pinA = a.pinnedBy.has(user.id) ? 1 : 0;
      const pinB = b.pinnedBy.has(user.id) ? 1 : 0;
      return pinB - pinA || compare(a, b);
    });
    const page = visible.slice(startIndex, startIndex + count);
    await resolveProfiles(page);

    envelope(res, {
      files: [],
      folders: page.map((p) => projectDto(user, p)),
      current: {
        id: PROJECTS_ROOT_ID,
        parentId: 0,
        rootFolderType: PROJECTS_ROOT_FOLDER_TYPE,
        title: "AI Projects",
        filesCount: 0,
        foldersCount: visible.length,
      },
      pathParts: [{ id: PROJECTS_ROOT_ID, title: "AI Projects" }],
      startIndex,
      count: page.length,
      total: visible.length,
      new: 0,
    });
  }),

  // GET projects/rooms — the Instructions (`type=10`) or Knowledge (`type=11`)
  // rooms the caller may link, for the "Room list" selector. `filterValue`
  // narrows by title. Only `type` 10 and 11 are allowed: a project links
  // nothing else.
  getLinkableRooms: asyncHandler(async (req, res) => {
    const user = caller();
    const query = req.query as Record<string, unknown>;
    const type = Number(queryString(query, "type"));
    if (type !== INSTRUCTIONS_ROOM_TYPE && type !== KNOWLEDGE_ROOM_TYPE) {
      badRequest(`type must be ${INSTRUCTIONS_ROOM_TYPE} or ${KNOWLEDGE_ROOM_TYPE}`);
    }
    const filter = queryString(query, "filterValue")?.toLowerCase();
    const rooms = listRooms()
      .filter((r) => r.roomType === type && canReadRoom(user, r))
      .filter((r) => filter === undefined || r.title.toLowerCase().includes(filter))
      .sort((a, b) => a.title.localeCompare(b.title));
    envelope(
      res,
      rooms.map((r) => ({
        ...sourceDto(r),
        createdBy: (() => {
          const creator = findUser(r.createdBy);
          return creator ? userDto(creator) : null;
        })(),
      })),
      { count: rooms.length, total: rooms.length },
    );
  }),

  // GET projects/users — the portal users the mock knows, for the invite
  // and change-owner panels (`filterValue` narrows by name or email).
  // Stands in for the People API while the data is mocked.
  getUsers: asyncHandler(async (req, res) => {
    const query = req.query as Record<string, unknown>;
    const filter = queryString(query, "filterValue")?.toLowerCase();
    const users = listUsers()
      .filter(
        (u) =>
          filter === undefined ||
          u.displayName.toLowerCase().includes(filter) ||
          u.email.toLowerCase().includes(filter),
      )
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
      .map((u) => ({
        ...userDto(u),
        employeeType: u.employeeType,
        canOwnProject: canHoldManagerRole(u),
      }));
    envelope(res, users, { count: users.length, total: users.length });
  }),

  // GET projects/profiles — the AI profiles a project can be bound to, as
  // the model picker of the project dialog lists them. Mock data; a project
  // may also be bound to any real profile of the portal (see parseSettings).
  getProfiles: asyncHandler(async (_req, res) => {
    const profiles = listProfiles().map((p) => ({
      id: p.id,
      name: p.name,
      provider: p.provider,
      available: p.enabled,
    }));
    envelope(res, profiles, { count: profiles.length, total: profiles.length });
  }),

  // GET projects/{id} — one project with its `aiProject` block and the
  // caller's `security`. 404 for a project the caller is not in (admins
  // see all).
  getProject: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    await resolveProfiles([project]);
    envelope(res, projectDto(user, project));
  }),

  // POST projects — creates a project; the caller becomes its owner and is
  // granted Viewer on the linked rooms they are not yet in. Only the portal
  // owner, DocSpace admins and room admins may create. Body: `title`,
  // optional `logo.color` and `logo.cover`, optional `aiProject` block.
  createProject: asyncHandler(async (req, res) => {
    const user = caller();
    if (!canHoldManagerRole(user)) {
      httpError(403, "only the portal owner, DocSpace admins and room admins can create projects");
    }
    const body: JsonObject = isObject(req.body) ? req.body : {};
    const title = parseTitle(body, true) as string;
    const settings = parseSettings(user, body);
    const project = createProject({
      title,
      logoColor: parseLogoColor(body),
      logoCover: parseLogoCover(body),
      createdBy: user.id,
      profileId: settings.profileId ?? null,
      instructionsRoomIds: settings.instructionsRoomIds ?? [],
      knowledgeRoomIds: settings.knowledgeRoomIds ?? [],
    });
    logger.info(`project ${project.id} "${project.title}" created by ${user.id}`);
    await resolveProfiles([project]);
    envelope(res, projectDto(user, project));
  }),

  // PUT projects/{id} — renames, recolors or changes the `aiProject` block.
  // Owner and project manager only. A room newly added to the project is
  // shared as Viewer with every member not yet in it.
  updateProject: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    requireSettingsAccess(user, project);
    const body: JsonObject = isObject(req.body) ? req.body : {};

    const title = parseTitle(body, false);
    const logoColor = parseLogoColor(body);
    const logoCover = parseLogoCover(body);
    const settings = parseSettings(user, body);

    if (title !== undefined) {
      project.title = title;
    }
    if (logoColor !== undefined) {
      project.logoColor = logoColor;
    }
    if (logoCover !== undefined) {
      project.logoCover = logoCover;
    }
    if (settings.profileId !== undefined) {
      project.profileId = settings.profileId;
    }
    const addedRooms: number[] = [];
    if (settings.instructionsRoomIds) {
      addedRooms.push(
        ...settings.instructionsRoomIds.filter((id) => !project.instructionsRoomIds.includes(id)),
      );
      project.instructionsRoomIds = settings.instructionsRoomIds;
    }
    if (settings.knowledgeRoomIds) {
      addedRooms.push(
        ...settings.knowledgeRoomIds.filter((id) => !project.knowledgeRoomIds.includes(id)),
      );
      project.knowledgeRoomIds = settings.knowledgeRoomIds;
    }
    touch(project);
    if (addedRooms.length > 0) {
      logGrants(project, grantViewerToMembers(project, undefined, addedRooms));
    }
    await resolveProfiles([project]);
    envelope(res, projectDto(user, project));
  }),

  // DELETE projects/{id} — removes the project. Owner only. Members lose the
  // project; the linked rooms and the Viewer shares on them stay. Answers
  // the finished-operation shape so a client written against the async
  // room delete can poll it once.
  deleteProject: asyncHandler(async (req, res) => {
    const user = caller();
    const id = projectIdParam(req.params["id"]);
    const project = requireProject(user, id);
    requireOwner(user, project);
    deleteProject(id);
    logger.info(`project ${id} deleted by ${user.id}`);
    envelope(res, [{ id: `delete-${id}`, operation: 2, progress: 100, finished: true, error: "" }]);
  }),

  // PUT projects/{id}/pin — pins the project for the caller.
  pinProject: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    if (!project.members.has(user.id)) {
      httpError(403, "only a member can pin the project");
    }
    project.pinnedBy.add(user.id);
    envelope(res, projectDto(user, project));
  }),

  // PUT projects/{id}/unpin — unpins the project for the caller.
  unpinProject: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    project.pinnedBy.delete(user.id);
    envelope(res, projectDto(user, project));
  }),

  // GET projects/{id}/share — the members with their roles, owner first,
  // as FileShareDto entries.
  getMembers: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    const members = [...project.members.keys()]
      .map((id) => findUser(id))
      .filter((u): u is MockUser => u !== undefined)
      .sort((a, b) => {
        const rank = (u: MockUser): number => (accessOf(u, project) === Access.Owner ? 0 : 1);
        return rank(a) - rank(b) || a.displayName.localeCompare(b.displayName);
      })
      .map((u) => memberDto(u, project, user));
    envelope(res, members, { count: members.length, total: members.length });
  }),

  // PUT projects/{id}/share — invites, re-roles or removes members (access 0
  // removes). Owner and project manager only. Roles: 9 project manager (for
  // the portal owner, DocSpace admins and room admins), 11 member (anyone).
  // Every added member is granted Viewer on each linked room they are not
  // yet in; an email invitation creates a pending guest and is granted the
  // same (the real service grants on acceptance).
  setMembers: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    requireSettingsAccess(user, project);
    const body: JsonObject = isObject(req.body) ? req.body : {};
    const invitations = parseInvitations(project, body);

    const added: string[] = [];
    for (const { user: member, access } of invitations) {
      if (access === Access.None) {
        project.members.delete(member.id);
        project.pinnedBy.delete(member.id);
        continue;
      }
      if (!project.members.has(member.id)) {
        added.push(member.id);
      }
      project.members.set(member.id, access);
    }
    touch(project);
    logGrants(project, grantViewerToMembers(project, added));
    if (getBoolean(body, "notify")) {
      logger.info(
        `project ${project.id}: notification requested: ${getString(body, "message") ?? ""}`,
      );
    }

    const members = [...project.members.keys()]
      .map((id) => findUser(id))
      .filter((u): u is MockUser => u !== undefined)
      .map((u) => memberDto(u, project, user));
    envelope(res, { members, warning: null });
  }),

  // PUT projects/{id}/leave — the caller leaves the project. The owner
  // cannot leave: they hand the project over first. The Viewer shares on
  // the linked rooms are kept (SPEC §11).
  leaveProject: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    const access = accessOf(user, project);
    if (access === Access.None) {
      badRequest("you are not a member of this project");
    }
    if (access === Access.Owner) {
      badRequest("the owner cannot leave the project; change the owner first");
    }
    project.members.delete(user.id);
    project.pinnedBy.delete(user.id);
    touch(project);
    envelope(res, { success: true });
  }),

  // PUT projects/{id}/owner — hands the project to another member who may
  // hold the owner role. Body `{ userId, keepAsManager }`: the previous
  // owner stays as project manager when `keepAsManager` is true, else
  // leaves the project. Owner only.
  changeOwner: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    requireOwner(user, project);
    const body = unpackPositional(req.body, ["userId", "keepAsManager"] as const);
    const userId = typeof body.userId === "string" ? body.userId : undefined;
    if (userId === undefined || !UUID_PATTERN.test(userId)) {
      badRequest("userId must be a UUID");
    }
    const next = ensureUser(userId);
    if (next.id === user.id) {
      badRequest("the user is already the owner");
    }
    if (!canHoldManagerRole(next)) {
      badRequest(`${next.displayName} cannot own a project (users and guests cannot)`);
    }
    const keepAsManager = body.keepAsManager !== false;
    project.members.set(next.id, Access.Owner);
    project.createdBy = next.id;
    if (keepAsManager) {
      project.members.set(user.id, Access.ProjectManager);
    } else {
      project.members.delete(user.id);
      project.pinnedBy.delete(user.id);
    }
    touch(project);
    logGrants(project, grantViewerToMembers(project, [next.id]));
    envelope(res, projectDto(user, project));
  }),

  // GET projects/{id}/links — the invitation link (Member role) as a
  // FileShareDto, empty when the link is off.
  getInviteLink: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    const links = project.inviteLink.enabled ? [linkDto(project)] : [];
    envelope(res, links, { count: links.length });
  }),

  // PUT projects/{id}/links — switches the invitation link on or off and
  // renames it. Body `{ enabled, title? }`. Owner and project manager only.
  // Turning the link off and on again issues a new token.
  setInviteLink: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    requireSettingsAccess(user, project);
    const body: JsonObject = isObject(req.body) ? req.body : {};
    const enabled = getBoolean(body, "enabled");
    if (enabled === undefined) {
      badRequest("enabled must be a boolean");
    }
    const title = getString(body, "title");
    if (title !== undefined) {
      project.inviteLink.title = title.trim() || project.inviteLink.title;
    }
    if (enabled && !project.inviteLink.enabled) {
      project.inviteLink.requestToken = `mock-token-${project.id}-${Date.now().toString(36)}`;
    }
    project.inviteLink.enabled = enabled;
    envelope(res, enabled ? linkDto(project) : null);
  }),

  // GET projects/{id}/skills — the skills of every linked Instructions room,
  // grouped by room, for the "View skills" aside. Which of them a chat
  // round uses is the user's per-session toggle, not stored here.
  getSkills: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    const rooms = roomsOf(project.instructionsRoomIds).map((room) => ({
      id: room.id,
      title: room.title,
      logo: roomLogoDto(room),
      skills: room.skills.map((s) => ({
        id: s.id,
        title: s.title,
        name: s.name,
        description: s.description,
      })),
    }));
    envelope(res, { rooms });
  }),

  // GET projects/{id}/knowledge — the files of every linked Knowledge room
  // with their indexing state, grouped by room, for the "View files" aside.
  getKnowledgeFiles: asyncHandler(async (req, res) => {
    const user = caller();
    const project = requireProject(user, projectIdParam(req.params["id"]));
    const rooms = roomsOf(project.knowledgeRoomIds).map((room) => ({
      id: room.id,
      title: room.title,
      logo: roomLogoDto(room),
      totalFiles: room.files.length,
      indexedFiles: room.files.filter((f) => f.indexed).length,
      files: room.files.map((f) => ({
        id: f.id,
        title: f.title,
        fileExst: f.fileExst,
        indexed: f.indexed,
      })),
    }));
    envelope(res, { rooms });
  }),
};
