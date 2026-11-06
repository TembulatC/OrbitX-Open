# OrbitX

A real-time aerospace analytics platform designed to simulate and track satellite orbital trajectories based on open-source ballistic data.

🌐 **Service website:** [orbitx-web.com](https://orbitx-web.com)

---

## Architecture and Systems Design

The system is designed as a **modular vertical monolith**, with the internal layers of each business module organized according to the principles of **Onion Architecture**.

* **Domain Isolation:** Mathematical models, constants, and ballistic calculation algorithms are fully encapsulated within the domain layer and isolated from external frameworks, databases, and infrastructure layers.
* **Vertical slices:** The codebase is divided into independent functional modules, which eliminates code dependencies and ensures a transparent migration path to distributed systems as the project grows.

---

## Telemetry Pipeline and Memory Management

The platform processes parallel calculation loops for ballistic coordinates in real time using low-level resource management patterns from the .NET runtime:

* **Id-Only Caching Strategy:** To optimize the PostgreSQL disk subsystem and eliminate unnecessary I/O operations, the application caches lightweight collections of entity identifiers in `IMemoryCache`. Pagination and catalog filtering across 41 categories are calculated directly in the server’s main memory (RAM).
* **Parallel Asynchronous Computation:** The background coordinate-recalculation worker isolates the SGP4 core’s heavy mathematical computations from the application’s main thread using asynchronous long-running tasks (`Task.Run`). The shared memory state is protected using thread-safe collections (`ConcurrentDictionary`), which prevents race conditions when users access translations concurrently.
* **Deterministic resource cleanup:** Strict control over the lifecycle of duplex connections via SignalR (WebSockets) has been implemented. Through `CancellationToken` chains, the background worker instantly detects the termination of a client session, forcibly terminates the computational task for that peer, and cleans up “zombie connections” from memory, preventing memory leaks.
* **Fault Isolation (High Availability):** Structural fault tolerance has been implemented. In the event of a brief DBMS outage, the system isolates the infrastructure failure, switches to high-performance logging via Source Generators (`[LoggerMessage]`), and seamlessly streams coordinates to clients from a preheated RAM cache.

---

## Infrastructure and DevOps Specification

The architecture is deployed using a **triple Docker topology**, ensuring strict isolation boundaries between environments:

* **Separation of environments:** Isolated compose files support independent environments for local development (Local Dev), a closed testing environment on a VPS (Staging), and the production server (Production).
* **Data Security:** The PostgreSQL DBMS container is completely isolated within a Docker overlay network, with no ports exposed to the outside world. Access to the data is restricted to encrypted SSH tunnels with local port forwarding.
* **Traffic routing:** The Nginx proxy server acts as an ingress controller, handling SSL/TLS certificate termination and securely reverse-proxying WebSocket connections (`wss://`).

---

## Technology Stack

* **Backend:** .NET, ASP.NET Core, Entity Framework Core, PostgreSQL, SignalR (WebSockets).
* **Mathematical Kernel:** SGP4 (Simplified General Perturbations 4) ballistic calculation model.
* **Front end:** Vue 3, TypeScript, Vite, Leaflet.js (mapping).
* **Runtime environment:** Docker, Docker Compose, Nginx Reverse Proxy, Linux (Ubuntu LTS).

## Deployment

* To deploy a presentation/test sandbox for the service, enter the following commands in `cmd/PowerShell/any other console`:
```
git clone https://github.com/TembulatC/OrbitX-Open.git
```
* Go to the folder created by the command above (if you haven't created it yet)
* Next, rename `.env.example` to `.env`. Enter the information you need.
* Then enter the following commands:
```
cd "Full path to your folder\OrbitX-Open"
docker-compose up -d --build
```
After completing these steps, your sandbox will be deployed on your PC (be sure to install and run Docker Desktop for this).

The website will go live and be available at: `http://localhost:8081`

* To access Swagger for managing data and adding it to the database so you can view satellite trajectories (the database is initially empty when the service is deployed), go to: `http://localhost:{port (see below)}/swagger/index.html`
* You can find the correct port for the address above by running the following command in the root directory of the OrbitX-Open folder:
```
docker port orbitx-local
```
* Go to Swagger, select the `/api/v1/AddSatelliteData` method, and enter any satellite category you want, starting with a lowercase letter (for example, `kuiper`—see the categories on the website `https://celestrak.org/NORAD/elements/index.php?FORMAT=csv` by clicking the small blue document icons next to the category names. A page will open, and you can see the category name in its URL) and click `Execute`.
* Go to the website itself on port `8081`. Navigate to the satellites directory, select the category you recently added from the drop-down list of satellite categories, and click the `Run` button for the selected satellite.
