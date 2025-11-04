# Technical Design

## Data Model
- ProxyServer (extended)
  - Id, Label, Protocol, Address, Port, Username, PasswordEncrypted, Provider, PoolId, TagsJson
  - Type, Country, Region, City, ISP, SupportsHttps, SupportsIPv6, Anonymity
  - StickySupported, StickyKey, RotationSeconds, MaxConcurrency, CurrentConcurrency, Enabled
  - Score, SuccessCount, FailCount, LastUsedAt, LastFailureAt, LastCheckedAt, ResponseTimeMs, Notes
- ProxyPool
  - Id, Name, Strategy, GeoPref, MaxRetries, CooldownSeconds, ConcurrencyCap, BypassDomainsJson, RequireHttps
- ProxyHealthLog
  - Id, ProxyId, Timestamp, Result, LatencyMs, ErrorCode, TestType, CountryDetected, IPDetected
- ProxyUsageLog
  - Id, ProxyId, StartedAt, EndedAt, TargetDomain, Success, StatusCode, BytesSent, BytesRecv

## Services
- SecretService
  - Encrypt/Decrypt proxy.Password; DPAPI ProtectedData on Windows
- ProxyCatalogService
  - CRUD for ProxyServer/ProxyPool; queries with filters and paging
- ProxyHealthService
  - Health probes (HTTP/HTTPS GET, geo lookup via ip-api.com or provider endpoint), IPv6/DNS leak tests (phase 2)
  - Update Score via EWMA: Score_t = alpha*success - beta*latencyNorm + (1-alpha-beta)*Score_{t-1}
- ProxyResolverService
  - Resolve(env, targetDomain, override?) → ProxyServer?
  - Priority: manual inline > existing proxy id > pool(strategy)
  - Constraints: Enabled, Score >= threshold, Concurrency < Max, Geo preference, RequireHttps, BypassDomains
  - Rotation: on failure retry up to pool.MaxRetries, respect CooldownSeconds and backoff
- ProxyRotationService
  - Sticky session keys for providers that support it; rotate after RotationSeconds or failure

## UI Integration
- NewBrowserEnvironmentWindow
  - New section: Proxy
    - Radio: Direct | Manual | Existing | Pool
    - Manual: Protocol/Host/Port/User/Pass + Test; Save-as-Proxy checkbox
    - Existing: Combo with columns (Label, Geo, Type, Latency, SuccessRate); New Proxy...
    - Pool: Combo; View Pool...
  - Priority: UI overrides; prompts only on user action
- ProxyPoolView
  - Extend with latency/success/score columns, enable/disable toggle, search/filter
  - Add "New Pool" dialog with strategy fields

## Engine Integration (PlaywrightController)
- Before context creation:
  - var proxy = ProxyResolverService.Resolve(env, targetDomain)
  - Optional quick preflight (HEAD https, 3s timeout)
  - On failure rotate within pool
- Apply to BrowserNewContextOptions/LaunchPersistentOptions.Proxy
- Log to ProxyUsageLog after session

## Logging
- Log categories: ProxyResolve, ProxyHealth, ProxyUsage, ProxyUI
- Correlate with EnvironmentId/ProfileId

## Security
- Do not log secrets; mask in UI
- Store provider API keys in OS secret store / user secrets

## Migration
- EF Core migration to add new tables/columns and indexes
- Data retention policy for *_Log tables (e.g., keep last 30 days)

## Testing
- Unit tests for resolver strategies and constraints
- Integration tests: preflight + fallback
- UI tests for manual/existing/pool flows
