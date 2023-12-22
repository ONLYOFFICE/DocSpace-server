module.exports = (io) => {
    const logger = require("../log.js");
    const onlineIO = io.of("/onlineusers");
    const onlineUsers = [];
  
    onlineIO.on("connection", (socket) => {
      const session = socket.handshake.session;
  
      if (!session) {
        logger.error("empty session");
        return;
      }
  
  
      const userId = session?.user?.id;
      const tenantId = session?.portal?.tenantId;
  
      socket.on("disconnect", (reason) => {

      });
  
      socket.on("enter", ({ roomPart}) => {
        const room = getRoom(roomPart);
        filesIO.to(room).emit("s:enter-in-room", { roomPart, userId });
      });

      socket.on("leave", ({ roomPart }) => {
        const room = getRoom(roomPart);
        filesIO.to(room).emit("s:leave-in-room", { roomPart, userId });
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
  