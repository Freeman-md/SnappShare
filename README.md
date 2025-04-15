# SnappShare

SnappShare is a lightweight, secure file-sharing solution built with Azure and chunked, resumable uploads. It lets you upload large files from your phone or computer and generate a time-limited, secure link using Azure SAS tokens.

---

## 🚀 Why SnappShare?

One time, I needed to share a large file from my phone. I didn’t want to sign up for anything — just upload and send. But with network instability, every failed attempt meant starting over. That got me thinking:

**What if you could upload big files safely — and not worry about network drops or data loss?**

SnappShare solves that.

---

## ⚙ How It Works

1. 📤 **Chunk Uploads**: Files are uploaded in chunks (starting from 1MB, up to 5MB max per chunk). This means uploads resume from where they stopped — no restarts needed.
2. 🔐 **Secure Access**: Files are served via a time-limited [SAS Token](https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview) URL — only valid for the specified time.
3. 🧹 **Scheduled Deletion**: Once an upload is complete, a deletion message is scheduled via Azure Service Bus. An Azure Function listens and deletes the file at the right time.

---

## 🧱 Architecture (Refined MVP)

### Frontend
- Built with **React.js** (Vite + TypeScript + Tailwind + Shadcn/UI)

### Backend
- ASP.NET Core Web API (v8)
- Chunked uploads handled via endpoints
- Files stored in Azure Blob Storage
- SQL Database stores file metadata

### Identity & Access
- 🔒 Secure Azure access using:
  - **Service Principal** in local development
  - **Managed Identity** in production (App Service)
- Used for connecting to Blob Storage, SQL DB, and Service Bus

### Cloud Services
- **Azure Blob Storage** – file storage with lifecycle rules
- **Azure SQL Database** – stores file and chunk metadata
- **Azure Service Bus** – used internally for chunk coordination

### Deployment
- GitHub Actions CI/CD to **Azure App Service**
- Live logs via Azure Log Stream (App Service > Logs > Stream)

---

## 📌 API Summary

### `POST /file-entry/handle-upload`
Handles chunk upload. Accepts:
- `fileName`, `fileHash`, `chunkIndex`, `totalChunks`, `fileSize`, `chunkHash`, `expiresIn`, `chunkFile`

Returns:
- Upload status
- Final secure file URL (when all chunks uploaded)

---

## 🌐 Live App
👉 [Check it out](https://snappshare.vercel.app)

---

## 📸 Screenshots

- ✅ Upload interface (React)
- 🔒 SAS Token logic
- 🌩️ Azure Storage Blob view
- 🔄 Retry/resume logic for chunked files

---

## 📂 Running Locally

### Requirements
- .NET 8
- Azure Blob Storage + SQL + Service Principal credentials

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

## 🧠 Future Plans
- ⏫ Drag and drop multiple files
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
