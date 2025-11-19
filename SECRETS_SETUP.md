# Secrets Configuration Guide

## Local Development Setup

**1. Initialize user secrets:**
```bash
dotnet user-secrets init
```

**2. Add your API keys:**
```bash
# Azure Search API Key
dotnet user-secrets set "AzureSearch:ApiKey" "your-search-api-key"

# Azure OpenAI API Key
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-openai-api-key"
```

**3. Verify secrets:**
```bash
dotnet user-secrets list
```

**4. Run the application:**
```bash
dotnet run
```

## GitHub Secrets (For CI/CD)

**1. Go to repository settings:**
```
https://github.com/MariaBTV/ads_monotlith_app/settings/secrets/actions
```

**2. Add these secrets:**
- `AZURE_SEARCH_API_KEY` - Your Azure Search admin key
- `AZURE_OPENAI_API_KEY` - Your Azure OpenAI key

**3. Use in GitHub Actions workflow:**
```yaml
- name: Run tests
  env:
    AzureSearch__ApiKey: ${{ secrets.AZURE_SEARCH_API_KEY }}
    AzureOpenAI__ApiKey: ${{ secrets.AZURE_OPENAI_API_KEY }}
  run: dotnet test
```

## How It Works

- **Local Development**: User secrets are stored in `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`
- **GitHub Actions**: Environment variables override appsettings values
- **Production**: Use Azure Key Vault or App Service Configuration

## Security Notes

✅ **DO:**
- Use user secrets for local development
- Store secrets in GitHub Secrets for CI/CD
- Use Azure Key Vault for production
- Rotate keys regularly

❌ **DON'T:**
- Commit API keys to source control
- Share user secrets files
- Use production keys in development
