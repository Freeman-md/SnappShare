# SnappShare API

SnappShare is a secure, scalable backend API built with **ASP.NET Core 8** that supports **chunked file uploads**, **resumable sessions**, **media streaming**, and **automated file expiration**. Designed for cloud-native environments, it integrates tightly with **Azure Blob Storage**, **Azure Service Bus**, and **Cosmos DB**.

---

## ✨ Features

- 🔐 Secure file upload and session handling
- 📦 Chunked uploads with resumable support
- 🚀 Finalization and secure download via SAS token
- 🧼 Automated file cleanup (via message queue)
- 🧠 Built with separation of concerns (Service, Repository, Controller layers)
- ☁️ Azure-first architecture (Blob Storage, Service Bus, Cosmos DB)
- 🔧 Environment-based config for local or cloud setups

---

## 📐 Project Structure

```bash
api/
├── Controllers/           # API endpoints
├── Services/              # Business logic
├── Interfaces/            # Service and Repository contracts
├── Repositories/          # Data access layers (EF Core)
├── DTOs/                  # Data Transfer Objects
├── Models/                # Domain models (FileEntry, Chunk, etc.)
├── Enums/                 # Custom enums like UploadStatus, ExpiryDuration
├── Configs/               # Bound options for Storage, ServiceBus
├── Middlewares/          # Global exception handler
├── Data/                 # EF DbContext setup
├── Extensions/           # DI extension methods
└── Program.cs            # Startup & configuration logic
```

---

## 🌐 Endpoints

All endpoints are prefixed with `/file-entry`:

- `POST /create` – create a new file entry (metadata)
- `POST /handle-upload` – handles incoming chunk + manages state
- `POST /{fileId}/upload` – uploads a single chunk
- `POST /{fileId}/finalize` – finalizes upload, returns download URL
- `GET /{fileId}` – gets status of a file entry

---

## 🌐 Live App
👉 [Check it out](https://snappshare-web.vercel.app)

---

## ⚙️ Local Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- SQL Server (or use LocalDB)
- `dotnet-ef` installed globally
- Azure Storage Account, Service Bus, and optionally Cosmos DB
- A valid Azure AD **Service Principal** for local authentication (used for Blob access)

> ⚠️ You will need to create and configure various **Azure services** and a **Service Principal** to access them locally. When deployed to Azure, access is handled via **Managed Identity**.


### Setup
1. Add your Azure values to `.env`:
```
AZURE_CLIENT_ID=...
AZURE_TENANT_ID=...
AZURE_CLIENT_SECRET=...
```

2. Add connection string and other app settings to `appsettings.json` (for local only):
```json
"Storage": {
  "AccountName": "<account-name>",
  "ContainerName": "<container-name>"
},
"ServiceBus": {
  "NamespaceName": "<namespace-name>",
  "QueueName": "<queue-name>"
},
"ConnectionStrings": {
  "Snappshare": "Server=...;Initial Catalog=...;Authentication=Active Directory Default;"
}
```

3. Run the API:
```
dotnet run
```

---

## ☁️ Azure Deployment

This app is designed to be hosted on **Azure App Service** with the following services configured:

- **Azure Blob Storage** – for file chunk storage
- **Azure Service Bus** – for delayed deletion tasks
- **Cosmos DB** – optional metadata persistence
- **Managed Identity or Service Principal** – required for secure access to Azure services

Make sure to:
- Assign the right role to the App Service's managed identity (e.g., Storage Blob Data Contributor)
- Upload your `publish profile` to GitHub Actions Secrets
- Set required env variables in Azure App Service Configuration

---

## 🚀 GitHub Actions CI/CD

A GitHub Actions workflow is configured to:

1. Build & test the API
2. Publish artifacts
3. Deploy to Azure Web App

You must:
- Use `AZUREAPPSERVICE_PUBLISHPROFILE` as a GitHub secret
- Keep within artifact storage quota limits to avoid deployment errors

---

## 🧪 Testing

Tests live in `api.Tests` and cover service-level logic. More tests (controller & integration) will be added.

Run tests:
```bash
dotnet test
```

---

## ✅ Notes

- File entries are cleaned up automatically based on expiry (1 min to 1 day).
- Chunking and hash checks are built-in for upload reliability.
- Uploads can resume from where they stopped.

---

## 🧠 Inspiration
Built with real-world reliability and scalability in mind, optimized for developers needing resumable uploads on flaky connections.

---

## 🧠 Future Plans
- 📥 File download analytics
- 🧾 Download receipts for time tracking
- 🔑 Optional OTP/email-secured access
---

## 💬 Feedback?
I’d love to hear from you — what features would you like next?

- 🧵 [Twitter (X)](https://x.com/freemancodz)
- 💼 [LinkedIn](https://www.linkedin.com/in/freeman-madudili-9864101a2/)

---

SnappShare — Fast. Simple. Secure.
