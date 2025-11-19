# Semantic Search Implementation Guide

## Step 1: Azure OpenAI Setup

### 1.1 Verify Deployment
Your current config shows:
- Endpoint: `https://demo-msft-sdc-oai.openai.azure.com/`
- Model: `text-embedding-3-large`
- Dimensions: 1536

**Test the deployment:**
```bash
curl -X POST "https://demo-msft-sdc-oai.openai.azure.com/openai/deployments/text-embedding-3-large/embeddings?api-version=2024-02-15-preview" \
  -H "Content-Type: application/json" \
  -H "api-key: YOUR_API_KEY" \
  -d '{"input": "test", "model": "text-embedding-3-large"}'
```

Expected response: JSON with `data[0].embedding` array of 1536 floats.

---

## Step 2: Azure AI Search Index Setup

### 2.1 Delete Existing Index (if any issues)
```bash
DELETE https://demo-msft-sdc-retail-srch.search.windows.net/indexes/products-semantic?api-version=2024-07-01
api-key: YOUR_SEARCH_ADMIN_KEY
```

### 2.2 Create New Index with Vector + Semantic Support

**IMPORTANT: Use this exact schema**

```json
PUT https://demo-msft-sdc-retail-srch.search.windows.net/indexes/products-semantic?api-version=2024-07-01
Content-Type: application/json
api-key: YOUR_SEARCH_ADMIN_KEY

{
  "name": "products-semantic",
  "fields": [
    {
      "name": "id",
      "type": "Edm.String",
      "key": true,
      "filterable": true
    },
    {
      "name": "name",
      "type": "Edm.String",
      "searchable": true,
      "filterable": true,
      "sortable": true
    },
    {
      "name": "description",
      "type": "Edm.String",
      "searchable": true
    },
    {
      "name": "category",
      "type": "Edm.String",
      "searchable": true,
      "filterable": true,
      "facetable": true
    },
    {
      "name": "price",
      "type": "Edm.Double",
      "filterable": true,
      "sortable": true
    },
    {
      "name": "contentVector",
      "type": "Collection(Edm.Single)",
      "searchable": true,
      "dimensions": 1536,
      "vectorSearchProfile": "vector-profile"
    }
  ],
  "semantic": {
    "configurations": [
      {
        "name": "semantic-config",
        "prioritizedFields": {
          "titleField": {
            "fieldName": "name"
          },
          "prioritizedContentFields": [
            {
              "fieldName": "description"
            },
            {
              "fieldName": "category"
            }
          ]
        }
      }
    ]
  },
  "vectorSearch": {
    "profiles": [
      {
        "name": "vector-profile",
        "algorithm": "vector-algorithm"
      }
    ],
    "algorithms": [
      {
        "name": "vector-algorithm",
        "kind": "hnsw",
        "hnswParameters": {
          "m": 4,
          "efConstruction": 400,
          "metric": "cosine"
        }
      }
    ]
  }
}
```

### 2.3 Verify Index Creation
```bash
GET https://demo-msft-sdc-retail-srch.search.windows.net/indexes/products-semantic?api-version=2024-07-01
api-key: YOUR_SEARCH_ADMIN_KEY
```

You should see the index with all fields including `contentVector` with `dimensions: 1536`.

---

## Step 3: Code Changes

The updated `SemanticSearchService.cs` will:
1. Skip index creation (you created it via REST)
2. Generate embeddings using Azure OpenAI
3. Index products with vectors
4. Query using hybrid vector + semantic search

---

## Step 4: Testing

After running `dotnet run`:
1. Navigate to http://localhost:5000/Products (or your configured port)
2. Try searches:
   - "running shoes"
   - "comfortable footwear"
   - "wireless headphones"
3. Check that results are semantically relevant

---

## Troubleshooting

**Error: "property does not exist"**
- Ensure index was created with exact field names
- Delete and recreate index if needed

**Error: "429 Too Many Requests"**
- Add retry logic or slow down embedding generation

**Error: "dimensions mismatch"**
- Verify embedding model outputs 1536 dimensions
- Match `dimensions` in index schema

**No results returned**
- Check if products were indexed: 
  ```
  GET https://demo-msft-sdc-retail-srch.search.windows.net/indexes/products-semantic/docs/$count?api-version=2024-07-01
  ```
