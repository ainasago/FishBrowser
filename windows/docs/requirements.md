# Requirements

## Scope
- Browser environment management with fingerprinting.
- Enterprise-grade proxy management and usage in browser sessions.
- Persistence of WebGL/Fonts and related fingerprint modes (already implemented and verified with logs).
- UI priority: user-entered values override profiles/presets.
- Session persistence for browsers (already present).

## Functional Requirements
- Fingerprint persistence
  - Save/Load: WebGLVendor/WebGLRenderer, WebGLImageMode, WebGLInfoMode, WebGpuMode, AudioContextMode, SpeechVoicesEnabled, FontsMode, FontsJson.
  - UI shows the saved values when editing or switching environments.
  - Add comprehensive logs for save/load.
- Proxy Management
  - Manage proxy servers: add/import/edit/delete; show status, latency, score, country/ISP, type.
  - Manage proxy pools: strategies (random/weighted/rr/lowest-latency/geo-match), caps, retries, cooldown.
  - Health checks: HTTP/HTTPS reachability, latency, anonymity, geo, IPv6; scheduled and on-demand.
  - Usage logging: per-session success/failure, latency, target domain, bytes.
  - Security: encrypted credential storage; secrets never logged.
  - Priority: UI manual proxy > selected existing proxy > pool strategy > direct.
- Browser integration
  - Before launch, resolve proxy using priority; test preflight; rotate on failure.
  - Apply proxy to Playwright context/persistent context.
  - Record usage and update health/score.
- UI/UX
  - New "网络/代理" section in NewBrowserEnvironmentWindow: Direct | Manual | Existing | Pool.
  - Inline create/test proxy; status badge and IP/country shown on success.
  - In Existing/Pool modes: searchable dropdowns with labels, geo/type, latency, success rate.
  - Confirmation prompts only on user-driven selection; never during load.

## Non-Functional Requirements
- Reliability: automatic failover/rotation; concurrency caps per proxy.
- Observability: detailed logs; metrics for proxy success rate/latency.
- Security: encrypt proxy passwords; mask secrets in UI/logs; provider API keys in OS secret store.
- Performance: proxy resolution and preflight within < 1s average; scraping single page < 10s.
- Compatibility: .NET 9, WPF, EF Core (SQLite), Playwright.

## Acceptance Criteria
- Editing environment shows persisted fingerprint values consistently.
- Users can use manual proxy, pick existing, or select pool; manual input has highest priority.
- Successful preflight + browsing via Playwright with applied proxy; on failure, rotation occurs (N attempts) and is logged.
- Health check UI works and updates status/latency/score; pools display computed stats.
- Secrets never logged; passwords encrypted at rest.
