# =========================================
# Stage 1: Build the Angular Application
# =========================================
ARG NODE_VERSION=25.0.0-alpine
ARG NGINX_VERSION=alpine3.22

# Use a lightweight Node.js image for building (customizable via ARG)
FROM node:${NODE_VERSION} AS builder

# Set the working directory inside the container
WORKDIR /app

# Copy package-related files first to leverage Docker's caching mechanism
COPY package.json package-lock.json ./

# Install project dependencies using npm ci (ensures a clean, reproducible install)
RUN npm ci

# Install angular CLI globally
RUN npm install -g @angular/cli@20.3.2

# Copy the rest of the application source code into the container
COPY . .

# Build the Angular application
RUN ng build --configuration production

# =========================================
# Stage 2: Prepare Nginx to Serve Static Files
# =========================================

FROM nginxinc/nginx-unprivileged:${NGINX_VERSION} AS runner

# Use a built-in non-root user for security best practices
USER nginx

# Adresse des Backends: lokal heißt der Compose-Service "api",
# auf Railway wird API_HOST=api.railway.internal gesetzt
ENV API_HOST=api

# Nginx-Konfiguration als Vorlage kopieren (API_HOST wird beim Start eingesetzt)
COPY nginx.conf.template /etc/nginx/nginx.conf.template

# Copy the static build output from the build stage to Nginx's default HTML serving directory
COPY --chown=nginx:nginx --from=builder /app/dist/*/browser /usr/share/nginx/html

# Expose port 8080 to allow HTTP traffic
# Note: The default NGINX container now listens on port 8080 instead of 80 
EXPOSE 8080

# Beim Start nur ${API_HOST} ersetzen (nginx-Variablen wie $host bleiben erhalten)
# und die fertige Konfiguration nach /tmp schreiben, da der Container ohne Root läuft
ENTRYPOINT ["/bin/sh", "-c", "envsubst '${API_HOST}' < /etc/nginx/nginx.conf.template > /tmp/nginx.conf && exec nginx -c /tmp/nginx.conf -g 'daemon off;'"]
