# CapShop

Welcome to the CapShop repository. This is an event-driven microservices e-commerce backend built on .NET 8, accompanied by a React frontend. We use RabbitMQ and MassTransit for internal async communication, and Ocelot handles the API Gateway routing.

If you are jumping in to fix a bug or add a feature, this document covers what you need to get up and running without breaking things.

## Architecture and Tech Stack

This is not a monolith. The backend is split into scoped services:

- Framework: .NET 8 Web API
- Gateway: Ocelot configured as the single ingress point.
- Messaging: RabbitMQ via MassTransit.
- Orchestration/Saga: We use MassTransit State Machines in the OrderService to manage distributed transactions (e.g., locking inventory, taking payment, confirming order).
- Cache: Redis (mostly for basket/session states).
- Storage: Decoupled databases per service (make sure you do not cross-query DBs).
- Frontend: React 19 (lives in /frontend).

### The Services (/backend/Services)

1. CatalogService: Products, pricing, inventory basics.
2. OrderService: Handles the OrderSagaStateMachine. If an order is stuck, check the state machine logs here.
3. PaymentService: Mock/integration for payments. Listens to events from OrderSaga.
4. AuthService and UserService: JWT issuance and user profiles. You will need a valid token from here to hit protected routes via Ocelot.
5. AdminService: For admin dashboard operations.
6. NotificationService: Emails/alerts. Purely an event consumer.

## Local Development Setup

You can run this piecemeal via Visual Studio or Rider, but it is much easier to just use docker-compose to spin up the infrastructure layer.

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK
- Node.js v18+ (for frontend)

### Quick Start

We have a docker-compose.yml at the root that scaffolds everything.

1. Start all services, databases, RabbitMQ, and Redis:
   docker-compose up --build -d

2. Check logs if something is crashing (usually RabbitMQ race conditions on startup):
   docker-compose logs -f [service_name]

### Manual Run

If you need to attach a debugger to a backend service:

1. Spin up just the infra dependencies (rabbitmq, redis, db) using docker-compose.
2. Open the solution backend/CapShop.sln.
3. Set your startup projects (do not forget the Gateway).
4. Run the backend.
5. In a separate terminal, start the frontend by navigating to the frontend folder and running "npm install" followed by "npm start".

## Important Endpoints and Ports

When running locally via compose, here is where things usually live (check Ocelot config in /backend/Gateway/ocelot.json for exact routing rules):

- Frontend (React): http://localhost:3000
- API Gateway (Ocelot): http://localhost:5000 (All API calls should go here!)
- RabbitMQ Management: http://localhost:15672 (Credentials are guest / guest)

Always route your frontend API requests through the Ocelot Gateway URL, not the direct microservice ports.

## CI/CD Pipeline

We use Jenkins for our builds. 

- Look at the Jenkinsfile at the root to see the pipeline stages (Build, Test, Build Docker Image, Push, Deploy).
- For pull requests, we test against a sterile compose environment using docker-compose.ci.yml. If your build fails in PR, run "docker-compose -f docker-compose.ci.yml up --build" locally to see what broke.
- We occasionally use ngrok for exposing the local Jenkins instance to GitHub Webhooks.

## Gotchas and Developer Notes

- Saga Troubleshooting: If an order completes but payment fails, MassTransit will execute the compensating transactions (e.g., refund payment, revert inventory lock). If things get stuck in an intermediate state, check the RabbitMQ queues or the MassTransit logs in OrderService.
- Shared Project (/backend/Shared): Domain events and shared contracts live in the CapShop.Shared project. If you change an event signature here, you must rebuild all services that publish/consume it. Do not break the contract.

Submit pull requests against the develop branch. Make sure your unit tests pass.
