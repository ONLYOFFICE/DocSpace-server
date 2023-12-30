const redis = require("redis");
const config = require("../../config");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io.of("/onlineusers");

    const client = await redis.createClient(config).connect();

    onlineIO.on("connection", async (socket) => {
      const session = socket.handshake.session;

      if (!session) {
        logger.error("empty session");
        return;
      }
  
      const userId = session?.user?.id;
      const tenantId = session?.portal?.tenantId;
      var _roomId;

      socket.on("disconnect", async (reason) => {
        if(!_roomId){
          return;
        }
        var status = await client.get(`rooms-${_roomId}-${userId}`);
        if (!status) 
        {
          status = {statuses: {}, count: 1};
        }
        else
        {
          status = JSON.parse(status); 
          status.count--;
        }
        
        if(status.count <= 0)
        {
          await client.del(`rooms-${_roomId}-${userId}`);
        }
        else
        { 
          await client.set(`rooms-${_roomId}-${userId}`, JSON.stringify(status));
        }
        onlineIO.to(_roomId).emit("s:leave-in-room", { userId });
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
        onlineIO.to(roomId).emit("enter-in-room", { userId });
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
          user.statuses.splice(start, 1);
          await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));

          var newStatus = user.statuses.pop();
          onlineIO.to(roomId).emit("user-status", { userId, newStatus });
        }
      });

      socket.on("getStatuses", async ({ roomPart, userIds }) => {
        const roomId = getRoom(roomPart);
        var users = [];
        for (const userId of userIds) {
          var user = await client.get(`rooms-${roomId}-${userId}`);
          users.push({userIds, status: user.status.pop()});
        }
        onlineIO.emit("statuses", { users });
      });

      socket.on("leave", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = await client.get(`rooms-${roomId}-${userId}`);
        if (!user) 
        {
          user = {statuses: {}, count: 1};
        }
        else
        {
          user = JSON.parse(user); 
          user.count--;
        }
        
        if(user.count <= 0)
        {
          await client.del(`rooms-${roomId}-${userId}`);
        }
        else
        { 
          await client.set(`rooms-${roomId}-${userId}`, JSON.stringify(user));
        }
        _roomId = undefined;
        onlineIO.to(roomId).emit("leave-in-room", { userId });
      });

      socket.on("subscribe", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} subscribe room ${room}`);
        socket.join(room);
      });
  
      socket.on("unsubscribe", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} unsubscribe room ${room}`);
        socket.leave(room);
      });

      getRoom = (roomPart) => {
        return `${tenantId}-${roomPart}`;
      };
    });
};
  