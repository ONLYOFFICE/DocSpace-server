module.exports = (onlineusers) => {
    const router = require("express").Router()

    router.post("/leave-session-in-portal", (req, res) => {
      onlineusers.leaveSessionInPortal(req.body);
      res.end();
    });
    return router;
  };