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

/**
 * In-memory mock of the AI Projects backend.
 *
 * The real storage is the DocSpace Files service: a project is a room of
 * `RoomType.AiProjectRoom` whose settings (`profile_id`,
 * `instructions_room_ids`, `knowledge_room_ids`) sit next to the room's
 * chat settings, and membership is `files_security`. Until that lands, this
 * module keeps the same shapes in process memory so the client can be built
 * against `/api/2.0/ai/projects/*` (see `projectsController`). Nothing here
 * survives a restart, and the caller is always `CURRENT_USER_ID`: the mock
 * has no session, so the seed gives that user every role once (owner,
 * project manager, member) to exercise each view.
 */

/** `RoomType.AiProjectRoom` on the C# side. */
export const PROJECT_ROOM_TYPE = 12;
/** `RoomType.InstructionsRoom`: the rooms whose Markdown files are skills. */
export const INSTRUCTIONS_ROOM_TYPE = 10;
/** `RoomType.KnowledgeRoom`: the rooms whose files are searched by the chat. */
export const KNOWLEDGE_ROOM_TYPE = 11;
/** `FolderType.AiProjectRoom` on the C# side. */
export const PROJECT_FOLDER_TYPE = 40;
/** `FolderType.AiProjects`, the projects root; placeholder until the C# enum lands. */
export const PROJECTS_ROOT_FOLDER_TYPE = 41;
/** The id of the projects root folder in the mock. */
/** Mock ids start at 900000 so they never collide with the portal's real rooms. */
export const MOCK_ID_BASE = 900000;
export const PROJECTS_ROOT_ID = MOCK_ID_BASE;

/**
 * `FileShare` on the C# side, limited to what a project uses: the owner holds
 * `ReadWrite` (full access), a project manager `RoomManager`, a member
 * `ContentCreator` (the "Collaborator" role of ordinary rooms), and the share
 * a project grants on its linked rooms is `Read` (Viewer). `None` removes.
 */
export const Access = {
  None: 0,
  Owner: 1,
  Viewer: 2,
  ProjectManager: 9,
  Member: 11,
} as const;
export type AccessValue = (typeof Access)[keyof typeof Access];

/** `EmployeeType` on the C# side. */
export const EmployeeType = {
  RoomAdmin: 1,
  Guest: 2,
  DocSpaceAdmin: 3,
  User: 4,
} as const;
export type EmployeeTypeValue = (typeof EmployeeType)[keyof typeof EmployeeType];

export interface MockUser {
  id: string;
  displayName: string;
  email: string;
  employeeType: EmployeeTypeValue;
  /** Portal owner. */
  isOwner: boolean;
  /** `EmployeeStatus`: 1 active, 2 pending (invited by email, not yet signed in). */
  status: 1 | 2;
  avatarColor: string;
}

export interface MockSkill {
  id: number;
  /** File title, e.g. `tone-of-voice.md`. */
  title: string;
  /** Frontmatter `name`, what the model sees. */
  name: string;
  description: string;
}

export interface MockKnowledgeFile {
  id: number;
  title: string;
  fileExst: string;
  indexed: boolean;
}

export interface MockRoom {
  id: number;
  title: string;
  roomType: typeof INSTRUCTIONS_ROOM_TYPE | typeof KNOWLEDGE_ROOM_TYPE;
  createdBy: string;
  skills: MockSkill[];
  files: MockKnowledgeFile[];
  /** `files_security` of the room: user id → access. */
  members: Map<string, AccessValue>;
  /** The room logo colour; unset rooms take one from `ROOM_LOGO_COLORS` by id. */
  logoColor?: string;
  /** Id of the built-in cover drawn in the colour; unset draws the initials. */
  logoCover?: string | null;
}

export interface MockProfile {
  id: string;
  name: string;
  provider: string;
  enabled: boolean;
}

export interface MockInviteLink {
  id: string;
  title: string;
  enabled: boolean;
  requestToken: string;
}

export interface MockProject {
  id: number;
  title: string;
  logoColor: string;
  /**
   * Id of the built-in cover drawn in `logoColor` (one of
   * `GET files/rooms/covers`); `null` draws the initials. The mock keeps the
   * id only: the SVG is the portal's, and the client reads it from that list.
   */
  logoCover: string | null;
  createdBy: string;
  created: string;
  updated: string;
  /** User ids who pinned the project (pinning is per user, as for rooms). */
  pinnedBy: Set<string>;
  /** `null` = the portal default profile. */
  profileId: string | null;
  instructionsRoomIds: number[];
  knowledgeRoomIds: number[];
  /** `files_security` of the project: user id → access. */
  members: Map<string, AccessValue>;
  inviteLink: MockInviteLink;
}

/** The caller of every request: the mock has no session (see the module doc). */
export const CURRENT_USER_ID = "a1f0c3b2-0001-4a6e-9b0d-000000000001";

const USERS: MockUser[] = [
  {
    id: CURRENT_USER_ID,
    displayName: "Administrator",
    email: "admin@example.com",
    employeeType: EmployeeType.DocSpaceAdmin,
    isOwner: true,
    status: 1,
    avatarColor: "#E57C3A",
  },
  {
    id: "a1f0c3b2-0001-4a6e-9b0d-000000000002",
    displayName: "Anna K.",
    email: "anna@example.com",
    employeeType: EmployeeType.RoomAdmin,
    isOwner: false,
    status: 1,
    avatarColor: "#4781D1",
  },
  {
    id: "a1f0c3b2-0001-4a6e-9b0d-000000000003",
    displayName: "Pavel R.",
    email: "pavel@example.com",
    employeeType: EmployeeType.DocSpaceAdmin,
    isOwner: false,
    status: 1,
    avatarColor: "#E5A04A",
  },
  {
    id: "a1f0c3b2-0001-4a6e-9b0d-000000000004",
    displayName: "Mark S.",
    email: "mark@example.com",
    employeeType: EmployeeType.User,
    isOwner: false,
    status: 1,
    avatarColor: "#2DB482",
  },
  {
    id: "a1f0c3b2-0001-4a6e-9b0d-000000000005",
    displayName: "Irina V.",
    email: "irina@example.com",
    employeeType: EmployeeType.Guest,
    isOwner: false,
    status: 1,
    avatarColor: "#5B3FB3",
  },
];

const [ME, ANNA, PAVEL, MARK, IRINA] = USERS as [MockUser, MockUser, MockUser, MockUser, MockUser];

const PROFILES: MockProfile[] = [
  {
    id: "b2e1d4c3-0002-4b7f-8c1e-000000000001",
    name: "GPT-5",
    provider: "OpenAI",
    enabled: true,
  },
  {
    id: "b2e1d4c3-0002-4b7f-8c1e-000000000002",
    name: "Claude Sonnet 4.5",
    provider: "Anthropic",
    enabled: true,
  },
  {
    id: "b2e1d4c3-0002-4b7f-8c1e-000000000003",
    name: "Mistral Large",
    provider: "Mistral",
    enabled: false,
  },
];

function skills(start: number, entries: [string, string][]): MockSkill[] {
  return entries.map(([title, description], i) => ({
    id: start + i,
    title,
    name: title.replace(/\.md$/, ""),
    description,
  }));
}

function files(start: number, total: number, indexed: number, stem: string): MockKnowledgeFile[] {
  const out: MockKnowledgeFile[] = [];
  const exts = [".docx", ".xlsx", ".pdf", ".pptx"];
  for (let i = 0; i < total; i += 1) {
    const fileExst = exts[i % exts.length] as string;
    out.push({
      id: start + i,
      title: `${stem} ${i + 1}${fileExst}`,
      fileExst,
      indexed: i < indexed,
    });
  }
  return out;
}

function members(entries: [MockUser, AccessValue][]): Map<string, AccessValue> {
  return new Map(entries.map(([user, access]) => [user.id, access]));
}

const ROOMS: MockRoom[] = [
  {
    id: 900101,
    title: "Brand voice",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ME.id,
    skills: skills(9001011, [
      ["tone-of-voice.md", "How the brand speaks: warm, direct, no jargon."],
      ["headline-rules.md", "Length, casing and punctuation rules for headlines."],
      ["legal-disclaimers.md", "Disclaimers that must follow every price claim."],
    ]),
    files: [],
    members: members([
      [ME, Access.Owner],
      [ANNA, Access.ProjectManager],
    ]),
  },
  {
    id: 900102,
    title: "Legal tone",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ANNA.id,
    skills: skills(9001021, [
      ["formal-register.md", "Formal register and defined-terms conventions."],
      ["clause-numbering.md", "How clauses and sub-clauses are numbered."],
    ]),
    files: [],
    members: members([
      [ANNA, Access.Owner],
      [ME, Access.Viewer],
    ]),
  },
  {
    id: 900103,
    title: "Contract rules",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ANNA.id,
    skills: skills(9001031, [
      ["review-checklist.md", "What to check in every incoming contract."],
      ["risk-flags.md", "Clauses that must be escalated to legal."],
      ["summary-format.md", "Layout of a contract review summary."],
      ["jurisdiction-notes.md", "Per-jurisdiction notes for governing-law clauses."],
    ]),
    files: [],
    members: members([
      [ANNA, Access.Owner],
      [ME, Access.Viewer],
      [PAVEL, Access.Viewer],
    ]),
  },
  {
    id: 900104,
    title: "Support playbook",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: PAVEL.id,
    skills: skills(9001041, [["answer-style.md", "How support answers are structured."]]),
    files: [],
    members: members([
      [PAVEL, Access.Owner],
      [ME, Access.Viewer],
      [MARK, Access.Viewer],
    ]),
  },
  {
    id: 900105,
    title: "HR tone",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ME.id,
    skills: skills(9001051, [
      ["welcome-voice.md", "Friendly, plain-language voice for new hires."],
      ["inclusive-language.md", "Inclusive wording and pronoun conventions."],
    ]),
    files: [],
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900106,
    title: "Policy format",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ME.id,
    skills: skills(9001061, [["policy-template.md", "Sections every policy page must have."]]),
    files: [],
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900107,
    title: "IT setup steps",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ME.id,
    skills: skills(9001071, [
      ["device-checklist.md", "Order of steps when a laptop is issued."],
      ["access-requests.md", "How to ask for accounts and permissions."],
    ]),
    files: [],
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900108,
    title: "Security basics",
    roomType: INSTRUCTIONS_ROOM_TYPE,
    createdBy: ME.id,
    skills: skills(9001081, [
      ["security-rules.md", "Password, MFA and phishing rules for everyone."],
    ]),
    files: [],
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900201,
    title: "Product catalogue",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002011, 120, 118, "Product sheet"),
    members: members([
      [ME, Access.Owner],
      [ANNA, Access.Viewer],
    ]),
  },
  {
    id: 900202,
    title: "Pricing 2026",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002201, 14, 14, "Price list"),
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900203,
    title: "Signed contracts 2026",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ANNA.id,
    skills: [],
    files: files(9002301, 42, 40, "Contract"),
    members: members([
      [ANNA, Access.Owner],
      [ME, Access.Viewer],
    ]),
  },
  {
    id: 900204,
    title: "Help center articles",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: PAVEL.id,
    skills: [],
    files: files(9002401, 63, 63, "Article"),
    members: members([
      [PAVEL, Access.Owner],
      [ME, Access.Viewer],
      [MARK, Access.Viewer],
    ]),
  },
  {
    id: 900205,
    title: "HR handbook",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002501, 18, 18, "Handbook chapter"),
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900206,
    title: "Benefits guide",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002601, 9, 7, "Benefit"),
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900207,
    title: "Office rules",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002701, 6, 6, "Rule"),
    members: members([[ME, Access.Owner]]),
  },
  {
    id: 900208,
    title: "Tools and accounts",
    roomType: KNOWLEDGE_ROOM_TYPE,
    createdBy: ME.id,
    skills: [],
    files: files(9002801, 25, 20, "Tool guide"),
    members: members([[ME, Access.Owner]]),
  },
];

let nextProjectId = MOCK_ID_BASE + 4;
let nextUserSuffix = 100;

function inviteLink(id: number, enabled: boolean): MockInviteLink {
  return {
    id: `c3d2e5f4-0003-4c80-9d2f-${String(id).padStart(12, "0")}`,
    title: "Invitation link",
    enabled,
    requestToken: `mock-token-${id}`,
  };
}

const PROJECTS: MockProject[] = [
  {
    id: MOCK_ID_BASE + 1,
    title: "Marketing copy",
    logoColor: "#5B3FB3",
    logoCover: null,
    createdBy: ME.id,
    created: "2026-01-15T12:00:00.000Z",
    updated: "2026-10-20T09:30:00.000Z",
    pinnedBy: new Set([ME.id]),
    profileId: (PROFILES[0] as MockProfile).id,
    instructionsRoomIds: [900101],
    knowledgeRoomIds: [900201],
    members: members([
      [ME, Access.Owner],
      [ANNA, Access.ProjectManager],
      [MARK, Access.Member],
    ]),
    inviteLink: inviteLink(MOCK_ID_BASE + 1, false),
  },
  {
    id: MOCK_ID_BASE + 2,
    title: "Legal review",
    logoColor: "#2E5BA8",
    logoCover: null,
    createdBy: ANNA.id,
    created: "2026-02-03T08:15:00.000Z",
    updated: "2026-10-18T16:45:00.000Z",
    pinnedBy: new Set(),
    profileId: (PROFILES[1] as MockProfile).id,
    instructionsRoomIds: [900102, 900103],
    knowledgeRoomIds: [900203],
    members: members([
      [ANNA, Access.Owner],
      [ME, Access.ProjectManager],
      [PAVEL, Access.Member],
    ]),
    inviteLink: inviteLink(MOCK_ID_BASE + 2, true),
  },
  // Four rooms of each kind: exercises the "+N" overflow of the tags and the
  // wrapping of the sources column.
  {
    id: MOCK_ID_BASE + 3,
    title: "Onboarding docs",
    logoColor: "#E57C3A",
    logoCover: null,
    createdBy: ME.id,
    created: "2026-04-02T09:00:00.000Z",
    updated: "2026-10-22T14:10:00.000Z",
    pinnedBy: new Set(),
    profileId: (PROFILES[0] as MockProfile).id,
    instructionsRoomIds: [900105, 900106, 900107, 900108],
    knowledgeRoomIds: [900205, 900206, 900207, 900208],
    members: members([
      [ME, Access.Owner],
      [IRINA, Access.ProjectManager],
      [ANNA, Access.Member],
    ]),
    inviteLink: inviteLink(MOCK_ID_BASE + 3, false),
  },
];

// The last seed project is created through the same path a client uses, so
// the Viewer grant on the linked rooms is exercised by the seed itself.
function seedThirdProject(): void {
  const project: MockProject = {
    id: nextProjectId,
    title: "Support answers",
    logoColor: "#2DB482",
    logoCover: null,
    createdBy: PAVEL.id,
    created: "2026-03-10T10:00:00.000Z",
    updated: "2026-10-01T11:20:00.000Z",
    pinnedBy: new Set(),
    profileId: null,
    instructionsRoomIds: [900104],
    knowledgeRoomIds: [900204],
    members: members([
      [PAVEL, Access.Owner],
      [ME, Access.Member],
      [MARK, Access.Member],
      [IRINA, Access.Member],
    ]),
    inviteLink: inviteLink(nextProjectId, false),
  };
  nextProjectId += 1;
  PROJECTS.push(project);
  grantViewerToMembers(project);
}

export function listUsers(): MockUser[] {
  return USERS;
}

export function findUser(id: string): MockUser | undefined {
  return USERS.find((u) => u.id === id);
}

export function findUserByEmail(email: string): MockUser | undefined {
  const needle = email.trim().toLowerCase();
  return USERS.find((u) => u.email.toLowerCase() === needle);
}

/** A guest invited by email: pending until they sign in, as in DocSpace. */
export function createPendingGuest(email: string): MockUser {
  nextUserSuffix += 1;
  const user: MockUser = {
    id: `a1f0c3b2-0001-4a6e-9b0d-${String(nextUserSuffix).padStart(12, "0")}`,
    displayName: email.trim(),
    email: email.trim(),
    employeeType: EmployeeType.Guest,
    isOwner: false,
    status: 2,
    avatarColor: "#A3A9AE",
  };
  USERS.push(user);
  return user;
}

/**
 * A user the mock does not know (a real portal account picked in the
 * client's people selector): added as a room admin so any role is allowed,
 * named after the id until the real service replaces the mock.
 */
export function ensureUser(id: string): MockUser {
  const known = findUser(id);
  if (known) {
    return known;
  }
  const user: MockUser = {
    id,
    displayName: `User ${id.slice(0, 8)}`,
    email: `${id.slice(0, 8)}@example.com`,
    employeeType: EmployeeType.RoomAdmin,
    isOwner: false,
    status: 1,
    avatarColor: "#657077",
  };
  USERS.push(user);
  return user;
}

/**
 * A room the mock does not know (a real Instructions or Knowledge room
 * picked in the client's room selector): added empty, readable by everyone.
 */
export function ensureRoom(id: number, roomType: MockRoom["roomType"]): MockRoom {
  const known = findRoom(id);
  if (known) {
    return known;
  }
  const room: MockRoom = {
    id,
    title: `Room ${id}`,
    roomType,
    createdBy: CURRENT_USER_ID,
    skills: [],
    files: [],
    members: new Map([[CURRENT_USER_ID, Access.Owner]]),
  };
  ROOMS.push(room);
  return room;
}

export function listProfiles(): MockProfile[] {
  return PROFILES;
}

export function findProfile(id: string): MockProfile | undefined {
  return PROFILES.find((p) => p.id === id);
}

/** The colours a new room is given at random in the client (`logoColors`). */
const ROOM_LOGO_COLORS = ["#FF6680", "#FF8F40", "#F2D230", "#61C059", "#1FCECB", "#5CC3F7", "#6191F2", "#7757D9", "#FF7FD4"];

/** The room's logo as a room DTO carries it: its colour and cover id. */
export function roomLogo(room: MockRoom): { color: string; cover: string | null } {
  return {
    color: room.logoColor ?? ROOM_LOGO_COLORS[room.id % ROOM_LOGO_COLORS.length]!,
    cover: room.logoCover ?? null,
  };
}

export function listRooms(): MockRoom[] {
  return ROOMS;
}

export function findRoom(id: number): MockRoom | undefined {
  return ROOMS.find((r) => r.id === id);
}

export function listProjects(): MockProject[] {
  return PROJECTS;
}

export function findProject(id: number): MockProject | undefined {
  return PROJECTS.find((p) => p.id === id);
}

export function isPortalAdmin(user: MockUser): boolean {
  return user.isOwner || user.employeeType === EmployeeType.DocSpaceAdmin;
}

/** Owner and project manager are for the portal owner, DocSpace admins and room admins only. */
export function canHoldManagerRole(user: MockUser): boolean {
  return (
    user.isOwner ||
    user.employeeType === EmployeeType.DocSpaceAdmin ||
    user.employeeType === EmployeeType.RoomAdmin
  );
}

/** Users and guests can only be members; the owner role is set through change-owner, not share. */
export function allowedInviteAccess(user: MockUser): AccessValue[] {
  return canHoldManagerRole(user) ? [Access.ProjectManager, Access.Member] : [Access.Member];
}

/** Admins see every room; others the rooms they are in. */
export function canReadRoom(user: MockUser, room: MockRoom): boolean {
  return isPortalAdmin(user) || room.members.has(user.id);
}

export function createProject(input: {
  title: string;
  logoColor?: string;
  logoCover?: string | null;
  createdBy: string;
  profileId: string | null;
  instructionsRoomIds: number[];
  knowledgeRoomIds: number[];
}): MockProject {
  const now = new Date().toISOString();
  const project: MockProject = {
    id: nextProjectId,
    title: input.title,
    logoColor: input.logoColor ?? "#4781D1",
    logoCover: input.logoCover ?? null,
    createdBy: input.createdBy,
    created: now,
    updated: now,
    pinnedBy: new Set(),
    profileId: input.profileId,
    instructionsRoomIds: [...input.instructionsRoomIds],
    knowledgeRoomIds: [...input.knowledgeRoomIds],
    members: new Map([[input.createdBy, Access.Owner]]),
    inviteLink: inviteLink(nextProjectId, false),
  };
  nextProjectId += 1;
  PROJECTS.push(project);
  grantViewerToMembers(project);
  return project;
}

export function deleteProject(id: number): boolean {
  const index = PROJECTS.findIndex((p) => p.id === id);
  if (index === -1) {
    return false;
  }
  PROJECTS.splice(index, 1);
  return true;
}

export function touch(project: MockProject): void {
  project.updated = new Date().toISOString();
}

export function linkedRooms(project: MockProject): MockRoom[] {
  const ids = [...project.instructionsRoomIds, ...project.knowledgeRoomIds];
  return ids.map((id) => findRoom(id)).filter((r): r is MockRoom => r !== undefined);
}

/**
 * The access rule of a project: every member gets Viewer on every linked
 * Instructions and Knowledge room they are not yet in. An existing share is
 * left as it is, so a room owner or editor is never downgraded. In the real
 * service these are `files_security` rows written on behalf of the project
 * owner; removal from the project does not revoke them.
 *
 * Returns the `[userId, roomId]` pairs that were granted, for logging.
 */
export function grantViewerToMembers(
  project: MockProject,
  onlyUsers?: Iterable<string>,
  onlyRooms?: Iterable<number>,
): Array<[string, number]> {
  const users = onlyUsers ? [...onlyUsers] : [...project.members.keys()];
  const roomFilter = onlyRooms ? new Set(onlyRooms) : undefined;
  const granted: Array<[string, number]> = [];
  for (const room of linkedRooms(project)) {
    if (roomFilter && !roomFilter.has(room.id)) {
      continue;
    }
    for (const userId of users) {
      if (!project.members.has(userId) || room.members.has(userId)) {
        continue;
      }
      room.members.set(userId, Access.Viewer);
      granted.push([userId, room.id]);
    }
  }
  return granted;
}

seedThirdProject();
