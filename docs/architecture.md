# CoffeeNChill System Architecture

## 1. Overview

CoffeeNChill is a cloud-based coffee shop management application built using Azure Functions and .NET 8 isolated worker. The system provides functionality for managing the coffee shop menu and sharing staff documents.

The application is designed to run locally using Azurite for Azure Storage emulation and can also be containerised using Docker.

## 2. Architecture Components

The main components of the CoffeeNChill system are:

- **Azure Functions (.NET 8 isolated worker)** — Provides the HTTP API endpoints and application logic.
- **Azure Table Storage** — Stores and manages coffee shop menu information.
- **Azure Blob Storage** — Stores staff documents such as PDF, TXT, DOCX and XLSX files.
- **Azurite** — Provides local emulation of Azure Storage services during development and testing.
- **Docker** — Containerises the Functions application and Azurite services for consistent local deployment.
- **Postman** — Used to test the HTTP API endpoints.

## 3. Menu Management

The menu functionality uses Azure Table Storage to persist menu items.

Each menu item is represented as an entity containing information such as:

- Item name
- Category
- Price
- Description
- Availability

The Functions application communicates with Azure Table Storage through the application's menu service.

## 4. Staff Document Sharing

The staff document functionality uses Azure Blob Storage.

Documents are uploaded through the HTTP API using a multipart/form-data request. The application validates the file name, extension and size before storing the document.

Supported document types include:

- `.pdf`
- `.txt`
- `.docx`
- `.xlsx`

The application limits uploads to 50 MB.

The document API provides endpoints for:

- Uploading staff documents
- Listing stored documents
- Downloading staff documents

Blob Storage is used instead of Azure Files because the local Azurite environment used by this project does not provide the required Azure Files emulation. Azure Blob Storage therefore provides a suitable storage mechanism for the staff document-sharing functionality while remaining testable locally.

## 5. Application Flow

A typical request follows this flow:

1. A client sends an HTTP request to the Azure Functions API.
2. The appropriate Function endpoint receives the request.
3. The Function validates the request.
4. The relevant service communicates with Azure Storage.
5. The storage operation succeeds or an exception is handled and logged.
6. The Function returns an appropriate HTTP response to the client.

For document operations, logging is provided at both the storage-service and HTTP-function layers to assist with diagnosing storage failures and unexpected errors.

## 6. Data Storage

The application uses different Azure Storage services for different types of data:

| Data | Storage Service | Purpose |
|---|---|---|
| Menu items | Azure Table Storage | Stores structured menu data |
| Staff documents | Azure Blob Storage | Stores uploaded document files |

This separation allows each type of information to use a storage service appropriate to its purpose.

## 7. Local Development Architecture

During local development, Azure Storage dependencies are provided by Azurite.

The Functions application connects to Azurite using the development storage connection string:

`UseDevelopmentStorage=true`

Docker can be used to run the Functions application and Azurite in containers. This provides a reproducible local environment without requiring a live Azure subscription for development and testing.

## 8. Containerisation

The project includes Docker configuration for running the application in a containerised environment.

The containerised architecture consists of:

- CoffeeNChill Functions container
- Azurite storage container

The Functions container communicates with the Azurite container over the Docker network. The storage connection configuration allows the application to use the Azurite services for Table and Blob Storage operations.

## 9. Error Handling and Logging

The document storage service uses `ILogger` to record important storage events.

Logging includes:

- Successful document uploads
- Successful document listings
- Successful document downloads
- Missing document requests
- Azure storage failures
- Unexpected errors at the HTTP Function layer

Azure Storage `RequestFailedException` errors are logged with their underlying exception information. The HTTP Functions catch unexpected exceptions and return HTTP 500 responses rather than allowing failures to surface as unhandled exceptions.

## 10. Testing

The API can be tested using Postman.

The main document endpoints are:

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/documents/upload` | Upload a staff document |
| GET | `/api/documents` | List staff documents |
| GET | `/api/documents/download/{fileName}` | Download a staff document |

Menu endpoints can also be tested through the HTTP API using the project's Postman collection.

## 11. Architecture Summary

The overall architecture separates the application logic, API layer and storage layer.

```text
                 Client / Postman
                        |
                        v
              Azure Functions API
                        |
             +----------+----------+
             |                     |
             v                     v
        Menu Service        Document Service
             |                     |
             v                     v
      Azure Table Storage    Azure Blob Storage
             |                     |
             +----------+----------+
                        |
                     Azurite
                 (Local Testing)