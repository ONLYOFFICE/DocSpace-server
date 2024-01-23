const redis = require("redis");
const config = require("../../config");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io.of("/onlineusers");
    const redisOptions = config.get("Redis");
    console.log(redisOptions);
    const client = await redis.createClient(redisOptions).connect();
    onlineIO.on("connection", async (socket) => {
      const session = socket.handshake.session;

      if (!session) {
        logger.error("empty session");
        return;
      }
  
      const userId = session?.user?.id;
      const tenantId = session?.portal?.tenantId;
      var _roomId;

        var user = await client.get(`portals-${tenantId}-${userId}`);
        if (!user) 
        {
          user = {count: 1};
        }
        else
        {
          user = JSON.parse(user); 
          user.count++;
        }
        await client.set(`portals-${tenantId}-${userId}`, JSON.stringify(user));
        if(user.count == 1)
        {
          onlineIO.to(`p-${tenantId}`).emit("enter-in-portal", { userId });
        }

      socket.on("disconnect", async (reason) => {
        var user = await client.get(`portals-${tenantId}-${userId}`);
        if (user) 
        {
          user = JSON.parse(user); 
          user.count--;

          if(user.count <= 0)
          {
            await client.del(`portals-${tenantId}-${userId}`);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal", { userId });
          }
          else
          { 
            await client.set(`portals-${tenantId}-${userId}`, JSON.stringify(user));
          }
        }

        if(!_roomId)
        {
          return;
        }
        user = await client.get(`rooms-${_roomId}-${userId}`);
        if (user) 
        {
          user = JSON.parse(user); 
          user.count--;

          if(user.count <= 0)
          {
            await client.del(`rooms-${_roomId}-${userId}`);
            onlineIO.to(_roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            await client.set(`rooms-${_roomId}-${userId}`, JSON.stringify(user));
          }
        }
      });
  
      socket.on("enter", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          user = {statuses: {}, count: 1};
        }
        else
        {
          user = JSON.parse(user); 
          user.count++;
        }
        await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));
        _roomId = roomId
        if(user.count == 1)
        {
          onlineIO.to(roomId).emit("enter-in-room", { userId });
        }
      });

      socket.on("addStatus", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          return;
        }
        
        user = JSON.parse(user);
        user.statuses.push(status); 
        await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));
        onlineIO.to(roomId).emit("user-status", { userId, status });
      });

      socket.on("removeStatus", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          return;
        }
        
        user = JSON.parse(user);
        
        const start = user.statuses.indexOf(status);
        if(start > 0)
        {
          var nowStatus = user.statuses.pop();
          user.statuses.splice(start, 1);
          await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));

          var newStatus = user.statuses.pop();
          if(nowStatus != newStatus)
          {
            onlineIO.to(roomId).emit("user-status", { userId, newStatus });
          }
        }
      });

      socket.on("getStatusesInRoom", async ({ roomPart, userIds }) => {
        const roomId = getRoom(roomPart);
        var users = [];
        for (const userId of userIds) {
          var user = await client.get(`rooms-${roomId}-${userId}`);
          if(user!= null)
          {
            user = JSON.parse(user);
            users.push({userId, status: user.status.pop()});
          }
        }
        onlineIO.to(socket.id).emit("statuses-in-room", { users });
      });

      socket.on("getStatusesInPortal", async ({ userIds }) => {
        var users = [];
        for (const userId of userIds) 
        {
          var user = await client.get(`portals-${tenantId}-${userId}`);
          if(user != null)
          {
            users.push({userId});
          }
        }
        onlineIO.to(socket.id).emit("statuses-in-room", { users });
      });

      socket.on("leave", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (user) 
        {
          user = JSON.parse(user); 
          user.count--;

          if(user.count <= 0)
          {
            await client.del(`rooms-${roomId}-${userId}`);
            _roomId = undefined;
            onlineIO.to(roomId).emit("leave-in-room", { userId });
          }
          else
          { 
            await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));
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
    });
};
  