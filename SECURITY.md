# Security Policy

## 🔒 Security Best Practices

This document outlines security best practices for the E-commerce Microservices Platform.

## 🔑 Environment Variables & Secrets Management

### Never Commit Secrets

**NEVER** commit the following to version control:

- `.env` files containing actual credentials
- API keys (Stripe, third-party services)
- JWT secrets
- Database passwords
- Any production credentials

### Using Environment Variables

1. **Development**: Use `.env.example` as a template

   ```bash
   cp src/.env.example src/.env
   # Edit .env with your local credentials
   ```

2. **Production**: Use secure secret management
   - **Kubernetes**: Use Kubernetes Secrets
   - **Docker Swarm**: Use Docker Secrets
   - **Cloud Providers**: Use AWS Secrets Manager, Azure Key Vault, or GCP Secret Manager

### Generate Secure Secrets

#### JWT Secret

Generate a cryptographically secure random string:

```bash
# Option 1: Using OpenSSL (recommended)
openssl rand -base64 32

# Option 2: Using .NET
dotnet user-secrets set "JwtSettings:SecretKey" "$(openssl rand -base64 32)"

# Option 3: Using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

#### Database Passwords

Use strong passwords with:

- Minimum 16 characters
- Mix of uppercase, lowercase, numbers, and special characters
- Avoid common words or patterns

```bash
# Generate a secure password
openssl rand -base64 24
```

## 🛡️ JWT Configuration

### Development Settings

```bash
JWT_SECRET=CHANGE_ME_TO_A_SECURE_RANDOM_STRING_MIN_32_CHARACTERS
JWT_ISSUER=EcommerceAPI
JWT_AUDIENCE=EcommerceClient
JWT_TOKEN_LIFETIME=15  # 15 minutes for access tokens
```

### Production Recommendations

- **Token Lifetime**: Keep access tokens short-lived (5-15 minutes)
- **Refresh Tokens**: Use refresh tokens with longer expiry (7-30 days)
- **Secret Rotation**: Rotate JWT secrets periodically
- **Algorithm**: Use RS256 (asymmetric) instead of HS256 for production

## 💳 Stripe API Keys

### Test vs Production Keys

**Development/Testing**:

```bash
STRIPE_SECRET_KEY=sk_test_YOUR_STRIPE_TEST_KEY_HERE
```

**Production**:

```bash
STRIPE_SECRET_KEY=sk_live_YOUR_STRIPE_LIVE_KEY_HERE
```

### Best Practices

- ✅ Use **test keys** (`sk_test_`) in development
- ✅ Use **production keys** (`sk_live_`) only in production
- ✅ Store keys in environment variables, never in code
- ✅ Restrict API key permissions in Stripe Dashboard
- ✅ Monitor API key usage for suspicious activity
- ✅ Rotate keys if compromised

Get your keys from:

- Test: https://dashboard.stripe.com/test/apikeys
- Production: https://dashboard.stripe.com/apikeys

## 🗄️ Database Security

### Connection Strings

**Development**:

```bash
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_dev_password
```

**Production**:

- Use strong, unique passwords
- Enable SSL/TLS connections
- Restrict network access (firewall rules)
- Use connection pooling
- Enable audit logging

### PostgreSQL Security Checklist

- [ ] Change default `postgres` user password
- [ ] Create application-specific database users
- [ ] Grant minimum required permissions
- [ ] Enable SSL/TLS for connections
- [ ] Configure `pg_hba.conf` for access control
- [ ] Regular security updates
- [ ] Enable audit logging
- [ ] Regular backups with encryption

## 🔐 HTTPS/TLS

### Development

- HTTP is acceptable for local development
- Use `localhost` or `127.0.0.1`

### Production

- **ALWAYS** use HTTPS
- Use valid SSL/TLS certificates (Let's Encrypt, commercial CA)
- Enable HSTS (HTTP Strict Transport Security)
- Use TLS 1.2 or higher
- Disable weak cipher suites

## 🚨 Security Headers

Ensure these headers are set in production:

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
```

## 🔍 Monitoring & Logging

### What to Log

- ✅ Authentication attempts (success/failure)
- ✅ Authorization failures
- ✅ API errors and exceptions
- ✅ Suspicious activity patterns
- ✅ Configuration changes

### What NOT to Log

- ❌ Passwords
- ❌ JWT tokens
- ❌ Credit card numbers
- ❌ API keys
- ❌ Personal Identifiable Information (PII)

### Log Management

- Use structured logging (Seq, ELK Stack)
- Enable log rotation
- Secure log storage
- Regular log review
- Set up alerts for security events

## 🐳 Docker Security

### Image Security

- Use official base images
- Keep images updated
- Scan images for vulnerabilities
- Use multi-stage builds
- Run as non-root user

### Container Security

- Limit container resources (CPU, memory)
- Use read-only file systems where possible
- Drop unnecessary capabilities
- Use Docker secrets for sensitive data
- Regular security scans

## ☸️ Kubernetes Security

### Secrets Management

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: ecommerce-secrets
type: Opaque
data:
  jwt-secret: <base64-encoded-secret>
  db-password: <base64-encoded-password>
  stripe-key: <base64-encoded-key>
```

### Best Practices

- Use RBAC (Role-Based Access Control)
- Enable Pod Security Policies
- Use Network Policies
- Regular security updates
- Enable audit logging
- Use service mesh (Istio, Linkerd) for mTLS

## 🔄 Dependency Security

### Regular Updates

```bash
# Check for .NET vulnerabilities
dotnet list package --vulnerable

# Update packages
dotnet outdated

# Check for npm vulnerabilities
npm audit

# Fix npm vulnerabilities
npm audit fix
```

### Automated Scanning

- Enable GitHub Dependabot
- Use Snyk or similar tools
- Regular dependency updates
- Review security advisories

## 📋 Security Checklist

### Before Going to Production

- [ ] All secrets moved to secure secret management
- [ ] `.env` files not committed to version control
- [ ] Strong JWT secret generated (32+ characters)
- [ ] HTTPS/TLS enabled
- [ ] Security headers configured
- [ ] Database passwords changed from defaults
- [ ] Stripe production keys configured
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] Input validation implemented
- [ ] SQL injection prevention verified
- [ ] XSS protection enabled
- [ ] CSRF protection enabled
- [ ] Authentication/Authorization tested
- [ ] Security logging enabled
- [ ] Dependency vulnerabilities resolved
- [ ] Container images scanned
- [ ] Kubernetes secrets configured
- [ ] Network policies defined
- [ ] Backup and recovery tested

## 🐛 Reporting Security Vulnerabilities

If you discover a security vulnerability, please:

1. **DO NOT** open a public GitHub issue
2. Email security concerns to: [your-security-email@example.com]
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

We will respond within 48 hours and work with you to resolve the issue.

## 📚 Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [Kubernetes Security Best Practices](https://kubernetes.io/docs/concepts/security/)

## 📄 License

This security policy is part of the E-commerce Microservices Platform project.

---

**Last Updated**: 2025-12-20
