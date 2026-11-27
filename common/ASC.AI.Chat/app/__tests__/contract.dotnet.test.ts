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

import { describe, expect, it } from "vitest";
import type { CsEnum } from "./helpers/csharp.js";
import {
  accepts,
  constantOf,
  hasCsharpSources,
  readClass,
  readEnum,
  readsOf,
} from "./helpers/csharp.js";

// The contract with the .NET services, checked on the sources of both sides: the properties a
// C# DTO class declares against the fields the Node code reads from its JSON. It runs no
// service; it fails when a property is added, removed, renamed or changes its kind on either
// side without the other, which the unit tests cannot see because their fixtures are written
// by hand. What only a running service shows (a property that is declared and left null, a
// serializer setting) is for products/ASC.AI/ASC.AI.Tests.

const AI = "products/ASC.AI/Server/Models/";
const FILES = "products/ASC.Files/Core/ApiModels/ResponseDto/";
const SKILLS = "products/ASC.Files/Server/ApiModels/ResponseDto/AiSkillsDto.cs";

const ENUM_FILES: Record<string, string> = {
  ToolPermissionMode: "products/ASC.AI/Core/Settings/ToolPermissionMode.cs",
  ReasoningDepth: "common/ASC.AI.Integration/Profiles/ReasoningConfig.cs",
  Capabilities: "common/ASC.AI.Integration/Profiles/Capabilities.cs",
  DocumentAgentReasoning: "common/ASC.AI.Integration/DocumentAgent/DocumentAgentSettings.cs",
  FileType: "web/ASC.Web.Core/Files/FileType.cs",
  FolderType: "products/ASC.Files/Core/Core/Entries/Folder.cs",
  RoomType: "products/ASC.Files/Core/ApiModels/RequestDto/CreateRoomRequestDto.cs",
};

const BASES: Record<string, string> = {
  FileEntryDto: FILES + "FileEntryDto.cs",
  FileEntryBaseDto: FILES + "FileEntryDto.cs",
  EmployeeDto: "common/ASC.Api.Core/Model/EmployeeDto.cs",
};

interface Contract {
  /** The C# classes whose JSON the function reads: [file, class]. More than one for a page with its cursor. */
  cs: [string, string][];
  /** The Node function that reads it: [file, function]. */
  node: [string, string];
  /**
   * Fields Node reads that no C# class of the row declares, each with the reason it is read
   * anyway. A field that is here and gets declared, or stops being read, fails the test too.
   */
  notSent?: Record<string, string>;
  /**
   * Properties C# declares that the function does not read, each with the reason. Left out for
   * the Files and People DTOs, of which Node reads a few properties out of dozens.
   */
  unread?: Record<string, string>;
  /** Fields read as another kind than C# sends, each with the reason. */
  otherKind?: Record<string, string>;
}

const CONTRACTS: Contract[] = [
  {
    cs: [[AI + "ResponseDto/ProfileDto.cs", "ProfileDto"]],
    node: ["app/storage/profilesStorage.ts", "dtoToProfile"],
    notSent: {
      useProxy: "ProfileDto has no such property: the read never finds a value",
      isCloudProvider: "ProfileDto has no such property: the read never finds a value",
      contextWindow:
        "not a C# column; read so that a DTO which grows it feeds compaction (comment in the code)",
    },
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/ThreadDto.cs", "ThreadDto"]],
    node: ["app/storage/threadsStorage.ts", "dtoToThread"],
    unread: {
      createdAt: "not read: dtoToThread maps id, title, lastEditDate and profileId",
      entityId: "not read: dtoToThread maps id, title, lastEditDate and profileId",
    },
  },
  {
    cs: [
      [AI + "ResponseDto/ThreadsPageDto.cs", "ThreadsPageDto"],
      [AI + "RequestDto/Threads/ThreadsCursorDto.cs", "ThreadsCursorDto"],
    ],
    node: ["app/storage/threadsStorage.ts", "parseThreadsPage"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/MessageDto.cs", "MessageDto"]],
    node: ["app/storage/messagesStorage.ts", "dtoToMessage"],
    unread: { threadId: "not read: dtoToMessage maps id, timestamp and the contents" },
  },
  {
    cs: [
      [AI + "ResponseDto/MessagesPageDto.cs", "MessagesPageDto"],
      [AI + "RequestDto/Messages/MessagesCursorDto.cs", "MessagesCursorDto"],
    ],
    node: ["app/storage/messagesStorage.ts", "parseMessagesPage"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/AttachmentDto.cs", "AttachmentDto"]],
    node: ["app/storage/attachmentsStorage.ts", "dtoToAttachment"],
    notSent: {
      base64: "AttachmentDto has no such property: the fallback after dataUrl never finds a value",
      source:
        "AttachmentDto does not echo it; the caller back-fills it for tool uploads (comment in the code)",
    },
    unread: { type: "the file type is worked out from the title by getOnlyofficeFileType" },
  },
  {
    cs: [[AI + "ResponseDto/PromptDto.cs", "PromptDto"]],
    node: ["app/storage/promptsStorage.ts", "parsePrompt"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/PromptFolderDto.cs", "PromptFolderDto"]],
    node: ["app/storage/promptFoldersStorage.ts", "parseFolder"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/WebSearchConfigDto.cs", "WebSearchConfigDto"]],
    node: ["app/storage/webSearchStorage.ts", "parseWebSearchConfig"],
    notSent: {
      isCloudProvider: "WebSearchConfigDto has no such property: the read never finds a value",
    },
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/McpServerDto.cs", "McpServerDto"]],
    node: ["app/storage/mcpServersStorage.ts", "readRegistry"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/ByokProviderDto.cs", "ByokKeyDto"]],
    node: ["app/storage/byokKeysStorage.ts", "dtoToByokKey"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/ByokProviderDto.cs", "ByokModelDto"]],
    node: ["app/storage/byokKeysStorage.ts", "dtoToByokModel"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/ByokProviderDto.cs", "ByokProviderDto"]],
    node: ["app/storage/byokKeysStorage.ts", "dtoToByokProvider"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/ChatContextDto.cs", "ChatContextFolderDto"]],
    node: ["app/storage/chatContext.ts", "parseFolder"],
    unread: {
      isAgent: "not read: parseFolder maps id, title, folderType and canCreate",
      prompt: "not read: parseFolder maps id, title, folderType and canCreate",
    },
  },
  {
    cs: [
      [AI + "ResponseDto/ChatContextDto.cs", "ChatContextScopeDto"],
      [AI + "ResponseDto/PreferencesDto.cs", "PreferencesDto"],
    ],
    node: ["app/storage/chatContext.ts", "parseScope"],
    unread: { entityId: "not read: parseScope takes the entity id from the request" },
  },
  {
    cs: [
      [AI + "ResponseDto/ChatContextDto.cs", "ChatContextDto"],
      [AI + "ResponseDto/AiSettingsDto.cs", "AiSettingsDto"],
    ],
    node: ["app/storage/chatContext.ts", "parseChatContext"],
    unread: {
      embeddingModel: "not read: parseChatContext takes aiReady and toolPermissionMode from config",
      recommendedModelForForms:
        "not read: parseChatContext takes aiReady and toolPermissionMode from config",
      systemAiEnabled:
        "not read: parseChatContext takes aiReady and toolPermissionMode from config",
      vectorizationEnabled:
        "not read: parseChatContext takes aiReady and toolPermissionMode from config",
      vectorizationNeedReset:
        "not read: parseChatContext takes aiReady and toolPermissionMode from config",
    },
  },
  {
    cs: [
      [AI + "ResponseDto/FormAnalysisDto.cs", "FormSchemaDto"],
      [AI + "ResponseDto/FormAnalysisDto.cs", "FormColumnDto"],
    ],
    node: ["app/storage/formAnalysisStorage.ts", "parseSchema"],
    unread: {},
  },
  {
    cs: [[AI + "ResponseDto/FormAnalysisDto.cs", "FormQuestionDto"]],
    node: ["app/storage/formAnalysisStorage.ts", "parseQuestions"],
    unread: {},
  },
  {
    cs: [[FILES + "FolderDto.cs", "FolderDto"]],
    node: ["app/storage/docspaceFilesApi.ts", "parseFolderInfo"],
    notSent: { response: "the DocSpace envelope `{ response }` around the DTO" },
  },
  {
    cs: [[FILES + "FileDto.cs", "FileDto"]],
    node: ["app/storage/docspaceFilesApi.ts", "parseFileInfo"],
    notSent: { response: "the DocSpace envelope `{ response }` around the DTO" },
  },
  {
    cs: [["common/ASC.Api.Core/Model/EmployeeFullDto.cs", "EmployeeFullDto"]],
    node: ["app/storage/docspacePeopleApi.ts", "getSelf"],
  },
  {
    cs: [["common/ASC.Api.Core/Model/EmployeeFullDto.cs", "EmployeeFullDto"]],
    node: ["app/storage/docspacePeopleApi.ts", "getSelfId"],
  },
  {
    cs: [["web/ASC.Web.Api/ApiModels/ResponseDto/WebPluginDto.cs", "WebPluginDto"]],
    node: ["app/storage/docspacePeopleApi.ts", "parseWebPlugin"],
  },
  {
    cs: [
      [SKILLS, "AiSkillsDto"],
      [SKILLS, "AiSkillsRoomDto"],
    ],
    node: ["app/storage/roomSkills.ts", "parseSkillsCatalog"],
  },
  { cs: [[SKILLS, "AiSkillDto"]], node: ["app/storage/roomSkills.ts", "parseSkill"] },
  { cs: [[FILES + "FileDto.cs", "FileDto"]], node: ["app/meta/portalFiles.ts", "toPortalFile"] },
];

describe.skipIf(!hasCsharpSources())(
  "the DTOs of the .NET services and the code that reads them",
  () => {
    const enums = new Map<string, CsEnum>(
      Object.entries(ENUM_FILES).map(([name, file]) => [name, readEnum(file, name)]),
    );

    it.each(CONTRACTS.map((c) => [`${c.node[1]} of ${c.node[0]}`, c] as const))(
      "%s reads what C# declares",
      (_name, contract) => {
        const declared = new Map(
          contract.cs.flatMap(([file, name]) =>
            readClass(file, name, { enums, bases: BASES }).map((p) => [p.name, p] as const),
          ),
        );
        const reads = readsOf(...contract.node);

        const notSent = [...reads.keys()].filter((field) => !declared.has(field)).sort();
        const otherKind = [...reads]
          .filter(([field, how]) => {
            const property = declared.get(field);
            return property !== undefined && !accepts(how, property.kind);
          })
          .map(([field, how]) => `${field}: read as ${how}, sent as ${declared.get(field)?.kind}`)
          .sort();
        const unread = [...declared.keys()].filter((field) => !reads.has(field)).sort();

        expect({
          notSent,
          otherKind,
          ...(contract.unread ? { unread } : {}),
        }).toEqual({
          notSent: Object.keys(contract.notSent ?? {}).sort(),
          otherKind: Object.keys(contract.otherKind ?? {}).sort(),
          ...(contract.unread ? { unread: Object.keys(contract.unread).sort() } : {}),
        });
      },
    );
  },
);

// An enum crosses the boundary as a number, or by name where the C# type carries a string
// converter. A member added on the C# side reaches Node as a value it has never seen.
describe.skipIf(!hasCsharpSources())(
  "the enums of the .NET services and the values Node knows",
  () => {
    const camelCase = (name: string): string => name.charAt(0).toLowerCase() + name.slice(1);

    it("ToolPermissionMode: every member maps to a library mode and back to its number", async () => {
      const { csharpToToolPermissionMode, toolPermissionModeToCsharp } =
        await import("../storage/toolPermissionMode.js");
      const cs = readEnum(ENUM_FILES["ToolPermissionMode"] ?? "", "ToolPermissionMode");

      expect(cs.asString).toBe(false);
      for (const member of cs.members) {
        const mode = csharpToToolPermissionMode(member.value);
        expect(mode, member.name).toBe(member.name.toLowerCase());
        expect(mode && toolPermissionModeToCsharp(mode), member.name).toBe(member.value);
      }
      expect(csharpToToolPermissionMode(cs.members.length)).toBeNull();
    });

    it("ReasoningDepth: every member, as the converter writes it, is a depth Node reads", async () => {
      const { depthToLevel, parseCsharpReasoningDepth, levelToDepth, REASONING_LEVELS } =
        await import("../storage/reasoningDepth.js");
      const cs = readEnum(ENUM_FILES["ReasoningDepth"] ?? "", "ReasoningDepth");
      const sent = cs.members.map((m) => camelCase(m.name));

      expect([cs.asString, cs.camelCase]).toEqual([true, true]);
      for (const value of sent) {
        expect(depthToLevel(value), value).not.toBeNull();
        expect(parseCsharpReasoningDepth(value), value).toBe(value.toLowerCase());
      }
      // What Node writes back is a member C# parses (it ignores case).
      const known = sent.map((value) => value.toLowerCase());
      expect(REASONING_LEVELS.map(levelToDepth).filter((depth) => !known.includes(depth))).toEqual(
        [],
      );
    });

    it("DocumentAgentReasoning: the settings schema takes exactly the members C# writes", async () => {
      const { documentAgentSettingsSchema } =
        await import("../storage/documentAgentSettingsStorage.js");
      const cs = readEnum(ENUM_FILES["DocumentAgentReasoning"] ?? "", "DocumentAgentReasoning");

      expect([cs.asString, cs.camelCase]).toEqual([true, true]);
      expect([...documentAgentSettingsSchema.shape.reasoning.options].sort()).toEqual(
        cs.members.map((m) => camelCase(m.name)).sort(),
      );
    });

    it("Capabilities is sent as a number, as dtoToProfile reads it", () => {
      expect(readEnum(ENUM_FILES["Capabilities"] ?? "", "Capabilities").asString).toBe(false);
    });

    // Folder and room kinds are numbers Node compares with constants of its own.
    it.each([
      ["app/storage/docspaceFilesApi.ts", "AI_PROJECT_FOLDER_TYPE", "FolderType", "AiProjectRoom"],
      [
        "app/storage/docspaceFilesApi.ts",
        "FOLDER_TYPE_FORM_ROOM",
        "FolderType",
        "FillingFormsRoom",
      ],
      ["app/skills/generateSkill.ts", "INSTRUCTIONS_FOLDER_TYPE", "FolderType", "InstructionsRoom"],
      ["app/storage/docspaceFilesApi.ts", "AI_PROJECT_ROOM_TYPE", "RoomType", "AiProjectRoom"],
      ["app/storage/docspaceFilesApi.ts", "ROOM_TYPE_FORM_ROOM", "RoomType", "FillingFormsRoom"],
      ["app/storage/roomSkills.ts", "INSTRUCTIONS_ROOM_TYPE", "RoomType", "InstructionsRoom"],
      ["app/skills/generateSkill.ts", "INSTRUCTIONS_ROOM_TYPE", "RoomType", "InstructionsRoom"],
      ["app/storage/roomSkills.ts", "KNOWLEDGE_ROOM_TYPE", "RoomType", "KnowledgeRoom"],
    ])("%s: %s is %s.%s", (file, constant, enumName, member) => {
      const value = constantOf(file, constant);
      const cs = readEnum(ENUM_FILES[enumName] ?? "", enumName);

      expect(cs.asString).toBe(false);
      expect(cs.members.find((m) => m.value === value)?.name).toBe(member);
    });
  },
);
