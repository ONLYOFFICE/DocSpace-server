module.exports = (onlineusers) => {
    const router = require("express").Router()

    router.post("/leave-in-portal", (req, res) => {
      onlineusers.leaveInPortal(req.body);
      res.end();
    });

    router.post("/leave-expect-this-portal", (req, res) => {
      onlineusers.leaveExceptThisInPortal(req.body);
      res.end();
    });
    return router;
  };