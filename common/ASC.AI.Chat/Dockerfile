FROM node:24.22.1-alpine

WORKDIR /app

# Copy manifests, with the locally packed ai-chat tarball package.json points
# at (`file:onlyoffice-ai-chat-*.tgz`): the install below needs it. A glob
# matching nothing is fine next to real sources.
COPY package.json yarn.lock .yarnrc.yml *.tgz ./
COPY .yarn ./.yarn

# A yarn.lock that no longer matches package.json fails the build here instead of being
# resolved again: `focus` below neither takes `--immutable` nor reads
# YARN_ENABLE_IMMUTABLE_INSTALLS (yarn 4 reads it in `yarn install` only), so the lock file is
# checked by an install of its own. It links the dev dependencies too and builds nothing.
#
# Then install the runtime dependencies only (`tsx` is one of them). Both steps share one RUN:
# a layer keeps what its RUN leaves behind, and a later RUN cannot take files out of an earlier
# layer, so the dev dependencies - the tools of `yarn check`: eslint, vitest, msw, supertest,
# typescript - are removed before this layer is committed. The fetch cache is dropped in the
# same RUN, so the archives do not stay in the layer either
RUN yarn install --immutable --mode=skip-build \
  && rm -rf node_modules \
  && yarn workspaces focus --production \
  && yarn cache clean --all

# Copy application code (node_modules, local state, the tests and the tools of `yarn check`
# are excluded via .dockerignore)
COPY . .

# Expose the port the app runs on
EXPOSE 9837

# Production entrypoint: sets NODE_ENV=production and loads app.ts through tsx
CMD ["node", "server.js"]
