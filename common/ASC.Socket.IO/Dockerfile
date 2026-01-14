FROM node:20-alpine

WORKDIR /app

# Copy package.json and yarn.lock files
COPY package.json yarn.lock .yarnrc.yml ./
COPY .yarn ./.yarn

# Install dependencies
RUN yarn install --immutable

# Copy application code
COPY . .

# Expose the port the app runs on
EXPOSE 9899

# Command to run the application
CMD ["node", "server.js"]
