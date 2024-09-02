module.exports = (io) => {
    const logger = require("../log.js");
    const moment = require("moment");
    const systemIO = io;

    systemIO.on("connection", (socket) => 
    {
        const session = socket.handshake.session;
        if (session.system) 
        {
            logger.info(`connect system as socketId='${socket.id}'`);
      
            socket.on("ping", (date) => {
              logger.info(`ping (client ${socket.id}) at ${date}`);
              systemIO.to(socket.id).emit("pong", moment.utc());
            });
      
            socket.on("disconnect", (reason) => {
              logger.info(
                `disconnect system as socketId='${socket.id}' due to ${reason}`
              );
            });
        }
    });
}