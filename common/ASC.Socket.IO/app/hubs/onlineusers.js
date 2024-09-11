var uap = require('ua-parser-js');
const { json } = require("express/lib/response");
const config = require("../../config");
const redis = require("redis");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io;
    const portalUsers =[];
    const roomUsers =[];
    const editFiles =[];
    const redisOptions = config.get("Redis");
    var redisClient = redis.createClient(redisOptions);
    await redisClient.connect();

    onlineIO.on("connection", async (socket) => {
      if (socket.handshake.session.system) {
        return;
      }
      const ipAddress = getCleanIP(socket.handshake.headers['x-forwarded-for']);
      const parser = uap(socket.request.headers['user-agent']);
      const operationSystem = parser.os.version !== undefined ?  `${parser.os.name} ${parser.os.version}` : `${parser.os.name}`;  
      const browserVersion = parser.browser.version ? parser.browser.version : '';
  
      const userId = socket.handshake.session?.user?.id;
      const userName = (socket.handshake.session?.user?.userName || "").toLowerCase();
      const tenantId = socket.handshake.session?.portal?.tenantId;
      let id;
      let idInRoom = -1;
      let roomId = -1;
      let file;
      let sessionId = socket.handshake.session?.user?.connection;
      
      id = await EnterAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal");

      if(socket.handshake.session.file)
      {
        roomId = `${tenantId}-${socket.handshake.session.file.roomId}`;
        file = socket.handshake.session.file.title;
        if(!editFiles[roomId])
        {
          editFiles[roomId] = [];
        }
        if(!editFiles[roomId][userId])
        {
          editFiles[roomId][userId] = [];
        }
        var user = getUser(roomUsers, userId, roomId);
        if(editFiles[roomId][userId].length == 0 && user && user.status == "online")
        {
          onlineIO.to(roomId).emit(`start-edit-file-in-room`, {userId, file} );
        }
        editFiles[roomId][userId].push(file);
        idInRoom = await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
      }
      socket.on("disconnect", async (reason) => {
        await LeaveAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal", id);
        await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", idInRoom, true);
        if(file)
        {
          var index = editFiles[roomId][userId].indexOf(file);
          editFiles[roomId][userId].splice(index, 1);
          var user = getUser(roomUsers, userId, roomId);
          if(!editFiles[roomId][userId].includes(file) && user.status == "online")
          {
            onlineIO.to(roomId).emit(`stop-edit-file-in-room`, {userId, file} );
            if(editFiles[roomId][userId].length > 0)
            {
              file = editFiles[roomId][userId][0];
              onlineIO.to(roomId).emit(`start-edit-file-in-room`, {userId, file} );
            }
          }
        }
        id = -1;
        idInRoom = -1;
        sessionId = -1;
        roomId = -1;
        file = null;
      });

      socket.on("getSessionsInPortal", async () => {
        var users = [];
        if(portalUsers[tenantId])
        {
          Object.values(portalUsers[tenantId]).forEach(function(entry) {
            users.push(serialize(entry));
          });
        }
        onlineIO.to(socket.id).emit("statuses-in-portal",  users );
      });
      
      socket.on("subscribeToPortal", () => {
        logger.info(`client ${socket.id} subscribe portal ${tenantId}`);
        socket.join(`p-${tenantId}`);
      });
  
      socket.on("unsubscribeToPortal", () => {
        logger.info(`client ${socket.id} unsubscribe portal ${tenantId}`);
        socket.leave(`p-${tenantId}`);
      });
     /******************************* */
      socket.on("enterInRoom", async(roomPart)=>{
        roomId = getRoom(roomPart);
        idInRoom = await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
    });

    socket.on("leaveRoom", async()=>{
      await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", idInRoom, true);
      idInRoom = -1;
  });

    socket.on("getSessionsInRoom", async (roomPart) => {
      var users = [];
      roomId = getRoom(roomPart);
      if(roomUsers[roomId])
      {
        Object.values(roomUsers[roomId]).forEach(function(entry) {
          users.push(serialize(entry, true));
        });
      }
      onlineIO.to(socket.id).emit("statuses-in-room",  users );
    });

    socket.on("subscribeToRoom", (roomPart) => {
      roomId = getRoom(roomPart);
      logger.info(`client ${socket.id} subscribe room ${roomId}`);
      socket.join(roomId);
    });

    socket.on("unsubscribeToRoom", () => {
      logger.info(`client ${socket.id} unsubscribe room ${roomId}`);
      socket.leave(roomId);
      roomId = -1;
    });

      var getRoom = (obj) => {
        return `${tenantId}-${obj.roomPart}`;
      };

      async function LeaveAsync(usersList, key, redisKey, socketKey, socketDest, sessionId, isRoom = false) 
      {
        if(sessionId == -1 ){
          return;
        }
        var user = getUser(usersList, userId, key);
        if (user) 
        {
          var session = user.sessions.get(sessionId);
          user.sessions.delete(sessionId);
          var array = Array.from(user.sessions, ([name, value]) => {
            return value;
          });
          if(!array.find(e=> e.id == session.id))
          {
            user.offlineSessions.set(session.id,
              {
                id: session.id,
                platform: session.platform,
                browser: session.browser,
                ip: session.ip,
                status: "offline",
                date: new Date().toString()
            });
            
            await redisClient.set(redisKey, JSON.stringify(Array.from(user.offlineSessions)));
          }
          var date = new Date().toString();
            if(user.sessions.size <= 0)
            {
              user.status = "offline";
              onlineIO.to(socketKey).emit(`leave-in-${socketDest}`, {userId, date} );
            }
            else
            {
              if(isRoom || array.find(e=> e.browser == session.browser && e.platform == session.platform && e.ip == session.ip))
              {
                return;
              }
              onlineIO.to(socketKey).emit(`leave-session-in-${socketDest}`, {userId, sessionId, date} );
            }
        }
      }

      async function EnterAsync(usersList, key, redisKey, socketKey, socketDest, isRoom = false) {
        var user = getUser(usersList, userId, key);
        var session;
        var id = 0;
        if (!user) 
        {
          var sessions = new Map();
          sessions.set(
            1,
            {
              id: sessionId,
              platform: operationSystem,
              browser: parser.browser.name + " " + browserVersion,
              ip: ipAddress,
              status: "online"
            });
          id = 1;

          var offSess = await redisClient.get(redisKey);
          if(offSess && offSess != '{}')
          {
            offSess = new Map(JSON.parse(offSess).map(i => [i[0], i[1]]));
          }
          else
          {
            offSess = new Map();
          }
          user = {
            id: userId,
            displayName: userName,
            page: socket.handshake.session?.user?.profileUrl,
            sessions: sessions,
            status: "online",
            offlineSessions: offSess
          };
        }
        else
        {
          user.status = "online";

          var keys = Array.from(user.sessions.keys());
          id = keys.length == 0 ? 1 : keys[keys.length - 1] + 1;
          session = {
            id: sessionId,
            platform: operationSystem,
            browser: parser.browser.name + " " + browserVersion,
            ip: ipAddress,
            status:"online"
          };
          user.sessions.set(
            id,
            session
          );
        }
        
        user.offlineSessions.delete(sessionId);
        if(user.offlineSessions.size != 0)
        {
          await redisClient.set(redisKey, JSON.stringify(Array.from(user.offlineSessions)));
        }
        else{
          await redisClient.del(redisKey);
        }
        if(user.sessions.size == 1)
        {
          var stringUser = serialize(user, isRoom);
          addUser(usersList, user, userId, key);
          onlineIO.to(socketKey).emit(`enter-in-${socketDest}`, stringUser );
        }
        else
        {
          if(isRoom)
          {
            return id;
          }
          var array = Array.from(user.sessions, ([name, value]) => {
            return value;
          });
          if(array.filter(e=> e.browser == session.browser && e.platform == session.platform && e.ip == session.ip).size != 1)
          {
            return id;
          }
          onlineIO.to(socketKey).emit(`enter-session-in-${socketDest}`, {userId, session} );
        }

        return id;
      }

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

      function serialize(user, isRoom)
      {
        var serUser = {
          userId: user.id,
          page: user.page,
          status: user.status
        };
        if(isRoom && editFiles[roomId] && editFiles[roomId][user.id] && editFiles[roomId][user.id].length != 0)
        {
          serUser.file = editFiles[roomId][user.id][0];
          serUser.status = "edit";
        }
        else
        {  
          serUser.sessions = Array.from(user.sessions, ([name, value]) => {
            return value;
          });
          serUser.sessions = serUser.sessions.concat(Array.from(user.offlineSessions, ([name, value]) => {
            return value;
          }));
        }
        return serUser;
      }
    });

    async function leaveSessionInPortal({id, userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          var array = Array.from(user.sessions, ([name, value]) => {
            value.innerId = name;
            return value;
          });
          var sessions = array.filter(e=> e.id == id);

          Object.values(sessions).forEach(function(entry) {
            user.sessions.delete(entry.innerId);
          });

          user.offlineSessions.delete(id);
          if(user.offlineSessions.size != 0)
          {
            await redisClient.set(userId, JSON.stringify(Array.from(user.offlineSessions)));
          }
          else{
            await redisClient.del(userId);
          }
          var date = new Date().toString();
          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            var session = array.find(e=> e.id == id);
            if(session)
            {
              var sessionId = session.Id;
              onlineIO.to(`p-${tenantId}`).emit("leave-session-in-portal",  {userId, sessionId, date} );
            }
          }
        }
    }

    function leaveInPortal({userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          user.offlineSessions = new Map();
          user.sessions = new Map();

          var date = new Date().toString();
          user.status = "offline";
          onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
        }
    }

    async function leaveExceptThisInPortal({id, userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          var array = Array.from(user.sessions, ([name, value]) => {
            value.innerId = name;
            return value;
          });
          var sessions = array.filter(e=> e.id != id);

          var setIds= new Set();
          Object.values(sessions).forEach(function(entry) {
            user.sessions.delete(entry.innerId);
            setIds.add(entry.id);
          });

          array = Array.from(user.offlineSessions, ([name, value]) => {
            return value;
          });
          Object.values(array.filter(e=> e.id != id)).forEach(function(entry) {
            user.offlineSessions.delete(entry.id);
          });
          if(user.offlineSessions.size != 0)
          {
            await redisClient.set(userId, JSON.stringify(Array.from(user.offlineSessions)));
          }
          else{
            await redisClient.del(userId);
          }
          var date = new Date().toString();
          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            Object.values(setIds).forEach(function(entry) {
              onlineIO.to(`p-${tenantId}`).emit("leave-session-in-portal",  {userId, entry, date} );
            });
          }
        }
    }

    function getUser(list, userId, id){
      if(!list[id])
      {
        list[id] = [];
        return null;
      }
      return list[id][userId];
    }

    function addUser(list, user, userId, id){
      if(!list[id])
      {
        list[id] = [];
      }
      list[id][userId] = user;
    }
    
    return {
      leaveSessionInPortal,
      leaveInPortal,
      leaveExceptThisInPortal
    };
};
  