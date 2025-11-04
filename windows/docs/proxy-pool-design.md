# Proxy Pool: Design, Current State, Next Steps

## Summary
- A Proxy Pool is a collection of proxies with a selection strategy used at runtime.
- Current app supports:
  - Creating proxies and pools in the catalog
  - Selecting either a single proxy or a pool in NewBrowserEnvironmentWindow
  - Quick connectivity test for individual proxies
- Missing pieces (to complete commercial-grade behavior):
  - Manage pool members (assign/remove proxies to pools)
  - Runtime resolver: when ProxyType=pool, pick a proxy based on strategy
  - Background health checks and scoring per pool member

## Current Behavior (as of this change)
- NewBrowserEnvironmentWindow:
  - ProxyMode: none/reference/api
  - ProxyType: server/pool (new)
  - ProxyRefId: Id of selected proxy or pool when ProxyMode=reference
  - UI refresh on dropdown open to ensure newly created items are visible
- AddProxyDialog:
  - Now supports selecting an optional owning pool on save
  - When a pool is chosen, the newly created proxy is assigned to that pool immediately

## Data Model
- ProxyServer
  - PoolId: int? — owning pool (null if ungrouped)
- ProxyPool
  - Name, Strategy (random/health/least-used etc.), Enabled
- BrowserEnvironment
  - ProxyMode: none/reference/api
  - ProxyType: server/pool
  - ProxyRefId: references ProxyServer.Id or ProxyPool.Id depending on ProxyType

## API/Services
- ProxyCatalogService
  - Create(label, protocol, address, port, username, password)
  - GetAll(), GetPools(), Update(ProxyServer)
  - Note: Create returns a tracked entity; assigning PoolId followed by Update() persists pool membership
- ProxyHealthService
  - QuickProbeAsync(ProxyServer) — 3s timeout HTTP(S) quick probe

## UI/UX Changes
- NewBrowserEnvironmentWindow
  - Added Single/Pool toggle with two ComboBoxes for proxy or pool selection
  - Persisted ProxyType and ProxyRefId accordingly
- AddProxyDialog
  - Added optional pool selector to assign the new proxy to a pool on save
  - Save/Cancel buttons always visible; dialog is resizable with minimum size; Enter/Esc shortcuts

## Roadmap
1. Pool Management UI (high)
   - View pools and their members
   - Add/remove proxies to pools
   - Create/delete pools
2. Runtime Resolver (high)
   - For ProxyType=pool, pick a member by strategy
   - Update usage stats; fallback if proxy offline
3. Background Health Checks (medium)
   - Periodic QuickProbe
   - Score computation, disable offline, recover online
4. SOCKS5 Testing and Auth (medium)
   - Implement proper SOCKS5 probe
   - Inject NetworkCredential for auth proxies

## Verification
- Create a pool (Proxy Pools page)
- Use AddProxyDialog to create a proxy and assign the pool
- In NewBrowserEnvironmentWindow, pick ProxyMode=reference, target=pool, choose the pool
- Save environment; verify ProxyType="pool" and ProxyRefId points to pool Id in DB

## Notes
- DisplayMemberPath for proxies is Label; when not provided, UI uses address:port as default label
- Dropdowns auto-refresh to show newly created items immediately
