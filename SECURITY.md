# Security Policy

## 🔒 Reporting Security Vulnerabilities

If you discover a security vulnerability in this project, please report it by emailing **[your-email@example.com]**. Please do not open a public issue.

We take all security reports seriously and will respond as quickly as possible.

---

## 🛡️ Security Best Practices

### 1. **Never Commit Secrets to Git**

**❌ DO NOT commit:**

- API keys (Stripe, AWS, etc.)
- Database passwords
- JWT secrets
- Private keys or certificates
- OAuth tokens
- Any production credentials

**✅ DO use:**

- Environment variables (`.env` files)
- Secret management services (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
- Encrypted configuration files
- CI/CD secret stores (GitHub Secrets, GitLab CI/CD Variables)

### 2. **Environment Variables**

All sensitive configuration should be stored in `.env` files:

```bash
# .env (NEVER commit this file!)
POSTGRES_USER=your_username
POSTGRES_PASSWORD=your_secure_password
JWT_SECRET=your_generated_secret_key
STRIPE_SECRET_KEY=sk_test_your_stripe_key
```

**Important:**

- The `.env` file is already in `.gitignore`
- Use `.env.example` to document required variables (without actual values)
- Generate strong secrets: `openssl rand -base64 32`

### 3. **Postman Collections**

When creating Postman collections:

**❌ Avoid:**

```json
{
  "body": {
    "raw": "{\"email\": \"user@example.com\", \"password\": \"MyPassword123!\"}"
  }
}
```

**✅ Use variables instead:**

```json
{
  "body": {
    "raw": "{\"email\": \"{{test_email}}\", \"password\": \"{{test_password}}\"}"
  }
}
```

Define variables in:

- Collection variables (for test data)
- Environment variables (for environment-specific values)

### 4. **Git History Cleanup**

If you accidentally commit secrets:

1. **Immediately revoke/rotate the exposed secret**
2. **Remove from Git history:**

   ```bash
   # Using BFG Repo-Cleaner (recommended)
   bfg --replace-text passwords.txt
   git reflog expire --expire=now --all
   git gc --prune=now --aggressive

   # Or using git filter-branch
   git filter-branch --force --index-filter \
     "git rm --cached --ignore-unmatch path/to/file" \
     --prune-empty --tag-name-filter cat -- --all
   ```

3. **Force push to remote:**
   ```bash
   git push origin --force --all
   git push origin --force --tags
   ```

### 5. **Secret Scanning**

This repository uses **GitGuardian** for automatic secret detection.

**Common patterns detected:**

- Generic passwords in code
- API keys (AWS, Stripe, etc.)
- Database connection strings with credentials
- Private keys (RSA, SSH, etc.)
- OAuth tokens

**To prevent alerts:**

- Use environment variables
- Use Postman variables for test data
- Never hardcode credentials in code or configuration files

---

## 🔐 Secrets Management

### Development Environment

1. **Create `.env` file:**

   ```bash
   cd src
   cp .env.example .env
   # Edit .env with your local credentials
   ```

2. **Generate secure secrets:**

   ```bash
   # JWT Secret (minimum 32 characters)
   openssl rand -base64 32

   # Or use a password generator
   pwgen -s 64 1
   ```

3. **Never share your `.env` file**

### Production Environment

**Use a proper secret management solution:**

1. **Kubernetes Secrets:**

   ```yaml
   apiVersion: v1
   kind: Secret
   metadata:
     name: app-secrets
   type: Opaque
   data:
     jwt-secret: <base64-encoded-value>
     db-password: <base64-encoded-value>
   ```

2. **Cloud Provider Solutions:**

   - **AWS:** AWS Secrets Manager, AWS Systems Manager Parameter Store
   - **Azure:** Azure Key Vault
   - **GCP:** Google Secret Manager

3. **HashiCorp Vault** for multi-cloud environments

---

## 📋 Security Checklist

Before committing code, ensure:

- [ ] No hardcoded passwords, API keys, or secrets
- [ ] `.env` file is in `.gitignore`
- [ ] Postman collections use variables for credentials
- [ ] Database connection strings use environment variables
- [ ] JWT secrets are generated and stored securely
- [ ] All production secrets are managed by a secret management service
- [ ] No sensitive data in commit messages
- [ ] No sensitive data in code comments

---

## 🚨 Incident Response

If a secret is exposed:

1. **Immediate Actions:**

   - Revoke/rotate the exposed secret immediately
   - Assess the impact (what systems/data could be accessed)
   - Check access logs for unauthorized usage

2. **Remediation:**

   - Remove the secret from Git history
   - Update all systems using the old secret
   - Force push cleaned history to remote

3. **Prevention:**
   - Review and update security practices
   - Add pre-commit hooks for secret detection
   - Educate team members

---

## 🔧 Tools & Resources

### Secret Scanning Tools

- **GitGuardian** - Automated secret detection (already integrated)
- **git-secrets** - Prevents committing secrets
  ```bash
  brew install git-secrets
  git secrets --install
  git secrets --register-aws
  ```
- **TruffleHog** - Find secrets in Git history
  ```bash
  docker run --rm -it -v "$PWD:/pwd" trufflesecurity/trufflehog:latest github --repo https://github.com/your/repo
  ```

### Pre-commit Hooks

Install pre-commit hooks to prevent secret commits:

```bash
# Install pre-commit
pip install pre-commit

# Create .pre-commit-config.yaml
cat > .pre-commit-config.yaml << EOF
repos:
  - repo: https://github.com/Yelp/detect-secrets
    rev: v1.4.0
    hooks:
      - id: detect-secrets
        args: ['--baseline', '.secrets.baseline']
EOF

# Install hooks
pre-commit install
```

---

## 📚 Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security/getting-started/securing-your-repository)
- [GitGuardian Documentation](https://docs.gitguardian.com/)
- [12-Factor App - Config](https://12factor.net/config)

---

## 📞 Contact

For security concerns, contact:

- **Email:** [your-email@example.com]
- **Security Team:** [security@example.com]

---

**Last Updated:** January 1, 2026
