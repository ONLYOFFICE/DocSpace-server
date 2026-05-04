// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { AssignmentsEngine } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";

const engine = new AssignmentsEngine({ storage });

export const assignmentsController = {
  resolveForAction: asyncHandler(async (req, res) => {
    const actionType = req.query.actionType;
    if (!actionType) {
      res.json(null);
      return;
    }
    const result = await engine.resolveForAction(actionType);
    res.json(result);
  }),

  tryResolveForAction: asyncHandler(async (req, res) => {
    const actionType = req.query.actionType;
    if (!actionType) {
      res.json(null);
      return;
    }
    const result = await engine.tryResolveForAction(actionType);
    res.json(result);
  }),

  assign: asyncHandler(async (req, res) => {
    const { actionType, profileId } = req.body ?? {};
    const result = await engine.assign(actionType, profileId);
    res.json(result);
  }),

  unassign: asyncHandler(async (req, res) => {
    const actionType = req.body?.actionType ?? req.query.actionType;
    await engine.unassign(actionType);
    res.json({ success: true });
  }),

  bulkAssign: asyncHandler(async (req, res) => {
    const result = await engine.bulkAssign(req.body ?? {});
    res.json(result);
  }),

  getAssignment: asyncHandler(async (req, res) => {
    const actionType = req.query.actionType;
    if (!actionType) {
      res.json({ profileId: null });
      return;
    }
    const result = await engine.getAssignment(actionType);
    res.json({ profileId: result });
  }),

  getAllAssignments: asyncHandler(async (req, res) => {
    const result = await engine.getAllAssignments();
    res.json(result);
  }),

  cascadeProfileDelete: asyncHandler(async (req, res) => {
    const profileId = req.body?.profileId ?? req.query.profileId;
    await engine.cascadeProfileDelete(profileId);
    res.json({ success: true });
  }),
};
