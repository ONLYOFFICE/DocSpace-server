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
    var allUsers = [];
    let targetFile = [];
    const redisOptions = config.get("Redis");
    var redisClient = redis.createClient(redisOptions);
    await redisClient.connect();

    async function startAsync(socket)
    {
      if (socket.handshake.session.system) {
        return;
      }
      const ipAddress = getCleanIP(socket.handshake.headers['x-forwarded-for']);
      const parser = uap(socket.request.headers['user-agent']);
      const operationSystem = parser.os.version !== undefined ?  `${parser.os.name} ${parser.os.version}` : `${parser.os.name}`;  
      const browserVersion = parser.browser.version ? parser.browser.version : '';
      const browser = parser.browser.name + " " + browserVersion
  
      const userId = socket.handshake.session?.user?.id;
      const tenantId = socket.handshake.session?.portal?.tenantId;
      let id;
      let idInRoom = -1;
      let roomId = -1;
      let file;
      let sessionId = socket.handshake.session?.user?.connection;
      
      await InitUsersAsync(tenantId, portalUsers);
      id = await EnterAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal");
      if(socket.handshake.session.file)
      {
        roomId = `${tenantId}-${socket.handshake.session.file.roomId}`;
        await InitUsersAsync(roomId, roomUsers);
        file = socket.handshake.session.file.title;
        idInRoom = await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
        if(!editFiles[roomId])
        {
          editFiles[roomId] = [];
        }
        if(!editFiles[roomId][userId])
        {
          editFiles[roomId][userId] = [];
        }
        if(editFiles[roomId][userId].length == 0)
        {
          onlineIO.to(roomId).emit(`start-edit-file-in-room`, {userId, file} );
          targetFile[roomId] = file;
        }
        editFiles[roomId][userId].push(file);
      }
      socket.on("disconnect", async (reason) => {
        await LeaveAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal", id);
        await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", idInRoom, true);
        if(file)
        {
          var index = editFiles[roomId][userId].indexOf(file);
          editFiles[roomId][userId].splice(index, 1);
          var user = getUser(roomUsers, userId, roomId);
          if(!editFiles[roomId][userId].includes(file) && user.status == "online" && targetFile[roomId] == file)
          {
            onlineIO.to(roomId).emit(`stop-edit-file-in-room`, {userId, file} );
            if(editFiles[roomId][userId].length > 0)
            {
              file = editFiles[roomId][userId][0];
              targetFile[roomId] = file;
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
      socket.on("enterInRoom", async(roomPart)=>
      {
        roomId = getRoom(roomPart);
        await InitUsersAsync(roomId, roomUsers);
        idInRoom = await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
      });

    socket.on("leaveRoom", async()=>{
      await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", idInRoom, true);
      idInRoom = -1;
  });

    socket.on("getSessionsInRoom", async (roomPart) => {
      var users = [];
      roomId = getRoom(roomPart);
      await InitUsersAsync(roomId, roomUsers);
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
    });

      var getRoom = (obj) => {
        return `${tenantId}-${obj.roomPart}`;
      };

      async function InitUsersAsync(key, list)
      {
        if(!allUsers[key])
        {
          allUsers[key] = JSON.parse(await redisClient.get(`allusers-${key}`));
          if(!allUsers[key])
          {
            allUsers[key]= [];
          }
          else
          {
            for(var i = 0; i < allUsers[key].length; i++)
            {
              var id = allUsers[key][i];
              var u = {};
              u.id = id;
  
              var offSess = await redisClient.get(id);
              if(offSess && offSess != '{}')
              {
                offSess = new Map(JSON.parse(offSess).map(i => [i[0], i[1]]));
              }
              else
              {
                offSess = new Map();
              }
              u.offlineSessions = offSess;
              u.status = "offline";
              u.sessions = new Map();
              addUser(list, u, id, key);
            }
          }
        }
        if(!allUsers[key].includes(userId))
        {
          allUsers[key].push(userId);
          await redisClient.set(`allusers-${key}`, JSON.stringify(allUsers[key]));
        }
      }

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
            
            redisClient.set(redisKey, JSON.stringify(Array.from(user.offlineSessions)));
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
              browser: browser,
              ip: ipAddress,
              status: "online"
            });
          id = 1;

          var offSess = new Map();

          user = {
            id: userId,
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
            browser: browser,
            ip: ipAddress,
            status:"online"
          };
          user.sessions.set(
            id,
            session
          );
        }
  
        for (let k of user.offlineSessions.keys()) {
          var value = user.offlineSessions.get(k);
          if (value.id == sessionId || (value.ip == ipAddress && value.browser == browser && value.platform == operationSystem))
          {
            user.offlineSessions.delete(k);
          }
        }
        if(user.offlineSessions.size != 0)
        {
          redisClient.set(redisKey, JSON.stringify(Array.from(user.offlineSessions)));
        }
        else
        {
          redisClient.del(redisKey);
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
          if(array.filter(e=> e.browser == session.browser && e.platform == session.platform && e.ip == session.ip).length != 1)
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
          status: user.status
        };
        if(isRoom)
        {
          if(editFiles[roomId] && editFiles[roomId][user.id] && editFiles[roomId][user.id].length != 0)
          {
            serUser.file = editFiles[roomId][user.id][0];
            serUser.status = "edit";
          }
          if(serUser.status == "offline")
          {
            serUser.date = Array.from(user.offlineSessions)[user.offlineSessions.size-1][1].date;
          }
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
    }

    function logoutUser({userId, tenantId} = {}) 
    {
      var user = getUser(portalUsers, userId, tenantId);
      if (user) 
      {
        user.sessions.forEach(
          function(entry) 
          {
            onlineIO.to(`${tenantId}-${userId}`).emit("s:logout-session", entry.id);
          });
      }
    }

    async function logoutExpectThis({id, userId, tenantId} = {}) 
    {
      var user = getUser(portalUsers, userId, tenantId);
      if (user) 
      {
        user.sessions.forEach(
          function(entry) 
          {
            if(entry.id != id)
            {
              onlineIO.to(`${tenantId}-${userId}`).emit("s:logout-session", entry.id);
            }
          });
      }
    }

    function logoutSession({ room, loginEventId } = {}) {
      logger.info(`logout user ${room} session ${loginEventId}`);
      onlineIO.to(room).emit("s:logout-session", loginEventId);
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
      startAsync,
      logoutUser,
      logoutSession,
      logoutExpectThis
    };
};
  