module.exports = (onlineusers) => {
    const router = require("express").Router()

    router.post("/logout-user", (req, res) => {
      onlineusers.logoutUser(req.body);
      res.end();
    });

    router.post("/logout-expect-this", (req, res) => {
      onlineusers.logoutExpectThis(req.body);
      res.end();
    });

    router.post("/logout-session", (req, res) => {
      onlineusers.logoutSession(req.body);
      res.end();
    });
    return router;
  };