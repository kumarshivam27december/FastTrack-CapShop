# Stage 1: Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Accept build arguments
ARG REACT_APP_API_BASE_URL=http://gateway:5041/gateway
ARG REACT_APP_GOOGLE_CLIENT_ID

# Copy package files
COPY ["frontend/package.json", "frontend/package-lock.json*", "./"]

# Install dependencies
RUN npm ci

# Copy source code
COPY ["frontend/", "."]

# Build the application with environment variables
RUN REACT_APP_API_BASE_URL=$REACT_APP_API_BASE_URL REACT_APP_GOOGLE_CLIENT_ID=$REACT_APP_GOOGLE_CLIENT_ID npm run build

# Stage 2: Runtime stage
FROM node:20-alpine
WORKDIR /app

# Install serve to serve the static build
RUN npm install -g serve

# Copy the built app from the build stage
COPY --from=build /app/build ./build

# Expose port 3000 for the frontend
EXPOSE 3000

# Set environment variable for API base URL
ENV REACT_APP_API_BASE_URL=http://gateway:5041/gateway
ENV NODE_ENV=production

# Run the app
CMD ["serve", "-s", "build", "-l", "3000"]
