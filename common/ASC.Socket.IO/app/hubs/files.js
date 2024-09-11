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

module.exports = (io) => {
  const logger = require("../log.js");
  const filesIO = io;

  function start(socket){
    const session = socket.handshake.session;
      if (session.system) {
        return;
      }
  
      const userId = () => {
        return socket.handshake.session?.user?.id;
      }
      const tenantId = () => {
        return socket.handshake.session?.portal?.tenantId;
      }
      
      const linkId = () => {
        return socket.handshake.session?.linkId;
      }
  
      const getRoom = (roomPart) => {
        return `${tenantId()}-${roomPart}`;
      };
  
      const connectMessage = !session.anonymous ? 
        `connect user='${userId()}' on tenant='${tenantId()}' socketId='${socket.id}'` : 
        `connect anonymous user by share key on tenant='${tenantId()}' socketId='${socket.id}'`;
  
      logger.info(connectMessage);
  
      socket.on("disconnect", (reason) => {
        const disconnectMessage = !session.anonymous ? 
          `disconnect user='${userId()}' on tenant='${tenantId()}' socketId='${socket.id}' due to ${reason}` :
          `disconnect anonymous user by share key on tenant='${tenantId()}' socketId='${socket.id}' due to ${reason}`;
  
        logger.info(disconnectMessage)
      });
  
      socket.on("subscribe", ({ roomParts, individual }) => {
        changeSubscription(roomParts, individual, subscribe);
      });
  
      socket.on("unsubscribe", ({ roomParts, individual }) => {
        changeSubscription(roomParts, individual, unsubscribe);
      });
  
      socket.on("refresh-folder", (folderId) => {
        const room = getRoom(`DIR-${folderId}`);
        logger.info(`refresh folder ${folderId} in room ${room}`);
        socket.to(room).emit("refresh-folder", folderId);
      });
  
      socket.on("restore-backup", () => {
        const room = getRoom("backup-restore");
        const sess = socket.handshake.session;
        const tenant = sess?.portal?.tenantId || "unknown";
        const user = sess?.user?.id || "unknown";
        const sessId = sess?.id;
  
        logger.info(`WS: restore backup in room ${room} session=[sessionId='sess:${sessId}' tenantId=${tenant}|${tenantId()} userId='${user}'|'${userId()}']`);
        socket.to(room).emit("restore-backup");
      });
  
      function changeSubscription(roomParts, individual, changeFunc) {
        if (!roomParts) return;
  
        changeFunc(roomParts);
  
        if (individual) {
          if (Array.isArray(roomParts)) {
            changeFunc(roomParts.map((p) => `${p}-${userId()}`));
            
            if (linkId()) {
              changeFunc(roomParts.map((p) => `${p}-${linkId()}`));
            }
            
          } else {
            changeFunc(`${roomParts}-${userId()}`);
            
            if (linkId()) {
              changeFunc(`${roomParts}-${linkId()}`);
            }
          }
        }
      }
  
      function subscribe(roomParts) {
        if (!roomParts) return;
  
        if (Array.isArray(roomParts)) {
          const rooms = roomParts.map((p) => getRoom(p));
          logger.info(`client ${socket.id} join rooms [${rooms.join(",")}]`);
          socket.join(rooms);
        } else {
          const room = getRoom(roomParts);
          logger.info(`client ${socket.id} join room ${room}`);
          socket.join(room);
        }
      }
  
      function unsubscribe(roomParts) {
        if (!roomParts) return;
  
        if (Array.isArray(roomParts)) {
          const rooms = roomParts.map((p) => getRoom(p));
          logger.info(`client ${socket.id} leave rooms [${rooms.join(",")}]`);
          socket.leave(rooms);
        } else {
          const room = getRoom(roomParts);
          logger.info(`client ${socket.id} leave room ${room}`);
          socket.leave(room);
        }
      }
  }

  function startEdit({ fileId, room } = {}) {
    logger.info(`start edit file ${fileId} in room ${room}`);
    filesIO.to(room).emit("s:start-edit-file", fileId);
  }

  function stopEdit({ fileId, room } = {}) {
    logger.info(`stop edit file ${fileId} in room ${room}`);
    filesIO.to(room).emit("s:stop-edit-file", fileId);
  }

  function modifyFolder(room, cmd, id, type, data) {
    filesIO.to(room).emit("s:modify-folder", { cmd, id, type, data });
  }

  function modifyFormRoom(room, cmd, id, type, data, isOneMember) {
    filesIO.to(room).emit("s:modify-room", { cmd, id, type, data, isOneMember });
  }

  function createForm({ id, room, data, userIds, isOneMember } = {}) {
    logger.info(`create new form ${id} in room ${room}`);
      if (userIds) {
          userIds.forEach(userId => modifyFormRoom(`${room}-${userId}`, "create-form", id, "file", data, isOneMember));
      }
      else {
          modifyFormRoom(room, "create-form", id, "file", data, isOneMember);
      }
  }

  function createFile({ id, room, data, userIds } = {}) {
    logger.info(`create new file ${id} in room ${room}`);

    if(userIds)
    {
      userIds.forEach(userId => modifyFolder(`${room}-${userId}`, "create", id, "file", data));
    }
    else
    {
      modifyFolder(room, "create", id, "file", data);
    }
  }

  function createFolder({ id, room, data, userIds } = {}) {
    logger.info(`create new folder ${id} in room ${room}`);
    if(userIds)
    {
      userIds.forEach(userId => modifyFolder(`${room}-${userId}`, "create", id, "folder", data));
    } 
    else 
    {
      modifyFolder(room, "create", id, "folder", data);
    }
  }

  function updateFile({ id, room, data, userIds } = {}) {
    logger.info(`update file ${id} in room ${room}`);
    
    if(userIds)
    {
      userIds.forEach(userId => modifyFolder(`${room}-${userId}`, "update", id, "file", data));
    }
    else
    {
      modifyFolder(room, "update", id, "file", data);
    }
  }

  function updateFolder({ id, room, data, userIds } = {}) {
    logger.info(`update folder ${id} in room ${room}`);
    modifyFolder(room, "update", id, "folder", data);

    if(userIds)
    {
      userIds.forEach(userId =>  modifyFolder(`${room}-${userId}`, "update", id, "folder", data));
    }
    else
    {
      modifyFolder(room, "update", id, "folder", data);
    }
  }

  function deleteFile({ id, room, userIds } = {}) {
    logger.info(`delete file ${id} in room ${room}`);

    if(userIds)
    {
      userIds.forEach(userId => modifyFolder(`${room}-${userId}`, "delete", id, "file"));
    }
    else
    {
      modifyFolder(room, "delete", id, "file");
    }
  }

  function deleteFolder({ id, room, userIds } = {}) {
    logger.info(`delete folder ${id} in room ${room}`);
    
    if(userIds)
    {
      userIds.forEach(userId => modifyFolder(`${room}-${userId}`, "delete", id, "folder"));
    }
    else
    {
      modifyFolder(room, "delete", id, "folder");
    }
  }

  function markAsNewFile({ fileId, count, room } = {}) {
    logger.info(`markAsNewFile ${fileId} in room ${room}:${count}`);
    filesIO.to(room).emit("s:markasnew-file", { fileId, count });
  }

  function markAsNewFiles(items = []) {
    items.forEach(markAsNewFile);
  }

  function markAsNewFolder({ folderId, userIds, room } = {}) {
    logger.info(`markAsNewFolder ${folderId}`);
    userIds.forEach(({count, owner}) =>{
      filesIO.to(`${room}-${owner}`).emit("s:markasnew-folder", { folderId, count });
    });
  }

  function markAsNewFolders(items = []) {
    items.forEach(markAsNewFolder);
  }

  function changeQuotaUsedValue({ featureId, value, room } = {}) {
    logger.info(`changeQuotaUsedValue in room ${room}`, { featureId, value });
    filesIO.to(room).emit("s:change-quota-used-value", { featureId, value });
  }

  function changeQuotaFeatureValue({ featureId, value, room } = {}) {
     logger.info(`changeQuotaFeatureValue in room ${room}`, { featureId, value });
     filesIO.to(room).emit("s:change-quota-feature-value", { featureId, value });
  }

  function changeUserQuotaFeatureValue({ customQuotaFeature,enableQuota, usedSpace, quotaLimit, userIds, room } = {}) {

    logger.info(`changeUserQuotaFeatureValue feature ${customQuotaFeature}, room ${room}`, { customQuotaFeature, enableQuota, usedSpace, quotaLimit });

    if (userIds) {
      userIds.forEach(userId => changeCustomQuota(`${room}-${userId}`, customQuotaFeature, enableQuota, usedSpace, quotaLimit));
    }
    else {
      changeCustomQuota(room, customQuotaFeature,enableQuota, usedSpace, quotaLimit);
    }
  }

  function changeCustomQuota(room, customQuotaFeature, enableQuota, usedSpace, quotaLimit) {
      
      if (customQuotaFeature === "tenant_custom_quota") {
          filesIO.to(room).emit("s:change-user-quota-used-value", { customQuotaFeature, enableQuota, quota: quotaLimit });
      } else {
          filesIO.to(room).emit("s:change-user-quota-used-value", { customQuotaFeature, usedSpace, quotaLimit });
      }
  }

  function changeInvitationLimitValue({ value, room } = {}) {
    logger.info(`changed user invitation limit in room ${room}, value ${value}`);
    filesIO.to(room).emit("s:change-invitation-limit-value", value);
  }

  function updateHistory({ room, id, type } = {}) {
    logger.info(`update ${type} history ${id} in room ${room}`);
    filesIO.to(room).emit("s:update-history", { id, type });
  }

  function logoutSession({ room, loginEventId } = {}) {
    logger.info(`logout user ${room} session ${loginEventId}`);
    filesIO.to(room).emit("s:logout-session", loginEventId);
  }

  return {
    start,
    startEdit,
    stopEdit,
    createFile,
    createForm,
    createFolder,
    deleteFile,
    deleteFolder,
    updateFile,
    updateFolder,
    changeQuotaUsedValue,
    changeQuotaFeatureValue,
    changeUserQuotaFeatureValue,
    markAsNewFiles,
    markAsNewFolders,
    changeInvitationLimitValue,
    updateHistory,
    logoutSession
  };
};
