# Quick Start Guide - AI Copilot Feature

## ⚡ Fast Track to Running AI Assistant

### Prerequisites Check
- ✅ SQL Server running (container: sqlserver on port 1433)
- ✅ .NET 9.0 SDK installed
- ✅ Application builds successfully
- ⏳ Azure OpenAI credentials (follow setup below)

---

## 🚀 3-Step Setup (25 minutes)

### Step 1: Create Azure OpenAI (15 min)

**Option A: Azure Portal (GUI)**
```
1. Go to https://portal.azure.com
2. Create resource → Search "Azure OpenAI" → Create
3. Fill in:
   - Resource group: (new or existing)
   - Region: East US
   - Name: retailmonolith-openai
   - Pricing: Standard S0
4. Deploy model:
   - Open Azure OpenAI Studio
   - Deployments → Create new deployment
   - Model: gpt-4o
   - Name: chat-model
```

**Option B: Azure CLI (Fast)**
```powershell
# Login
az login

# Create resource
az cognitiveservices account create `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup `
  --kind OpenAI `
  --sku S0 `
  --location eastus

# Deploy model
az cognitiveservices account deployment create `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup `
  --deployment-name chat-model `
  --model-name gpt-4o `
  --model-version "2024-05-13" `
  --model-format OpenAI `
  --sku-name "Standard" `
  --sku-capacity 30
```

### Step 2: Get Credentials (2 min)

**Portal Method:**
```
1. Go to your Azure OpenAI resource
2. Click "Keys and Endpoint"
3. Copy:
   - Endpoint URL
   - Key 1
```

**CLI Method:**
```powershell
# Get endpoint
az cognitiveservices account show `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup `
  --query properties.endpoint `
  --output tsv

# Get key
az cognitiveservices account keys list `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup `
  --query key1 `
  --output tsv
```

### Step 3: Configure Application (3 min)

```powershell
# Navigate to project directory
cd c:\Users\rredgrave\code\ads_monotlith_app

# Set secrets
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE-NAME.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR-API-KEY-HERE"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "chat-model"

# Verify secrets
dotnet user-secrets list
```

---

## 🎯 Run & Test (5 minutes)

### Start Application
```powershell
# Ensure SQL Server is running
docker ps | Select-String "sqlserver"

# Run application
dotnet run
```

### Test AI Copilot
1. Open: **http://localhost:5068**
2. Click: **"AI Assistant"** (robot icon in nav)
3. Test queries:
   ```
   "Show me laptops under $1000"
   "I need a smartphone with good camera"
   "What gaming laptops do you have?"
   "Budget tablets around $300"
   ```

### Expected Results
- ✅ Chat interface loads
- ✅ AI responds with product recommendations
- ✅ Products show in right panel with details
- ✅ Can click "Add to Cart"
- ✅ Conversation history maintained
- ✅ Responses within 3-5 seconds

---

## 🐛 Troubleshooting

### Issue: "Azure OpenAI endpoint not configured"
**Solution:**
```powershell
dotnet user-secrets list
# Verify all 3 settings are present
# Re-run Step 3 if missing
```

### Issue: Build errors
**Solution:**
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Issue: SQL Server not running
**Solution:**
```powershell
docker start sqlserver
# Wait 10 seconds
docker ps
```

### Issue: AI returns empty recommendations
**Check:**
1. Database has products: `SELECT COUNT(*) FROM Products`
2. API key is valid
3. Deployment name matches: "chat-model"
4. Check logs: Look for Azure OpenAI errors

### Issue: 429 Rate Limit errors
**Solution:**
- Wait 60 seconds between requests
- Increase TPM (Tokens Per Minute) in Azure deployment
- Consider upgrading pricing tier

---

## 📊 Verification Checklist

Before considering complete:
- [ ] Application builds without errors
- [ ] SQL Server container running
- [ ] Azure OpenAI resource created
- [ ] Model deployed (gpt-4o or gpt-35-turbo)
- [ ] User secrets configured (3 values)
- [ ] Application runs (dotnet run)
- [ ] AI Assistant link visible in navigation
- [ ] Chat page loads
- [ ] AI responds to test query
- [ ] Recommendations appear
- [ ] Add to Cart button works
- [ ] No console errors

---

## 💰 Cost Estimates

### Azure OpenAI Pricing (Pay-as-you-go)
- **gpt-4o**: ~$5/1M input tokens, ~$15/1M output tokens
- **gpt-35-turbo**: ~$0.50/1M input tokens, ~$1.50/1M output tokens

### Estimated Usage
- Average query: 500 input tokens + 400 output tokens
- gpt-4o: ~$0.008 per query
- gpt-35-turbo: ~$0.001 per query

### Daily Estimates (100 queries/day)
- gpt-4o: ~$0.80/day (~$24/month)
- gpt-35-turbo: ~$0.10/day (~$3/month)

**Recommendation:** Start with gpt-35-turbo for testing

---

## 🎓 Usage Tips

### Good Queries (Get Best Results)
✅ "Show me laptops under $1000"
✅ "I need a professional camera"
✅ "What budget smartphones do you have?"
✅ "Gaming laptop with good graphics"

### Poor Queries (May Get Generic Responses)
❌ "Hi" / "Hello"
❌ "Help"
❌ "What can you do?"

### Advanced Queries
💡 "I'm a photographer. Recommend a camera and accessories"
💡 "I need a laptop for video editing under $2000"
💡 "Compare your best tablets"
💡 "What's the difference between these phones?"

---

## 🔒 Security Notes

- ✅ API keys stored in user secrets (not in code)
- ✅ User secrets excluded from git
- ✅ Antiforgery tokens on all POST requests
- ✅ HTML escaping prevents XSS
- ✅ No sensitive data in logs

---

## 📞 Support

### Check Logs
```powershell
# Application logs show in console
# Look for:
# - "Calling Azure OpenAI with X messages"
# - "Received response from Azure OpenAI"
# - Error messages with stack traces
```

### Useful Azure CLI Commands
```powershell
# List deployments
az cognitiveservices account deployment list `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup

# Check resource status
az cognitiveservices account show `
  --name retailmonolith-openai `
  --resource-group YourResourceGroup `
  --query properties.provisioningState
```

---

## ✅ Success Criteria Met

- ✅ Code implementation complete
- ✅ All files created and configured
- ✅ Build successful (0 errors)
- ✅ UI components ready
- ✅ API integration implemented
- ✅ Error handling in place
- ✅ Documentation complete

**Status: Implementation Complete - Awaiting Azure Credentials**
