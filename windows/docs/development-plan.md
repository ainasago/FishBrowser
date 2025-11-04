# Development Plan

## Milestones
- M1: Fingerprint persistence and UI synchronization (done)
- M2: Proxy foundations (models, DB, services)
- M3: Proxy UI/UX in environment editor; preflight + browser integration
- M4: Health checks, scoring, pool strategies, rotation & usage logging
- M5: Hardening: security, metrics, docs, tests

## Iteration Breakdown

### M2 – Models and Services
- Add entities: ProxyServer++, ProxyPool, ProxyHealthLog, ProxyUsageLog.
- EF migrations; seed minimal sample.
- Services:
  - ProxyCatalogService (CRUD)
  - ProxyHealthService (HTTP/HTTPS/Geo/IPv6 checks)
  - ProxyResolverService (priority rules, strategy, constraints)
  - SecretService (encrypt/decrypt credentials)

Deliverables: compiling app + basic CRUD for proxies/pools via existing ProxyPoolView (minimal enhancements).

### M3 – UI & Engine Integration
- NewBrowserEnvironmentWindow: Proxy section
  - Modes: Direct | Manual | Existing | Pool
  - Manual: inputs + Test button
  - Existing: dropdown with label/geo/type/latency; inline "新建代理"
  - Pool: dropdown with strategy summary
- PlaywrightController: use ProxyResolverService.Resolve(env, target)
- Preflight test + fallback rotation

Deliverables: Launch with selected/entered proxy; preflight logs visible; no profile overwrite prompts during load.

### M4 – Health/Pool/Rotation
- Scheduled health checks + dashboard stats
- Pool strategies: random/rr/weighted/lowest-latency/geo-match
- Rotation policies: retries/backoff/cooldown; sticky sessions support
- Usage logging and scoring (EWMA)

Deliverables: Stable pool rotation with statistics and UI indicators.

### M5 – Hardening
- Encrypt credentials at rest; mask in logs/UI
- Add metrics and export endpoints (future)
- Unit/integration tests for resolver/rotation/health
- Documentation finalized

## Risks & Mitigations
- Risk: poor proxy quality → add fast preflight + rotation
- Risk: DB growth of logs → retention policy
- Risk: secrets leakage → SecretService + masking

## Timeline (indicative)
- M2: 2-3 days
- M3: 2-3 days
- M4: 3-5 days
- M5: 2 days
