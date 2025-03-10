module.exports = () => {
const router = require("express").Router();
router.get("/health", (req, res) => 
    {
        res.status(200).json({status: "Healthy"});
    });
    
    return router;
};
  