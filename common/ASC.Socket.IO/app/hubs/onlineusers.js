const redis = require("redis");
const config = require("../../config");
var uap = require('ua-parser-js');
const { json } = require("express/lib/response");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io;
    const redisOptions = config.get("Redis");
    
    const client = await redis.createClient(redisOptions).connect();
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
      
      var user = await client.get(`portals-${tenantId}-${userId}`);
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
        user = deserialize(user); 
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
        onlineIO.to(`p-${tenantId}`).emit("enter-in-portal", { user });
      }
      
      await client.set(`portals-${tenantId}-${userId}`, serialize(user), 'EX', 60 * 60 * 24 * 3);

      socket.on("disconnect", async (reason) => {
        var user = await client.get(`portals-${tenantId}-${userId}`);
        if (user) 
        {
          user = deserialize(user); 
          user.sessions.delete(id);
          id = -1;

          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            user.date = Date.now();
            await client.set(`portals-${tenantId}-${userId}`, serialize(user));
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal", { userId });
          }
          else
          { 
            await client.set(`portals-${tenantId}-${userId}`, serialize(user), 'EX', 60 * 60 * 24 * 3);
          }
        }

        if(!_roomId)
        {
          return;
        }

        /*****************************/

        user = await client.get(`rooms-${_roomId}-${userId}`);
        if (user) 
        {
          user = deserialize(user); 
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            user.date = Date.now();
            await client.set(`rooms-${_roomId}-${userId}`, serialize(user));
            onlineIO.to(_roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            await client.set(`rooms-${_roomId}-${userId}`, serialize(user),'EX', 60 * 60 * 24 * 3);
          }
        }
      });
  
      socket.on("enter", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
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
          user = deserialize(user); 
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
          onlineIO.to(roomId).emit("enter-in-room", { user });
        }
        await client.set(`rooms-${roomId}-${userId}`, serialize(user));
      });

      socket.on("changeStatus", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          return;
        }
        
        user = deserialize(user);
        user.sessions[idInRoom].status = status;
        await client.set(`rooms-${roomId}-${userId}`, serialize(user), 'EX', 60 * 60 * 24 * 3);
        onlineIO.to(roomId).emit("user-status", { user });
      });

      socket.on("removeStatus", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          return;
        }
        
        user = deserialize(user);

        user.sessions[idInRoom].status = null;
        await client.set(`rooms-${roomId}-${userId}`, serialize(user), 'EX', 60 * 60 * 24 * 3);
        onlineIO.to(roomId).emit("user-status", { user });
      });

      socket.on("getSessionsInRoom", async ({ roomPart, userIds }) => {
        const roomId = getRoom(roomPart);
        var users = [];
        for (const userId of userIds) {
          var user = await client.get(`rooms-${roomId}-${userId}`);
          if(user!= null)
          {
            user = deserialize(user);
            users.push(user);
          }
        }
        onlineIO.to(socket.id).emit("statuses-in-room", { users });
      });

      socket.on("getSessionsInPortal", async ({ userIds }) => {
        var users = [];
        for (const userId of userIds) 
        {
          var user = await client.get(`portals-${tenantId}-${userId}`);
          if(user != null)
          {
            user = deserialize(user)
            users.push(user);
          }
        }
        onlineIO.to(socket.id).emit("statuses-in-room", { users });
      });

      socket.on("leave", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (user) 
        {
          user = deserialize(user); 
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.length <= 0)
          {
            await client.del(`rooms-${roomId}-${userId}`);
            _roomId = undefined;
            onlineIO.to(roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            await client.set(`rooms-${roomId}-${userId}`, serialize(user), 'EX', 60 * 60 * 24 * 3);
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
        user.sessions = JSON.stringify([... user.sessions]);
        let string = JSON.stringify(user);
        return string;
      }

      function deserialize(string){
        var user = JSON.parse(string);
        user.sessions = new Map(JSON.parse(user.sessions));
        return user;
      }
    });
};
  