# Logos.AI - RAG Knowledge Base

Logos.AI is a **Retrieval-Augmented Generation (RAG)** system built with **.NET 9**. It allows users to upload PDF documents, index them using OpenAI embeddings, and perform semantic search to find relevant information within the documents.

## 🚀 Features

*   **📄 PDF Ingestion:** Upload PDF documents via the web interface.
*   **🧩 Smart Chunking:** Automatically splits text into chunks while preserving page numbers.
*   **🧠 Semantic Search:** Uses OpenAI Embeddings to find the most relevant context for a user's query.
*   **⚡ Vector Database:** Utilizes **Qdrant** for high-speed vector similarity search.
*   **💾 Hybrid Storage:**
    *   **SQLite:** Stores raw document text, metadata, and upload history ("Cold Storage").
    *   **Qdrant:** Stores vectors and payloads for search ("Hot Storage").
*   **UI:** Simple ASP.NET MVC interface for uploading and searching.

## 🛠️ Tech Stack

*   **Framework:** .NET 9 (ASP.NET Core Web API / MVC)
*   **Vector DB:** Qdrant
*   **AI Model:** OpenAI (`text-embedding-3-small`)
*   **Database:** SQLite (Entity Framework Core)
*   **Architecture:** Modular (API, Engine, Abstractions)

## ⚙️ Getting Started

### 1. Prerequisites

*   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   [Docker](https://www.docker.com/) (for running Qdrant)
*   OpenAI API Key

### 2. Run Qdrant (Vector DB)

Run the following command to start Qdrant in a Docker container:

```bash
docker run -p 6333:6333 -p 6334:6334 \
    -v $(pwd)/qdrant_storage:/qdrant/storage:z \
    qdrant/qdrant
```

### 3. Configuration

Update `Logos.AI.API/appsettings.json` with your settings:

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-...",
    "Model": "gpt-4o-mini" 
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": "6334"
  },
  "ConnectionStrings": {
    "LogosDatabase": "Data Source=logos.db"
  },
  "Rag": {
    "ChunkSizeWords": 300,
    "ChunkOverlapWords": 50,
    "TopK": 5
  }
}
```

### 4. Database Migration

Ensure the SQLite database is created:

```bash
cd Logos.AI.API
dotnet ef database update
```

### 5. Run the Application

```bash
dotnet run
```

Navigate to `http://localhost:5000/rag/index` (or the port specified in your launch profile).

## 🏗️ Architecture Overview

1.  **Upload:** User uploads a PDF -> System extracts text -> Splits into chunks.
2.  **Indexing:**
    *   Chunks are saved to **SQLite** (for record-keeping).
    *   Chunks are sent to **OpenAI** to generate Embeddings (Vectors).
    *   Vectors + Metadata (Page #, Filename) are upserted to **Qdrant**.
3.  **Search:**
    *   User asks a question.
    *   Question is converted to a Vector (via OpenAI).
    *   **Qdrant** performs a Cosine Similarity search.
    *   Relevant text chunks are returned and displayed to the user.

## 📝 License

[MIT](LICENSE)
