# Contributing to AI-LMS

Thank you for contributing. Please read these guidelines before opening a PR.

---

## Branch Naming

| Type        | Pattern                       | Example                          |
|-------------|-------------------------------|----------------------------------|
| Feature     | `feature/<short-description>` | `feature/auth-login`             |
| Bug fix     | `fix/<short-description>`     | `fix/lesson-load-error`          |
| Refactor    | `refactor/<description>`      | `refactor/tenant-middleware`      |
| Docs        | `docs/<description>`          | `docs/local-setup`               |
| Infra/DevOps| `infra/<description>`         | `infra/docker-compose-update`    |

---

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short summary>

Examples:
feat(auth): add JWT refresh token endpoint
fix(lesson): correct chapter ordering query
docs(readme): update local setup instructions
refactor(tenant): extract tenant resolver middleware
chore(deps): update frontend dependencies
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `infra`

---

## Pull Request Rules

- PRs must target `main` (or `develop` if a dev branch exists)
- All PRs require a description following the PR template
- Self-review your code before requesting review
- Never commit secrets, credentials, or `.env` files
- Tenant isolation must not be broken in any PR

---

## Code Standards

- Frontend: Follow ESLint + Prettier config in `frontend/`
- Backend: Follow C# conventions, EditorConfig, and project layering rules
- AI service: Follow PEP8 + Ruff config in `ai-service/`
- All new features should include at minimum a test placeholder

---

## Environment Variables

- Only use `.env.example` as a template
- Never commit `.env` or any real secret
- Add new variables to `.env.example` with placeholder values only

---

## Questions

Open a discussion or issue before making large architectural changes.
