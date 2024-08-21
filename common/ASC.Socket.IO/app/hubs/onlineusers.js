var uap = require('ua-parser-js');
const { json } = require("express/lib/response");
const config = require("../../config");
const redis = require("redis");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io;
    const portalUsers =[];
    const roomUsers =[];
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
      let sessionId = socket.handshake.session?.user?.connection;
      
      id = await EnterAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal");
      socket.on("disconnect", async (reason) => {
        await LeaveAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal", id);
        await LeaveAsync(roomUsers, roomId, `r-${userId}`, roomId, "room", idInRoom);
        id = -1;
        idInRoom = -1;
        sessionId = -1;
      });

      socket.on("getSessionsInPortal", async () => {
        var users = [];
        Object.values(portalUsers[tenantId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
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

      var getRoom = (roomPart) => {
        return `${tenantId}-${roomPart}`;
      };

      async function LeaveAsync(usersList, key, redisKey, socketKey, socketDest, sessionId) 
      {
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
            
            var date = new Date().toString();
            if(user.sessions.size <= 0)
            {
              user.status = "offline";
              updateUser(portalUsers, user, userId, tenantId);
              onlineIO.to(socketKey).emit(`leave-in-${socketDest}`, {userId, date} );
            }
            else
            {
              updateUser(portalUsers, user, userId, tenantId);
              onlineIO.to(socketKey).emit(`leave-session-in-${socketDest}`, {userId, sessionId, date} );
            }
          }
        }
      }

      async function EnterAsync(usersList, key, redisKey, socketKey, socketDest) {
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
            page: session?.user?.profileUrl,
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
            ip: ipAddress
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
          var stringUser = serialize(user);
          onlineIO.to(socketKey).emit(`enter-in-${socketDest}`, stringUser );
        }
        else
        {
          onlineIO.to(socketKey).emit(`enter-session-in-${socketDest}`, {userId, session} );
        }

        updateUser(usersList, user, userId, key);
        return id;
      }

      socket.on("enterInRoom", async(roomPart)=>{
          roomId = getRoom(roomPart);
          idInRoom = await EnterAsync(roomUsers, roomId, `r-${userId}`, roomId, "room");
      });

      socket.on("leaveRoom", async()=>{
        await LeaveAsync(roomUsers, roomId, `r-${userId}`, roomId, "room", idInRoom);
        idInRoom = -1;
    });

      socket.on("getSessionsInRoom", async (roomPart) => {
        var users = [];
        var roomId = getRoom(roomPart);
        Object.values(roomUsers[roomId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
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

      function serialize(user)
      {
        var serUser = {
          id: user.id,
          displayName: user.displayName,
          page: user.page,
          sessions: Array.from(user.sessions, ([name, value]) => {
            return value;
          }),
          status: user.status
        };
        serUser.sessions = serUser.sessions.concat(Array.from(user.offlineSessions, ([name, value]) => {
          return value;
        }));
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
            updateUser(portalUsers, user, userId, tenantId);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            updateUser(portalUsers, user, userId, tenantId);
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
          updateUser(portalUsers, user, userId, tenantId);
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
            updateUser(portalUsers, user, userId, tenantId);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            updateUser(portalUsers, user, userId, tenantId);
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

    function updateUser(list, user, userId, id){
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
  