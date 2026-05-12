# Phase 1 — Section 3 Readiness Checklist

Use this checklist before starting **Phase 1, Section 4**.
Every item should be verified in a current checkout of the repo.

---

## Automated checks (run these first)

```bash
cd frontend
npm run type-check      # must exit 0, 0 TS errors
npm run lint            # must exit 0
npm run test -- --run   # must pass: 41 tests
npm run build           # must complete with no errors

cd ../backend
dotnet test             # must pass: 31 xUnit tests

cd ../ai-service
pytest                  # must pass: 4 tests
```

**Verified ✅ — Phase 1 Section 3 Parts 1–5 complete (2026-05-11)**

---

## Backend auth endpoints

- [ ] `POST /api/auth/register` returns 201 + `AuthResponse` (accessToken, tokenType, expiresIn, userId, email, role, tenantId)
- [ ] `POST /api/auth/login` returns 200 + `AuthResponse`
- [ ] Invalid credentials return 401 with `{ title: "Authentication failed." }`
- [ ] Duplicate email returns 409 with `{ title: "The email address is already registered." }`
- [ ] Backend `dotnet run` starts without error at `http://localhost:5000`

## Frontend auth flow

- [ ] `npm run dev` starts at `http://localhost:5173`
- [ ] Navigating to `/` or `/admin` while unauthenticated redirects to `/auth/login`
- [ ] Login with invalid credentials shows "Invalid email or password." inline
- [ ] Login with valid credentials redirects to the originally requested page (or `/`)
- [ ] After login, the Topbar shows the user's email initial and a Logout button
- [ ] Logout button clears the session and redirects to `/auth/login`
- [ ] Navigating to `/auth/login` while already logged in redirects to `/`
- [ ] Register with an existing email shows "An account with this email address already exists."
- [ ] Register with a new account creates a session and redirects to `/`
- [ ] Hard-refreshing the browser while logged in restores the session without flashing the login page
- [ ] An expired/revoked token (simulated by manually clearing `auth_token` from localStorage) causes the next protected page load to redirect to `/auth/login`

## Token persistence and state

- [ ] `localStorage.auth_token` is set after a successful login/register
- [ ] `localStorage.auth_user` is set with `{ id, email, role, tenantId }` JSON
- [ ] Both keys are cleared after logout
- [ ] Both keys are cleared when a 401 response is received from the API

## Error handling

- [ ] Network error (backend offline) shows "Unable to reach the server. Please check your connection."
- [ ] Server error (500) shows the generic error message, not a raw stack trace
- [ ] Form field errors are shown inline, per-field
- [ ] Submit button is disabled while a request is in-flight

---

## Section 3 completion summary

### What was built

| Part | Description |
|------|-------------|
| Part 1 | Backend domain, application, infrastructure: User aggregate, PBKDF2 password hasher, JWT token service |
| Part 2 | AuthService (register + login), InMemoryUserRepository, exception middleware, 23 backend tests |
| Part 3 | Frontend: LoginForm, RegisterForm, LoginPage, RegisterPage, auth.service.ts, auth types, routes |
| Part 4 | Frontend: AuthContext (signIn/signOut/isRestoring), tokenStorage, ProtectedRoute, AuthProvider, Topbar logout |
| Part 5 | mapAuthError utility, session-expired CustomEvent, AuthLayout redirect, i18n error keys, readiness doc |

### Key technical decisions

- JWT stored in `localStorage` (XSS-vulnerable — acceptable for MVP; migrate to httpOnly cookie in Phase 3)
- Session restore: synchronous localStorage read on mount, guarded by `isRestoring` flag
- Session expiry: `auth:session-expired` CustomEvent bridges api-client ↔ AuthContext without a circular import
- Error mapping: centralized in `utils/mapAuthError.ts`; server `title` field used (not `detail`)
- Role sent as integer (BackendUserRole enum index) to backend

---

## Intentionally deferred to later phases

| Concern | Target phase |
|---------|-------------|
| JWT refresh token | Phase 3 |
| Token expiry check on restore (decode `exp` claim) | Phase 2 |
| `/me` profile hydration on restore | Phase 2 |
| Multi-tab session sync (`storage` event) | Phase 3 |
| httpOnly cookie storage strategy | Phase 3 |
| MFA / two-factor authentication | Phase 4+ |
| Password reset flow | Phase 2 |
| Tenant-aware auth (TenantProvider) | Phase 3 |
| Per-route role guards (`allowedRoles` in route handle) | Phase 2 |
| Advanced permission matrix | Phase 3 |
| Device management / session listing | Phase 4+ |
| Social login (OAuth providers) | Phase 4+ |

---

## Known risks and tradeoffs

| Risk | Severity | Mitigation |
|------|----------|-----------|
| `localStorage` JWT vulnerable to XSS | Medium | Phase 3: migrate to httpOnly cookie; no sensitive data in token claims |
| No token expiry check on restore | Low | Expired token → first API call returns 401 → `auth:session-expired` event → redirect |
| No refresh token → users must re-login when token expires | Low | Acceptable for Phase 1 dev build; add refresh in Phase 3 |
| `InMemoryUserRepository` → data lost on restart | Expected | Replace with EF Core in Phase 2 |
