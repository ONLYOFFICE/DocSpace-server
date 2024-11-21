// (c) Copyright Ascensio System SIA 2009-2024
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

module.exports = (files) => {
  const router = require("express").Router();

  router.post("/start-edit", (req, res) => {
    files.startEdit(req.body);
    res.end();
  });

  router.post("/stop-edit", (req, res) => {
    files.stopEdit(req.body);
    res.end();
  });

  router.post("/create-file", (req, res) => {
    files.createFile(req.body);
    res.end();
  });

  router.post("/create-form", (req, res) => {
    files.createForm(req.body);
    res.end();
  });

  router.post("/create-folder", (req, res) => {
    files.createFolder(req.body);
    res.end();
  });

  router.post("/update-file", (req, res) => {
    files.updateFile(req.body);
    res.end();
  });

  router.post("/update-folder", (req, res) => {
    files.updateFolder(req.body);
    res.end();
  });

  router.post("/delete-file", (req, res) => {
    files.deleteFile(req.body);
    res.end();
  });

  router.post("/delete-folder", (req, res) => {
    files.deleteFolder(req.body);
    res.end();
  });

  router.post("/mark-as-new-file", (req, res) => {
    files.markAsNewFiles(req.body);
    res.end();
  });

  router.post("/mark-as-new-folder", (req, res) => {
    files.markAsNewFolders(req.body);
    res.end();
  });

  router.post("/change-quota-used-value", (req, res) => {
    files.changeQuotaUsedValue(req.body);
    res.end();
  });

  router.post("/change-quota-feature-value", (req, res) => {
    files.changeQuotaFeatureValue(req.body);
    res.end();
  });

  router.post("/change-user-quota-used-value", (req, res) => {
    files.changeUserQuotaFeatureValue(req.body);
    res.end();
  });

  router.post("/change-invitation-limit-value", (req, res) => {
    files.changeInvitationLimitValue(req.body);
    res.end();
  });
  
  router.post("/update-history", (req, res) => {
    files.updateHistory(req.body);
    res.end();
  });

  router.post("/logout-session", (req, res) => {
    files.logoutSession(req.body);
    res.end();
  });

  router.post("/add-user", (req, res) => {
    files.addUser(req.body);
    res.end();
  });

  router.post("/update-user", (req, res) => {
    files.updateUser(req.body);
    res.end();
  });

  router.post("/delete-user", (req, res) => {
    files.deleteUser(req.body);
    res.end();
  });

  router.post("/add-group", (req, res) => {
    files.addGroup(req.body);
    res.end();
  });

  router.post("/update-group", (req, res) => {
    files.updateGroup(req.body);
    res.end();
  });

  router.post("/delete-group", (req, res) => {
    files.deleteGroup(req.body);
    res.end();
  });

  router.post("/add-guest", (req, res) => {
    files.addGuest(req.body);
    res.end();
  });

  router.post("/update-guest", (req, res) => {
    files.updateGuest(req.body);
    res.end();
  });

  router.post("/delete-guest", (req, res) => {
    files.deleteGuest(req.body);
    res.end();
  });

  return router;
};