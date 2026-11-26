FROM node:24.22.1-alpine

WORKDIR /app

# Copy manifests, with the locally packed ai-chat tarball package.json points
# at (`file:onlyoffice-ai-chat-*.tgz`): the install below needs it. A glob
# matching nothing is fine next to real sources.
COPY package.json yarn.lock .yarnrc.yml *.tgz ./
COPY .yarn ./.yarn

# Install the runtime dependencies only (`tsx` is one of them): the tools of `yarn check` -
# eslint, vitest, msw, supertest, typescript - stay out of the image. `focus` takes no
# `--immutable`; it installs what yarn.lock names. Drop the fetch cache so the archives do
# not stay in the layer
RUN yarn workspaces focus --production && yarn cache clean --all

# Copy application code (node_modules, local state, the tests and the tools of `yarn check`
# are excluded via .dockerignore)
COPY . .

# Expose the port the app runs on
EXPOSE 9837

# Production entrypoint: sets NODE_ENV=production and loads app.ts through tsx
CMD ["node", "server.js"]
