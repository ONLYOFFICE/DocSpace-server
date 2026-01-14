FROM node:20-alpine

WORKDIR /app

# Copy package.json and yarn.lock files
COPY package.json yarn.lock ./
COPY .yarn ./.yarn
COPY patches ./patches

# Install dependencies
RUN yarn install --immutable

# Copy application code
COPY . .

# Expose the port the app runs on
EXPOSE 1900

# Command to run the application
CMD ["node", "server/webDavServer.js"]
