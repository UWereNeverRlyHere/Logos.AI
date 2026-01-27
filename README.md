# Logos.AI — Інтелектуальний Медичний Асистент

**Logos.AI** — це RAG (Retrieval-Augmented Generation) система, розроблена для семантичного пошуку по медичних протоколах та настановах. Проект використовує сучасні методи NLP (Natural Language Processing) та векторного пошуку для надання лікарям точних відповідей із посиланнями на першоджерела.

## 🚀 Основні Можливості

* **Smart Document Ingestion:** Завантаження PDF-документів з автоматичним розпізнаванням структури.
* **Intelligent Chunking:** Унікальний алгоритм нарізки тексту, який зберігає цілісність речень та абзаців (на відміну від звичайного поділу за кількістю слів), що критично важливо для медичного контексту.
* **Semantic Search:** Пошук за змістом, а не лише за ключовими словами. Система розуміє, що "біль у животі" та "абдомінальний синдром" — це пов'язані речі.
* **Precision Filtering:** Використання порогу впевненості (`Confidence Score`) та точного пошуку (`Exact Search`), щоб уникнути "галюцинацій" та нерелевантних відповідей.
* **Evidence-Based:** Кожна знайдена відповідь містить посилання на конкретний документ та сторінку.

## 🛠 Технологічний Стек

### Backend
* **Core:** .NET 9 (ASP.NET Core MVC)
* **Architecture:** Clean Architecture (API, Engine, Abstractions)
* **Database:** PostgreSQL / MSSQL (через EF Core) для метаданих.

### AI & Data
* **Embeddings:** OpenAI `text-embedding-3-small` (1536 dimensions).
* **Vector DB:** Qdrant (зберігання векторів та payload).
* **PDF Processing:** PdfPig (для витягування тексту та координат).

## ⚙️ Налаштування (appsettings.json)

Система використовує гнучкий паттерн Options для налаштування параметрів RAG без перекомпіляції:

```json
{
  "Rag": {
    "ChunkSizeWords": 300,    // Цільовий розмір блоку тексту
    "ChunkOverlapWords": 50,  // Перекриття для збереження контексту
    "MinScore": 0.5,          // Поріг схожості (фільтр шуму)
    "TopK": 5                 // Кількість результатів
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "logos_knowledge_base"
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "EmbeddingModel": "text-embedding-3-small"
  }
}