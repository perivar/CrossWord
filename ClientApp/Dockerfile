# references:
# https://jskim1991.medium.com/improving-performance-of-react-app-using-nginx-a7460db423d4
# https://blog.devgenius.io/part-two-dockerizing-for-production-a-typescript-react-app-with-nginx-with-wsl2-alpine-linux-on-85660be3956
# https://github.com/perivar/CrosswordFeud/blob/4cf7ecf1e5d943d8e49143341f90166c91660d2d/ClientApp/Dockerfile

# build environment
FROM node:alpine as builder
ENV NODE_ENV production # this ensures we do not install dev dependencies

# Set the working directory to /app inside the container
WORKDIR /app

# Cache and install dependencies (npm ci makes sure the exact versions in the lockfile gets installed)
# COPY . .
# RUN npm ci 

# Cache and install dependencies
COPY package.json .
# COPY package-lock.json .
# RUN npm install
COPY yarn.lock .
RUN yarn

# Copy over the rest of the client files
COPY . .
# RUN npm run build
RUN yarn build

# hosting

# Bundle static assets with nginx
FROM nginx:stable-alpine as production
ENV NODE_ENV production

# Copy built assets from `build` image
COPY --from=builder /app/build /usr/share/nginx/html

# Add your nginx.conf
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx/default.conf /etc/nginx/conf.d
COPY nginx/gzip.conf /etc/nginx/conf.d

# EXPOSE 3000 # Remember, I like to expose ports in docker-compose

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
