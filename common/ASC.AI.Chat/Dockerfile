FROM node:20-alpine

WORKDIR /app

# Copy manifests and the local @onlyoffice/ai-chat tarball (a file: dependency resolved at install time)
COPY package.json yarn.lock .yarnrc.yml onlyoffice-ai-chat-*.tgz ./
COPY .yarn ./.yarn

# Install dependencies; drop the fetch cache so the archives do not stay in the layer
RUN yarn install --immutable && yarn cache clean --all

# Copy application code (node_modules and local state are excluded via .dockerignore)
COPY . .

# Expose the port the app runs on
EXPOSE 9837

# Production entrypoint: sets NODE_ENV=production and loads app.ts through tsx
CMD ["node", "server.js"]
