var uap = require('ua-parser-js');
const { json, status } = require("express/lib/response");
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
    
    var clear = () =>
    {
      var today = new Date();
      var date = new Date(today.setDate(today.getDate() - 3));
      portalUsers.forEach(users =>
      {
        for(var j = 0; j < users.length; j++)
        {
          for(var i = 0; i < users[j].offlineSessions.length; i++)
          {
            if(users[j].offlineSessions[i].date <= date)
            {
              users[j].offlineSessions.splice(i, 1);
              i--;
            }
          }
          if(users[j].offlineSessions.length != 0)
          {
            redisClient.set(users[j].id, JSON.stringify(users[j].offlineSessions));
          }
          else
          {
            redisClient.del(users[j].id);
          }
          if(users[j].offlineSessions.length == 0 && users[j].sessions.length == 0)
          {
            users.splice(j, 1);
            j--;
          }
        }
      });
    };
    setInterval(clear, 1000 * 60 * 60 * 12);
    async function startAsync(socket)
    {
      if (socket.handshake.session.system) 
      {
        return;
      }
      const ipAddress = socket.handshake.session.ip;
      const ipInfo = socket.handshake.session.ipInfo;
      const parser = uap(socket.request.headers['user-agent']);
      const operationSystem = parser.os.version !== undefined ?  `${parser.os.name} ${parser.os.version}` : `${parser.os.name}`;  
      const browserVersion = parser.browser.version ? parser.browser.version : '';
      const browser = parser.browser.name + " " + browserVersion
  
      const userId = socket.handshake.session?.user?.id;
      const displayName = socket.handshake.session?.user?.displayName;
      const avatar = socket.handshake.session?.user?.avatar;
      const tenantId = socket.handshake.session?.portal?.tenantId;
      var logout = false;
      let roomId = -1;
      let file;
      let sessionId = socket.handshake.session?.user?.connection;
      await InitUsersAsync(tenantId, portalUsers);
      await EnterAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal");
      if(socket.handshake.session.file)
      {
        roomId = `${tenantId}-${socket.handshake.session.file.roomId}`;
        await InitUsersAsync(roomId, roomUsers);
        file = socket.handshake.session.file.title;
       await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
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
        await LeaveAsync(portalUsers, tenantId, userId, `p-${tenantId}`, "portal", sessionId);
        await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", sessionId, true);
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
        sessionId = -1;
        roomId = -1;
        file = null;
      });

      socket.on("getSessionsInPortal", async (obj) => {
        var index = obj.startIndex;
        var count = obj.count;
        var users = [];
        if(index == 0)
        {
          pushedUsers = [];
          date = new Date();
        }
        if(portalUsers[tenantId])
        {
          var onlineUsers = portalUsers[tenantId].filter(o => o.sessions.length != 0).sort(userSort);
          
          for(var i = index; i < onlineUsers.length; i++)
          {
            users.push(serialize(onlineUsers[i]));
            pushedUsers.push(onlineUsers[i].id);
            if(users.length == count)
            {
              break;
            }
          }
          if(onlineUsers.length < count)
          {
            var offlineUsers = portalUsers[tenantId].filter(o => o.sessions.length == 0).sort(userSort);
            index = index - onlineUsers.length;
            index = index < 0 ? 0 : index;
            for(var i = index; i < offlineUsers.length; i++)
            {
              users.push(serialize(offlineUsers[i]));
              pushedUsers.push(offlineUsers[i].id);
            }
          }
        }
        var result = 
        { 
          total: portalUsers[tenantId].length,
          users: users
        };
        onlineIO.to(socket.id).emit("sessions-in-portal",  result );
      });

      socket.on("getSessions", async (obj) => {
        var id = obj.id;
        var user = getUser(portalUsers, id, tenantId);
        var sessions = user.sessions.concat(user.offlineSessions);
        onlineIO.to(socket.id).emit("user-sessions",  sessions );
      });

      socket.on("logout", () => {
        logout = true;
      });
      
      socket.on("subscribeToPortal", () => {
        logger.info(`client ${socket.id} subscribe portal ${tenantId}`);
        socket.join(`p-${tenantId}`);
      });

      socket.on("subscribeToUser", (obj) => {
        var id = obj.id;
        socket.join(`p-${tenantId}-${id}`);
      });
  
      socket.on("unsubscribeToPortal", () => {
        logger.info(`client ${socket.id} unsubscribe portal ${tenantId}`);
        socket.leave(`p-${tenantId}`);
      });

      socket.on("unsubscribeToUser", (obj) => {
        var id = obj.id;
        socket.leave(`p-${tenantId}-${id}`);
      });
     /******************************* */
      socket.on("enterInRoom", async(roomPart)=>
      {
        roomId = getRoom(roomPart);
        await InitUsersAsync(roomId, roomUsers);
        await EnterAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", true);
      });

    socket.on("leaveRoom", async()=>{
      await LeaveAsync(roomUsers, roomId, `${roomId}-${userId}`, roomId, "room", sessionId, true);
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

    var userSort = (a,b) => 
      {
        if(a.sessions.length != 0 && b.sessions.length != 0 )
        {
          return b.sessions[b.sessions.length - 1].date - a.sessions[a.sessions.length - 1].date;
        }
        if(a.sessions.length != 0)
        {
          return -1;
        }
        if(b.sessions.length != 0)
        {
          return 1;
        }
        return b.offlineSessions[b.offlineSessions.length - 1].date - a.offlineSessions[a.offlineSessions.length - 1].date;
      };

      var getRoom = (obj) => {
        return `${tenantId}-${obj.roomPart}`;
      };

      async function InitUsersAsync(key, list)
      {
        if(!allUsers[key])
        {
          allUsers[key] = [];
          var users = JSON.parse(await redisClient.get(`allusers-${key}`));
          if(users)
          {
            allUsers[key] = users;
          }
          if(allUsers[key].length != 0)
          {
            for(var i = 0; i < allUsers[key].length; i++)
            {
              var user = allUsers[key][i];
              var u = {};
              u.id = user.id;
              u.displayName = user.displayName;
              u.avatar = user.avatar;
              var offSess = await redisClient.get(u.id);
              if(offSess && offSess != '[]')
              {
                offSess = JSON.parse(offSess);
                for(var i = 0; i < offSess.length; i++)
                {
                  offSess[i].date = new Date(offSess[i].date);
                }
              }
              else
              {
                offSess = [];
              }
              u.offlineSessions = offSess;
              u.sessions = [];
              addUser(list, u, key);
            }
            clear();
          }
        }
        if(!allUsers[key].some((a) => a.id == userId))
        {
          var u = {};
          u.id = userId;
          u.avatar = avatar;
          u.displayName = displayName;
          allUsers[key].push(u);
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
          var index = user.sessions.findIndex(u=> u.id == sessionId);
          var session = user.sessions[index];
          user.sessions.splice(index, 1);
          if(!user.sessions.find(e=> e.id == session.id))
          {
            user.offlineSessions.push(
              {
                id: session.id,
                platform: session.platform,
                browser: session.browser,
                country: session.country,
                city: session.city,
                ip: session.ip,
                status: "offline",
                date: new Date()
            });
            redisClient.set(redisKey, JSON.stringify(user.offlineSessions));
          }
          if(user.sessions.length == 0)
          {
            if(!logout) 
            {
              onlineIO.to(socketKey).emit(`leave-in-${socketDest}`, {userId , sessionId} );
            }
            else
            {
              onlineIO.to(socketKey).emit(`logout-in-${socketDest}`, {userId, sessionId} );
            }
          }
          else
          {
            if(isRoom)
            {
              return;
            }
            if(!logout)
            {
              if(user.sessions[user.sessions.length - 1].id != session.id)
              {
                onlineIO.to(socketKey).emit(`new-session-in-${socketDest}`, {userId: user.id, displayName: user.displayName, avatar: user.avatar, session: user.sessions[user.sessions.length - 1]} );
              }
              onlineIO.to(`${socketKey}-${userId}`).emit(`leave-session-in-${socketDest}`, {userId, sessionId} );
            }
            else
            {
              onlineIO.to(`${socketKey}-${userId}`).emit(`logout-session-in-${socketDest}`, {userId, sessionId} );
            }
          }
        }
      }

      async function EnterAsync(usersList, key, redisKey, socketKey, socketDest, isRoom = false) {
        var user = getUser(usersList, userId, key);
        var isNew = !user;
        var session;
        if (isNew) 
        {
          var sessions = new Array();
          session ={
            id: sessionId,
            platform: operationSystem,
            browser: browser,
            country: ipInfo.country,
            city: ipInfo.city,
            ip: ipAddress,
            status: "online",
            date: new Date()
          };
          sessions.push(session);

          var offSess = new Array();

          user = {
            id: userId,
            avatar: avatar,
            displayName: displayName,
            sessions: sessions,
            offlineSessions: offSess
          };
        }
        else
        {
          session = {
            id: sessionId,
            platform: operationSystem,
            browser: browser,
            country: ipInfo.country,
            city: ipInfo.city,
            ip: ipAddress,
            status:"online",
            date: new Date()
          };
          user.sessions.push(session);
        }
        
        for(var i = 0; i < user.offlineSessions.length; i++)
        {
          var value = user.offlineSessions[i];
          if (value.id == sessionId || (value.ip == ipAddress && value.browser == browser && value.platform == operationSystem))
          {
            user.offlineSessions.splice(i, 1);
            i--;
          }
        }
        if(user.offlineSessions.length != 0)
        {
          redisClient.set(redisKey, JSON.stringify(Array.from(user.offlineSessions)));
        }
        else
        {
          redisClient.del(redisKey);
        }
        if(isNew)
        {
          addUser(usersList, user, key);
        }
        if(user.sessions.length == 1)
        {
          onlineIO.to(socketKey).emit(`enter-in-${socketDest}`, {userId: user.id, displayName: user.displayName, avatar: user.avatar, session: session});
        }
        else
        {
          if(isRoom)
          {
            return;
          }
          onlineIO.to(`${socketKey}-${userId}`).emit(`enter-session-in-${socketDest}`, {userId, session} );
        }

        return;
      }

      function serialize(user, isRoom)
      {
        var serUser = {
          userId: user.id,
          displayName : user.displayName,
          avatar: user.avatar
        };
        if(user.sessions.length != 0)
        {
          serUser.session = user.sessions[user.sessions.length - 1]
          if(isRoom)
          {
            if(editFiles[roomId] && editFiles[roomId][user.id] && editFiles[roomId][user.id].length != 0)
            {
              serUser.file = editFiles[roomId][user.id][0];
              serUser.session.status = "edit";
            }
          }
        }
        else
        {
          serUser.session = user.offlineSessions[user.offlineSessions.length - 1];
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
      var x =  list[id].find(q=> q.id == userId);
      return x;
    }

    function addUser(list, user, id){
      if(!list[id])
      {
        list[id] = [];
      }
      list[id].push(user);
    }
    
    return {
      startAsync,
      logoutUser,
      logoutSession,
      logoutExpectThis
    };
};
  