module.exports = (files, users) => {
  const router = require("express").Router(),
    bodyParser = require("body-parser"),
    check = require("../middleware/authService.js")();

  router.use(bodyParser.json());
  router.use(bodyParser.urlencoded({ extended: false }));
  router.use(require("cookie-parser")());
  router.use((req, res, next) => {
    const token = req?.headers?.authorization;
    if (!check(token)) {
      res.sendStatus(403);
      return;
    }

    next();
  });

  router.use("/files", require(`./files.js`)(files));
  router.use("/onlineusers", require(`./onlineusers.js`)(users));

  return router;
};
