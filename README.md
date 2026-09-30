# CoffeeNChill

## Project Overview

CoffeeNChill is a cloud-native canteen management backend built for CLDV6212 Part 1. It replaces paper menus and filing-cabinet documents with:

- A digital menu stored in **Azure Table Storage**, managed via HTTP-triggered Azure Functions
- Staff operational documents stored in **Azure Blob Storage**, with upload/list/download endpoints
- Everything running locally against **Azurite** and containerized with **Docker**

## Architecture

See [`docs/architecture.md`](docs/architecture.md) for the full breakdown. Short version: Azure Functions isolated worker (.NET 8), Table Storage for menu items, Blob Storage for staff documents, both resolved via dependency injection from the `AzureWebJobsStorage` environment variable so the same image runs locally and inside Docker unchanged.

> **Note on storage choice:** staff documents use Blob Storage rather than Azure Files. Azurite doesn't emulate the Files service at all, so a File Share implementation could never be verified locally. This was confirmed with the lecturer as an error in the brief — Blob Storage is the correct, expected implementation.

## Setup

See [`docs/setup-guide.md`](docs/setup-guide.md) for full prerequisites, local (non-Docker) setup, and troubleshooting. Quick start with Docker:

```bash
git clone https://github.com/EMGPMD/cldv6212-group-1-poe-part-1-st10457044-tshiamo-aphane.git
cd cldv6212-group-1-poe-part-1-st10457044-tshiamo-aphane

docker network create coffeenchill-net

docker run -d --name azurite --network coffeenchill-net \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mmolokik/coffeenchill-azurite:v1.0

docker run -d --name coffeenchill-functions --network coffeenchill-net \
  -p 7071:80 \
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;" \
  mmolokik/coffeenchill-functions:v1.0
```

API available at `http://localhost:7071/api/...`.

> **Part 1 requirement:** the application is run as two standalone Docker containers, independently, without orchestration tools. A `docker-compose.yml` also exists in this repo — it's scaffolding for Part 2 (Queue Triggers & Docker Compose Orchestration) and is not used for this submission.

## Docker Hub

- **Functions:** [`mmolokik/coffeenchill-functions:v1.0`](https://hub.docker.com/r/mmolokik/coffeenchill-functions)
- **Azurite:** [`mmolokik/coffeenchill-azurite:v1.0`](https://hub.docker.com/r/mmolokik/coffeenchill-azurite)

## API Endpoints

| Method | Route                                | Auth      | Notes                                                                                        |
| ------ | ------------------------------------ | --------- | -------------------------------------------------------------------------------------------- |
| POST   | `/api/menu`                          | Anonymous | Create menu item                                                                             |
| GET    | `/api/menu`                          | Anonymous | Get all menu items                                                                           |
| GET    | `/api/menu/category/{category}`      | Anonymous | Get menu items by category                                                                   |
| PUT    | `/api/menu/{category}/{sku}`         | Anonymous | Update a menu item                                                                           |
| DELETE | `/api/menu/{category}/{sku}`         | Anonymous | Delete a menu item                                                                           |
| POST   | `/api/documents/upload`              | Anonymous | Upload staff document (multipart/form-data, `file` field, max 50MB, `.pdf/.txt/.docx/.xlsx`) |
| GET    | `/api/documents`                     | Anonymous | List staff documents                                                                         |
| GET    | `/api/documents/download/{fileName}` | Anonymous | Download a staff document                                                                    |

## Postman Collection

<!-- TODO: finalise once Tshiamo's environment file (COF-6) and full Docker-container run (COF-7) land -->

`docs/CLDV6212 Part 1 - CoffeeNChill.postman_collection.json`, with a companion environment file setting `baseUrl` to `http://localhost:7071`. Import both, select the environment, run against the running Docker containers.

## Team & Roles

| Person   | Component           | Responsibility                                                                                      |
| -------- | ------------------- | --------------------------------------------------------------------------------------------------- |
| Refentse | Menu CRUD           | Azure Table Storage schema, 5 HTTP CRUD functions                                                   |
| Aksi     | Document Storage    | Blob Storage integration, upload/list/download functions, logging & error handling                  |
| Mmoloki  | Docker & Submission | Dockerfiles, image builds, Docker Hub publishing, DI/config fixes, README, demo video, coordination |
| Tshiamo  | Testing             | Postman collection and environment covering all endpoints                                           |

## Demo Video

https://youtu.be/RBIhAF9Nv_0
