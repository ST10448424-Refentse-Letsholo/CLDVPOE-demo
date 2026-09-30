# CoffeeNChill Setup Guide

This guide explains how to set up, run, test, and troubleshoot the CoffeeNChill application locally.

---

## 1. Prerequisites

Before running the CoffeeNChill application locally, install:

- **.NET 8 SDK**
- **Docker Desktop**
- **Git**
- **Postman**

The project uses **Azure Functions with the .NET 8 isolated worker model**.

---

## 2. Clone the Repository

Clone the project repository:

```bash
git clone https://github.com/EMGPMD/cldv6212-group-1-poe-part-1-st10457044-tshiamo-aphane.git
```

Navigate into the project:

```bash
cd cldv6212-group-1-poe-part-1-st10457044-tshiamo-aphane
```

---

## 3. Run the Application Locally

Navigate to the Functions project:

```bash
cd CoffeeNChill.Functions
```

Build the project:

```bash
dotnet build
```

When no Azure Storage connection string is configured, the application uses:

```text
UseDevelopmentStorage=true
```

This allows the application to connect to a local **Azurite** instance.

---

## 4. Run Azurite with Docker

Azurite can be run locally using Docker.

The project uses the published CoffeeNChill Azurite image for the Part 1 standalone container setup.

Create the shared Docker network:

```bash
docker network create coffeenchill-net
```

Run Azurite:

```bash
docker run -d --name azurite --network coffeenchill-net \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mmolokik/coffeenchill-azurite:v1.0
```

The exposed ports provide access to the local Blob, Queue, and Table Storage services.

---

## 5. Run the Functions Application with Docker

Part 1 requires **standalone Docker containers without orchestration tools**. The Functions application and Azurite therefore run as two separate containers connected through the same Docker network.

The project provides a `Dockerfile` in `CoffeeNChill.Functions/` for building the Azure Functions container.

From the repository root, build the Functions image:

```bash
cd CoffeeNChill.Functions
docker build -t mmolokik/coffeenchill-functions:v1.0 .
```

Return to the repository root:

```bash
cd ..
```

Run the Functions container on the same network as Azurite:

```bash
docker run -d --name coffeenchill-functions --network coffeenchill-net \
  -p 7071:80 \
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;" \
  mmolokik/coffeenchill-functions:v1.0
```

The `Dockerfile` exposes port `80` inside the container, which is mapped to port `7071` on the host.

> **Part 1 requirement:** The application is demonstrated using standalone Docker containers run independently. Docker Compose is not used as the Part 1 execution method.

> **Note:** The repository also contains a `docker-compose.yml`. This is scaffolding for Part 2 (**Queue Triggers & Docker Compose Orchestration**) and is not used for Part 1. Part 1 specifically requires standalone containers run independently, without orchestration tools.

## 6. Verify the Containers

Check that both standalone containers are running:

```bash
docker ps
```

The output should show:

- `azurite`
- `coffeenchill-functions`

View the Functions application logs:

```bash
docker logs coffeenchill-functions --tail 50
```

View the Azurite logs:

```bash
docker logs azurite --tail 50
```

These logs can help identify startup problems and Azure Storage errors.

## 7. API Endpoints

When the Functions container is running on port `7071`, the API is available at:

```text
http://localhost:7071
```

### Menu Endpoints

Use the menu endpoints provided by the project to create, retrieve, update, and delete menu items.

### Document Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/documents/upload` | Upload a staff document |
| `GET` | `/api/documents` | List staff documents |
| `GET` | `/api/documents/download/{fileName}` | Download a staff document |

---

## 8. Testing with Postman

Open Postman and import the collection included in the `docs` directory:

```text
CLDV6212 Part 1 - CoffeeNChill.postman_collection.json
```

Use the collection to test the CoffeeNChill API.

### Document Upload

1. Select the document upload request.
2. Use `POST`.
3. Select **Body → form-data**.
4. Add a field named `file`.
5. Set the field type to **File**.
6. Select a supported document.
7. Send the request.

Supported document extensions:

- `.pdf`
- `.txt`
- `.docx`
- `.xlsx`

The maximum supported upload size is **50 MB**.

---

## 9. Testing Document Operations

### 9.1 Upload a Document

Send a `multipart/form-data` request to:

```text
POST http://localhost:7071/api/documents/upload
```

The request must contain a `file` form-data field.

**Expected successful response:** `201 Created`

### 9.2 List Documents

Send:

```text
GET http://localhost:7071/api/documents
```

**Expected successful response:** `200 OK`

The response contains information about the stored documents.

### 9.3 Download a Document

Send:

```text
GET http://localhost:7071/api/documents/download/{fileName}
```

Replace `{fileName}` with the name of the stored document.

**Expected successful response:** `200 OK`

If the document does not exist, the API returns:

```text
404 Not Found
```

---

## 10. Troubleshooting

### Functions Container Does Not Start

Check the container logs:

```bash
docker logs coffeenchill-functions --tail 50
```

Check whether the containers are running:

```bash
docker ps -a
```

### Azurite Connection Errors

Confirm that Azurite is running:

```bash
docker ps
```

Inspect the shared Docker network:

```bash
docker network inspect coffeenchill-net
```

Both `azurite` and `coffeenchill-functions` should be connected to `coffeenchill-net`.

### Port Already in Use

Check whether another process or container is using:

- Port `7071`
- Port `10000`
- Port `10001`
- Port `10002`

For Docker containers, use:

```bash
docker ps
```

Stop an existing container if necessary:

```bash
docker stop <container-name>
```

### Document Upload Fails

Check that:

- The request uses `multipart/form-data`.
- The form field is named `file`.
- The file extension is supported.
- The file is not larger than 50 MB.
- Azurite is running.
- Both containers are connected to `coffeenchill-net`.
- The Functions container can communicate with Azurite.

### Storage Errors

Check the Functions logs:

```bash
docker logs coffeenchill-functions --tail 50
```

Check the Azurite logs:

```bash
docker logs azurite --tail 50
```

The document service records successful operations and Azure Storage exceptions using application logging.

## 11. Useful Docker Commands

### Create the Shared Network

```bash
docker network create coffeenchill-net
```

### Start Azurite

```bash
docker run -d --name azurite --network coffeenchill-net \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mmolokik/coffeenchill-azurite:v1.0
```

### Build the Functions Image

```bash
cd CoffeeNChill.Functions
docker build -t mmolokik/coffeenchill-functions:v1.0 .
cd ..
```

### Start the Functions Container

```bash
docker run -d --name coffeenchill-functions --network coffeenchill-net \
  -p 7071:80 \
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;" \
  mmolokik/coffeenchill-functions:v1.0
```

### List Running Containers

```bash
docker ps
```

### View Functions Logs

```bash
docker logs coffeenchill-functions --tail 50
```

### View Azurite Logs

```bash
docker logs azurite --tail 50
```

### Inspect the Shared Network

```bash
docker network inspect coffeenchill-net
```

### Stop a Container

```bash
docker stop <container-name>
```

### Remove a Container

```bash
docker rm <container-name>
```

### Stop Azurite

```bash
docker stop azurite
```

### Stop the Functions Container

```bash
docker stop coffeenchill-functions
```

## 12. Quick Setup Summary

Follow this sequence for the required Part 1 standalone Docker setup:

```text
1. Clone repository
        ↓
2. Create the shared Docker network
        ↓
3. Start the standalone Azurite container
        ↓
4. Build the Functions Docker image
        ↓
5. Start the standalone Functions container on the same network
        ↓
6. Verify both containers are running
        ↓
7. Open the API at http://localhost:7071
        ↓
8. Import the Postman collection
        ↓
9. Test menu and document endpoints
        ↓
10. Check Docker logs if errors occur
```

For the required Part 1 setup, run the two services as standalone containers using `docker run`; do not use Docker Compose.

## 13. Summary

This setup provides a repeatable local development and testing environment for the CoffeeNChill application using:

- **.NET 8 Azure Functions**
- **Azurite**
- **Docker**
- **Azure Table Storage emulation**
- **Azure Blob Storage emulation**
- **Postman**

The setup supports development, API testing, document sharing, and troubleshooting without requiring a live Azure Storage environment for local testing.
