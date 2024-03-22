﻿module.exports = (io) => {
  const logger = require("../log.js");
  const moment = require("moment");
  const filesIO = io; //TODO: Restore .of("/files");

  filesIO.on("connection", (socket) => {
    const session = socket.handshake.session;

    if (!session) {
      logger.error("empty session");
      return;
    }

    if (session.system) {
      logger.info(`connect system as socketId='${socket.id}'`);

      socket.on("ping", (date) => {
        logger.info(`ping (client ${socket.id}) at ${date}`);
        filesIO.to(socket.id).emit("pong", moment.utc());
      });

      socket.on("disconnect", (reason) => {
        logger.info(
          `disconnect system as socketId='${socket.id}' due to ${reason}`
        );
      });

      return;
    }

    if (!session.user && !session.anonymous) {
      logger.error("invalid session: unknown user");
      return;
    }

    if (!session.portal) {
      logger.error("invalid session: unknown portal");
      return;
    }

    const userId = session?.user?.id;
    const tenantId = session?.portal?.tenantId;
    const linkId = session?.linkId;

    getRoom = (roomPart) => {
      return `${tenantId}-${roomPart}`;
    };

    const connectMessage = !session.anonymous ? 
      `connect user='${userId}' on tenant='${tenantId}' socketId='${socket.id}'` : 
      `connect anonymous user by share key on tenant='${tenantId}' socketId='${socket.id}'`;

    logger.info(connectMessage);

    socket.on("disconnect", (reason) => {
      const disconnectMessage = !session.anonymous ? 
        `disconnect user='${userId}' on tenant='${tenantId}' socketId='${socket.id}' due to ${reason}` :
        `disconnect anonymous user by share key on tenant='${tenantId}' socketId='${socket.id}' due to ${reason}`;

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

      logger.info(`WS: restore backup in room ${room} session=[sessionId='sess:${sessId}' tenantId=${tenant}|${tenantId} userId='${user}'|'${userId}']`);
      socket.to(room).emit("restore-backup");
    });

    function changeSubscription(roomParts, individual, changeFunc) {
      if (!roomParts) return;

      changeFunc(roomParts);

      if (individual) {
        if (Array.isArray(roomParts)) {
          changeFunc(roomParts.map((p) => `${p}-${userId}`));
          
          if (linkId) {
            changeFunc(roomParts.map((p) => `${p}-${linkId}`));
          }
          
        } else {
          changeFunc(`${roomParts}-${userId}`);
          
          if (linkId) {
            changeFunc(`${roomParts}-${linkId}`);
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
  });

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
      
      if (customQuotaFeature == "tenant_custom_quota") {
          filesIO.to(room).emit("s:change-user-quota-used-value", { customQuotaFeature, enableQuota, quota: quotaLimit });
      } else {
          filesIO.to(room).emit("s:change-user-quota-used-value", { customQuotaFeature, usedSpace, quotaLimit });
      }
  }

  function changeInvitationLimitValue({ value, room } = {}) {
    logger.info(`changed user invitation limit in room ${room}, value ${value}`);
    filesIO.to(room).emit("s:change-invitation-limit-value", value);
 }

  return {
    startEdit,
    stopEdit,
    createFile,
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
    changeInvitationLimitValue
  };
};
