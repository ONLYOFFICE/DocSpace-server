module.exports = (req) => {
  const proto = req.headers['x-forwarded-proto']?.split(',').shift();
  const host = req.headers['x-forwarded-host']?.split(',').shift();
  const port = req.headers["x-forwarded-port"]?.split(",").shift();

  return `${proto}://${host}${port ? `:${port}` : ""}`;
};
