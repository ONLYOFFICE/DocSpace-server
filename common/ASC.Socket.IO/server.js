// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

const express = require("express");
const { Server } = require("socket.io");
const { createAdapter } = require('@socket.io/redis-adapter');
const { createServer } = require("http");
const logger = require("morgan");
const redis = require("redis");
const expressSession = require("express-session");
const cookieParser = require("cookie-parser");
const RedisStore = require("connect-redis").default;
const MemoryStore = require("memorystore")(expressSession);
const sharedsession = require("express-socket.io-session");
const process = require('process');

const config = require("./config");
const auth = require("./app/middleware/auth.js");
const winston = require("./app/log.js");

(async () => {
winston.stream = {
  write: (message) => winston.info(message),
};

const port = config.get("app").port || 9899;
const app = express();

const secret = config.get("core").machinekey + new Date().getTime();
const secretCookieParser = cookieParser(secret);
const baseCookieParser = cookieParser();

const redisOptions = config.get("Redis");

let store;
let redisClient;
if (redisOptions != null) {
  redisClient = redis.createClient(redisOptions);
  redisClient.on('error', err => winston.error('Redis Client Error', err));
  await redisClient.connect();
  winston.info('Redis connect');
  store = new RedisStore({ client: redisClient });
} else {
  store = new MemoryStore();
}

const session = expressSession({
  store: store,
  secret: secret,
  resave: false,
  saveUninitialized: true,
  cookie: {
    path: "/",
    httpOnly: true,
    secure: false,
    maxAge: null,
  },
  cookieParser: secretCookieParser,
  name: "socketio.sid",
});

app.use(logger("dev", { stream: winston.stream }));
app.use(session);

const httpServer = createServer(app);

const options = {
  cors: {
    //origin: "http://localhost:8092",
    methods: ["GET", "POST"],
    allowedHeaders: ["Authorization"],
    //credentials: true,
  },
  allowRequest: (req, cb) => {
    baseCookieParser(req, null, () => {});
    const token =
      req?.headers?.authorization ||
      req?.cookies?.authorization ||
      req?.cookies?.asc_auth_key ||
      req?._query?.share;

    if (!token) {
      winston.info(`not allowed request: empty token`);
      return cb("auth", false);
    }
    return cb("auth", true);
  },
};

const io = new Server(httpServer, options);

io.use(sharedsession(session, secretCookieParser, { autoSave: true }))
  .use((socket, next) => {
    baseCookieParser(socket.client.request, null, next);
  })
  .use((socket, next) => {
    auth(socket, next);
  });

if (redisClient != null) 
{
  const pubClient = redisClient;
  const subClient = pubClient.duplicate();
  subClient.on('error',  err => winston.error('Redis Client Error', err));
  subClient.connect();
  
  io.adapter(createAdapter(pubClient, subClient));
}


app.get("/", (req, res) => {
  res.send("<h1>Invalid Endpoint</h1>");
});

const filesHub = require("./app/hubs/files.js")(io);

app.use("/controller", require("./app/controllers")(filesHub));
app.use("/", require("./app/controllers/healthCheck.js") ());

httpServer.listen(port, () => winston.info(`Server started on port: ${port}`));

process.on('unhandledRejection', (reason, p) => {
  winston.error('Unhandled rejection at:', p, 'reason:', reason);
});

process.on('uncaughtException', (error) => {
  winston.error(`Unhandled exception: ${error}\n` + `Exception origin: ${error.stack}`);
});

module.exports = io;
})();