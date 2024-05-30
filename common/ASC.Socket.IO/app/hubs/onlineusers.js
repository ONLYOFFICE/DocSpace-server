var uap = require('ua-parser-js');
const { json } = require("express/lib/response");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io;
    const portalUsers =[];
    const roomUsers =[];

    onlineIO.on("connection", async (socket) => {
      const session = socket.handshake.session;
      if (session.system) {
        return;
      }
      const ipAddress = getCleanIP(socket.handshake.headers['x-forwarded-for']);
      const parser = uap(socket.request.headers['user-agent']);
      const operationSystem = parser.os.version !== undefined ?  `${parser.os.name} ${parser.os.version}` : `${parser.os.name}`;  
      const browserVersion = parser.browser.version ? parser.browser.version : '';
  
      const userId = session?.user?.id;
      const userName = (session?.user?.userName || "").toLowerCase();
      const tenantId = session?.portal?.tenantId;
      var _roomId;
      let id;
      let idInRoom;
      
      var user = getUser(portalUsers, userId, tenantId);
      if (!user) 
      {
        var sessions = new Map();
        sessions.set(
          1,
          {
            platform: operationSystem,
            browser: parser.browser.name + " " + browserVersion,
            ip: ipAddress
          });
        id = 1;
        user = {
          id: userId,
          displayName: userName,
          page: session?.user?.profileUrl,
          sessions: sessions,
          status: "online"
        };
      }
      else
      {
        user.status = "online";

        var keys = user.sessions.keys();
        id = keys.length == 0 ? 1 : Array.from(keys).pop() + 1;
        user.sessions.set(
          id,
          {
            platform: operationSystem,
            browser: parser.browser.name + " " + browserVersion,
            ip: ipAddress
          });
      }
      if(user.sessions.size == 1)
      {
        var stringUser = serialize(user);
        onlineIO.to(`p-${tenantId}`).emit("enter-in-portal", { stringUser });
      }

      updateUser(portalUsers, user, userId, tenantId);

      socket.on("disconnect", async (reason) => {
        var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          user.sessions.delete(id);
          id = -1;

          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            user.date = new Date().toString();
            updateUser(portalUsers, user, userId, tenantId);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal", { userId });
          }
          else
          { 
            updateUser(portalUsers, user, userId, tenant);
          }
        }

        if(!_roomId)
        {
          return;
        }

        /*****************************/

        var user = getUser(roomUsers, userId, _roomId);
        if (user) 
        {
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            user.date = new Date().toString();
            updateUser(roomUsers, user, userId, _roomId);
            onlineIO.to(_roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            updateUser(roomUsers, user, userId, _roomId);
          }
        }
      });
  
      socket.on("enter", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          var sessions = new Map();
          sessions.set(
            1,
            {
              platform: operationSystem,
              browser: parser.browser.name + " " + browserVersion,
              ip: ipAddress,
              status: status
            });
          idInRoom = 1;
          user = {
            id: userId,
            displayName: userName,
            page: session?.user?.profileUrl,
            sessions: sessions,
            status: "online"
          };
        }
        else
        {
          user.status = "online";
          var keys = user.sessions.keys();
          idInRoom = keys.length == 0 ? 1 : Array.from(keys).pop() + 1;
          sessions.set(
            idInRoom,
            {
              platform: operationSystem,
              browser: parser.browser.name + " " + browserVersion,
              ip: ipAddress,
              status: status
            });
        }
        _roomId = roomId
        if(user.sessions.size == 1)
        {
          var stringUser = serialize(user);
          onlineIO.to(roomId).emit("enter-in-room", { stringUser });
        }
        updateUser(roomUsers, user, userId, roomId);
      });

      socket.on("changeStatus", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          return;
        }
        
        user.sessions[idInRoom].status = status;
        updateUser(roomUsers, user, userId, roomId);
        var stringUser = serialize(user);
        onlineIO.to(roomId).emit("user-status", { stringUser });
      });

      socket.on("removeStatus", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          return;
        }

        user.sessions[idInRoom].status = null;
        updateUser(roomUsers, user, userId, roomId);
        var stringUser = serialize(user);
        onlineIO.to(roomId).emit("user-status", { stringUser });
      });

      socket.on("getSessionsInRoom", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var users = [];
        Object.values(roomUsers[roomId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
        onlineIO.to(socket.id).emit("statuses-in-room", { users });
      });

      socket.on("getSessionsInPortal", async () => {
        var users = [];
        Object.values(portalUsers[tenantId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
        onlineIO.to(socket.id).emit("statuses-in-portal", { users });
      });

      socket.on("leave", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (user) 
        {
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.length <= 0)
          {
            updateUser(roomUsers, user, userId, roomId);
            _roomId = undefined;
            onlineIO.to(roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            updateUser(roomUsers, user, userId, roomId);
            _roomId = undefined;
          }
        }
      });

      socket.on("subscribeToRoom", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} subscribe room ${room}`);
        socket.join(room);
      });
  
      socket.on("unsubscribeToRoom", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} unsubscribe room ${room}`);
        socket.leave(room);
      });

      socket.on("subscribeToPortal", () => {
        logger.info(`client ${socket.id} subscribe portal ${tenantId}`);
        socket.join(`p-${tenantId}`);
      });
  
      socket.on("unsubscribeToPortal", () => {
        logger.info(`client ${socket.id} unsubscribe room ${tenantId}`);
        socket.leave(`p-${tenantId}`);
      });

      getRoom = (roomPart) => {
        return `${tenantId}-${roomPart}`;
      };

      function getCleanIP (ipAddress) {
        if(typeof(ipAddress) == "undefined"){
          return "127.0.0.1";
        }
              const indexOfColon = ipAddress.indexOf(':');
              if (indexOfColon === -1){
                  return ipAddress;
              } else if (indexOfColon > 3){
                  return ipAddress.substring(0, indexOfColon);
              }
              else {
                  return "127.0.0.1";
              }
      }

      function serialize(user){
        var sessions = user.sessions;
        user.sessions = JSON.stringify([... user.sessions]);
        let string = JSON.stringify(user);
        user.sessions = sessions;
        return string;
      }

      function updateUser(list, user, userId, id){
        if(!list[id])
        {
          list[id] = [];
        }
        list[id][userId] = user;
      }

      function getUser(list, userId, id){
        if(!list[id])
        {
          list[id] = [];
          return null;
        }
        return list[id][userId];
      }
    });
};
  