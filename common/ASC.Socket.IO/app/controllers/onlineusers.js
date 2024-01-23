module.exports = (users) => {
    const router = require("express").Router()

    router.post("/test", (req, res) => {
      res.end();
    });
    return router;
  };