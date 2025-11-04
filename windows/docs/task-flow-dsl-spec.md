# Task Flow DSL Spec (AI-Authorable)

Version: 1.0
Status: Draft
Owner: WebScraperApp

## 1. Purpose
Provide a deterministic, machine-writable and human-readable DSL for defining web automation tasks (open page, click, input, extract, control flow like if/for/parallel). The spec is optimized so AI models can reliably generate valid scripts that pass strict validation.

## 2. Design Principles (AI-Friendly)
- Canonical structure. Fixed field names, predictable ordering, minimal ambiguity.
- Validation-first. Each step type has a tight schema; unknown fields are rejected.
- Deterministic defaults. All timeouts/awaits/visibility rules have explicit defaults.
- Minimal surface. V1 focuses on a core action set needed for 80% workflows.
- Side-effect aware. Steps declare retries, timeouts, idempotency hints.
- Versioned. `dslVersion` required; additive evolution favored; breaking changes gated by migration.

## 3. File Formats
- Primary: YAML (for humans). Secondary: JSON (for machines). Structures are identical.
- One flow per file. Reusable subflows can be imported.

## 4. Top-Level Structure
- dslVersion: string (e.g., "1.0")
- id: string (ULID/UUID/slug)
- name: string
- description?: string
- settings?: Settings
- fingerprint?: FingerprintRef
- proxy?: ProxyRef
- vars?: object (string|number|bool|array|object)
- steps: Step[]
- subflows?: Record<string, FlowLike>
- imports?: string[] (relative paths)

## 5. Settings
- selectorTimeoutMs: int (default 6000)
- navTimeoutMs: int (default 15000)
- stepTimeoutMs: int (default 20000)
- flowTimeoutMs: int (default 600000)
- retries: { step?: RetryPolicy, flow?: RetryPolicy }
- concurrency?: { maxRunners?: int, rateLimitPerHostPerMin?: int }

RetryPolicy:
- max: int (default 0)
- backoff: "none"|"fixed"|"expo" (default "expo")
- baseMs: int (default 500)
- maxMs: int (default 10000)
- jitter: bool (default true)

## 6. References
FingerprintRef:
- profileId?: int
- mode?: "existing|custom"
- compiledProfileId?: int (if precompiled)

ProxyRef:
- mode: "none|manual|existing|pool"
- proxyId?: int
- poolId?: int
- manual?: { protocol: "http|https|socks5", host: string, port: int, username?: string, password?: string }

## 7. Expressions
- Interpolation: `{{ ... }}` (must evaluate to scalar/array/object).
- Operators: + - * / %; == != > >= < <=; && || !; ternary `cond ? a : b`.
- Access: dot and bracket (e.g., data.items[0].title).
- Built-ins: len(), toInt(), toFloat(), toBool(), lower(), upper(), trim(), now(), isoNow(), guid(), sha256(str), match(text, regex), contains(hay, needle).
- Disallowed: network/io/crypto/unsafe eval.
- Variables: args.*, vars.*, env.*, runtime.*, page.*, data.*, secrets.*

## 8. Selectors
Selector:
- type: "css|xpath|text|role"
- value: string
- index?: int (0-based)
- within?: { frameSelector?: Selector, shadowHostSelector?: Selector }
- waitFor?: "attached|visible|hidden|stable" (default "visible")

## 9. Action Catalog (V1)
Each step is a single-key object where the key is the action name and the value is the parameters object.

- open: { url: string }
- goto: { url: string, waitUntil?: "load|domcontentloaded|networkidle" }
- reload: { }
- back: { }
- forward: { }
- click: { selector: Selector, button?: "left|middle|right", clickCount?: 1|2, delayMs?: int }
- dblclick: { selector: Selector }
- type: { selector: Selector, text: string, delayMs?: int }
- fill: { selector: Selector, value: string }
- press: { selector: Selector, key: string }
- hover: { selector: Selector }
- scrollTo: { selector?: Selector, x?: int, y?: int }
- waitFor: { selector?: Selector, timeoutMs?: int, state?: "attached|visible|hidden|stable" }
- waitNetworkIdle: { timeoutMs?: int }
- switchPage: { index?: int, urlContains?: string, titleContains?: string }
- switchFrame: { selector: Selector }
- screenshot: { file?: string, selector?: Selector, fullPage?: bool }
- download: { selector?: Selector, url?: string, saveAs?: string }
- cookies.set: { items: { name: string, value: string, domain?: string, path?: string, httpOnly?: bool, secure?: bool, sameSite?: "Lax|Strict|None", expires?: string }[] }
- cookies.clear: { names?: string[] }
- localStorage.set: { items: Record<string,string> }
- eval: { code: string, expose?: Record<string, any> }  # ran in page context, must be pure
- extract: { fields: ExtractSpec }
- emit: { key: string, value: any }
- log: { level: "info|warn|error|debug", message: string, data?: any }
- sleep: { ms: int }

ExtractSpec example:
- Simple: `{ title: { sel: { type: "css", value: "h1" }, attr: "text" } }`
- List: `{ items[]: { sel: { type: "css", value: ".card" }, fields: { title: { sel: {type:"css", value:"h3"}, attr:"text" }, link: { sel: {type:"css", value:"a"}, attr:"href" } } } }`
- attr: "text|html|href|src|value|attr:<name>"

## 10. Control Flow Steps
- if: { cond: string, then: Step[], else?: Step[] }
- for: { item: string, list: string|any[], do: Step[], maxIter?: int }
- while: { cond: string, do: Step[], maxIter?: int }
- try: {
    steps: Step[],
    catch?: { on?: string[] /* Timeout|SelectorNotFound|NavigationError|NetError|Any */, steps?: Step[], retry?: RetryPolicy },
    finally?: Step[]
  }
- parallel: { tasks: Step[][], maxConcurrency?: int }
- call: { name: string, args?: object, onError?: "propagate|suppress" }

## 11. Per-Step Common Options
Each action/control step may accept under the same object level:
- timeoutMs?: int (overrides defaults)
- retry?: RetryPolicy
- assert?: { expr: string, level?: "error|warn|soft", message?: string }
- idempotent?: bool (hint)

Example:
```yaml
- click:
    selector: { type: css, value: "#submit" }
  timeoutMs: 8000
  retry: { max: 2, backoff: expo, baseMs: 300 }
```

## 12. Errors & Codes
Canonical error types for catch/on and logs:
- Timeout, SelectorNotFound, NavigationError, NetworkError, ScriptError, AssertionFailed, DownloadError, Unknown

## 13. Logging & Artifacts
- Structured logs per step: { runId, stepId, action, start, end, durationMs, status, error?, attrs }
- Artifacts: screenshots, HTML dumps, HAR (optional), downloads. Each linked to stepId.
- Categories: TaskFlow, TaskStep, TaskEngine, ProxyResolve, PlaywrightController

## 14. Integration Mapping (Playwright)
- open/goto/reload/back/forward → Page navigation and context methods
- selector.waitFor → default to visible unless overridden
- addInitScript for future Canvas/WebGL/WebRTC/Navigator patches (from FingerprintProfile)
- Proxy/fingerprint applied at context creation (before first navigation)

## 15. Security & Secrets
- `secrets.*` read-only in expressions; never logged; never emitted; masked in UI.
- `eval.code` executed in page context; may only access exposed values; sandboxed.

## 16. Versioning & Compatibility
- `dslVersion` must be "1.x" for this spec.
- Minor additions must not change existing semantics.
- Migrations provide field rename/aliasing when needed.

## 17. Canonical Examples
A) Minimal search flow:
```yaml
dslVersion: "1.0"
id: flow_search_min
name: Search Minimal
settings:
  selectorTimeoutMs: 6000
vars:
  baseUrl: "https://example.com"
  q: "手机"
steps:
  - open: { url: "{{ vars.baseUrl }}" }
  - waitFor: { selector: { type: css, value: "input[name=q]" } }
  - type: { selector: { type: css, value: "input[name=q]" }, text: "{{ vars.q }}", delayMs: 40 }
  - click: { selector: { type: css, value: "button[type=submit]" } }
  - waitNetworkIdle: {}
  - extract:
      fields:
        results[]:
          sel: { type: css, value: ".result" }
          fields:
            title: { sel: { type: css, value: "h3" }, attr: text }
            link:  { sel: { type: css, value: "a" },  attr: href }
  - emit: { key: "results", value: "{{ data.results }}" }
```

B) If/For/Parallel with retry:
```yaml
dslVersion: "1.0"
id: flow_adv
name: Advanced Control Flow
steps:
  - if:
      cond: "{{ page.url contains 'login' }}"
      then:
        - fill: { selector: {type: css, value: "#user"}, value: "{{ secrets.user }}" }
        - fill: { selector: {type: css, value: "#pass"}, value: "{{ secrets.pass }}" }
        - click: { selector: {type: css, value: "button[type=submit]"} }
        - waitNetworkIdle: {}
      else:
        - log: { level: info, message: "Already logged in" }
  - for:
      item: k
      list: "{{ vars.keywords }}"
      do:
        - type: { selector: {type: css, value: "input[name=q]"}, text: "{{ k }}" }
        - click: { selector: {type: css, value: "button[type=submit]"} }
        - waitNetworkIdle: {}
  - parallel:
      maxConcurrency: 3
      tasks:
        - [ { goto: { url: "https://httpbin.org/headers" } }, { waitNetworkIdle: {} } ]
        - [ { goto: { url: "https://example.com" } }, { screenshot: { file: "ex.png" } } ]
```

C) Try/Catch with backoff:
```yaml
- try:
    steps:
      - click: { selector: { type: css, value: "#buy" } }
    catch:
      on: [ Timeout, SelectorNotFound ]
      retry: { max: 2, backoff: expo, baseMs: 400, maxMs: 4000 }
      steps:
        - reload: {}
        - waitNetworkIdle: {}
```

## 18. JSON Schema (Draft 2020-12; abbreviated)
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://webscraperapp.local/schemas/task-flow-dsl-1.0.json",
  "type": "object",
  "required": ["dslVersion", "id", "name", "steps"],
  "properties": {
    "dslVersion": { "const": "1.0" },
    "id": { "type": "string" },
    "name": { "type": "string" },
    "description": { "type": "string" },
    "settings": { "$ref": "#/defs/Settings" },
    "fingerprint": { "$ref": "#/defs/FingerprintRef" },
    "proxy": { "$ref": "#/defs/ProxyRef" },
    "vars": { "type": "object", "additionalProperties": true },
    "steps": { "type": "array", "items": { "$ref": "#/defs/Step" } },
    "subflows": { "type": "object", "additionalProperties": { "$ref": "#/defs/FlowLike" } },
    "imports": { "type": "array", "items": { "type": "string" } }
  },
  "defs": {
    "Settings": {
      "type": "object",
      "properties": {
        "selectorTimeoutMs": { "type": "integer", "minimum": 0 },
        "navTimeoutMs": { "type": "integer", "minimum": 0 },
        "stepTimeoutMs": { "type": "integer", "minimum": 0 },
        "flowTimeoutMs": { "type": "integer", "minimum": 0 },
        "retries": { "$ref": "#/defs/Retries" },
        "concurrency": { "$ref": "#/defs/Concurrency" }
      },
      "additionalProperties": false
    },
    "Retries": {
      "type": "object",
      "properties": { "step": { "$ref": "#/defs/RetryPolicy" }, "flow": { "$ref": "#/defs/RetryPolicy" } },
      "additionalProperties": false
    },
    "RetryPolicy": {
      "type": "object",
      "properties": {
        "max": { "type": "integer", "minimum": 0 },
        "backoff": { "enum": ["none", "fixed", "expo"] },
        "baseMs": { "type": "integer", "minimum": 0 },
        "maxMs": { "type": "integer", "minimum": 0 },
        "jitter": { "type": "boolean" }
      },
      "additionalProperties": false
    },
    "Concurrency": {
      "type": "object",
      "properties": {
        "maxRunners": { "type": "integer", "minimum": 1 },
        "rateLimitPerHostPerMin": { "type": "integer", "minimum": 0 }
      },
      "additionalProperties": false
    },
    "ProxyRef": {
      "type": "object",
      "properties": {
        "mode": { "enum": ["none", "manual", "existing", "pool"] },
        "proxyId": { "type": "integer" },
        "poolId": { "type": "integer" },
        "manual": {
          "type": "object",
          "properties": {
            "protocol": { "enum": ["http", "https", "socks5"] },
            "host": { "type": "string" },
            "port": { "type": "integer" },
            "username": { "type": "string" },
            "password": { "type": "string" }
          },
          "required": ["protocol", "host", "port"],
          "additionalProperties": false
        }
      },
      "required": ["mode"],
      "additionalProperties": false
    },
    "FingerprintRef": {
      "type": "object",
      "properties": {
        "profileId": { "type": "integer" },
        "mode": { "enum": ["existing", "custom"] },
        "compiledProfileId": { "type": "integer" }
      },
      "additionalProperties": false
    },
    "Selector": {
      "type": "object",
      "properties": {
        "type": { "enum": ["css", "xpath", "text", "role"] },
        "value": { "type": "string" },
        "index": { "type": "integer", "minimum": 0 },
        "waitFor": { "enum": ["attached", "visible", "hidden", "stable"] }
      },
      "required": ["type", "value"],
      "additionalProperties": false
    },
    "Step": {
      "type": "object",
      "minProperties": 1,
      "maxProperties": 5,
      "properties": {
        "open": { "$ref": "#/defs/Open" },
        "goto": { "$ref": "#/defs/Goto" },
        "reload": { "type": "object" },
        "back": { "type": "object" },
        "forward": { "type": "object" },
        "click": { "$ref": "#/defs/Click" },
        "dblclick": { "$ref": "#/defs/Click" },
        "type": { "$ref": "#/defs/Type" },
        "fill": { "$ref": "#/defs/Fill" },
        "press": { "$ref": "#/defs/Press" },
        "hover": { "$ref": "#/defs/Hover" },
        "scrollTo": { "$ref": "#/defs/ScrollTo" },
        "waitFor": { "$ref": "#/defs/WaitFor" },
        "waitNetworkIdle": { "type": "object", "additionalProperties": false },
        "switchPage": { "$ref": "#/defs/SwitchPage" },
        "switchFrame": { "$ref": "#/defs/SwitchFrame" },
        "screenshot": { "$ref": "#/defs/Screenshot" },
        "download": { "$ref": "#/defs/Download" },
        "eval": { "$ref": "#/defs/Eval" },
        "extract": { "$ref": "#/defs/Extract" },
        "emit": { "$ref": "#/defs/Emit" },
        "log": { "$ref": "#/defs/Log" },
        "sleep": { "$ref": "#/defs/Sleep" },
        "if": { "$ref": "#/defs/If" },
        "for": { "$ref": "#/defs/For" },
        "while": { "$ref": "#/defs/While" },
        "try": { "$ref": "#/defs/Try" },
        "parallel": { "$ref": "#/defs/Parallel" },
        "call": { "$ref": "#/defs/Call" },
        "timeoutMs": { "type": "integer", "minimum": 0 },
        "retry": { "$ref": "#/defs/RetryPolicy" },
        "assert": { "$ref": "#/defs/Assert" },
        "idempotent": { "type": "boolean" }
      },
      "additionalProperties": false
    },
    "Open": { "type": "object", "properties": { "url": { "type": "string" } }, "required": ["url"], "additionalProperties": false },
    "Goto": { "type": "object", "properties": { "url": { "type": "string" }, "waitUntil": { "enum": ["load", "domcontentloaded", "networkidle"] } }, "required": ["url"], "additionalProperties": false },
    "Click": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "button": { "enum": ["left","middle","right"] }, "clickCount": { "enum": [1,2] }, "delayMs": { "type": "integer" } }, "required": ["selector"], "additionalProperties": false },
    "Type": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "text": { "type": "string" }, "delayMs": { "type": "integer" } }, "required": ["selector","text"], "additionalProperties": false },
    "Fill": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "value": { "type": "string" } }, "required": ["selector","value"], "additionalProperties": false },
    "Press": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "key": { "type": "string" } }, "required": ["selector","key"], "additionalProperties": false },
    "Hover": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" } }, "required": ["selector"], "additionalProperties": false },
    "ScrollTo": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "x": { "type": "integer" }, "y": { "type": "integer" } }, "additionalProperties": false },
    "WaitFor": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "timeoutMs": { "type": "integer" }, "state": { "enum": ["attached","visible","hidden","stable"] } }, "additionalProperties": false },
    "SwitchPage": { "type": "object", "properties": { "index": { "type": "integer", "minimum": 0 }, "urlContains": { "type": "string" }, "titleContains": { "type": "string" } }, "additionalProperties": false },
    "SwitchFrame": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" } }, "required": ["selector"], "additionalProperties": false },
    "Screenshot": { "type": "object", "properties": { "file": { "type": "string" }, "selector": { "$ref": "#/defs/Selector" }, "fullPage": { "type": "boolean" } }, "additionalProperties": false },
    "Download": { "type": "object", "properties": { "selector": { "$ref": "#/defs/Selector" }, "url": { "type": "string" }, "saveAs": { "type": "string" } }, "additionalProperties": false },
    "Eval": { "type": "object", "properties": { "code": { "type": "string" }, "expose": { "type": "object" } }, "required": ["code"], "additionalProperties": false },
    "Extract": { "type": "object", "properties": { "fields": { "type": "object" } }, "required": ["fields"], "additionalProperties": false },
    "Emit": { "type": "object", "properties": { "key": { "type": "string" }, "value": {} }, "required": ["key","value"], "additionalProperties": false },
    "Log": { "type": "object", "properties": { "level": { "enum": ["info","warn","error","debug"] }, "message": { "type": "string" }, "data": {} }, "required": ["level","message"], "additionalProperties": false },
    "Sleep": { "type": "object", "properties": { "ms": { "type": "integer", "minimum": 0 } }, "required": ["ms"], "additionalProperties": false },
    "Assert": { "type": "object", "properties": { "expr": { "type": "string" }, "level": { "enum": ["error","warn","soft"] }, "message": { "type": "string" } }, "required": ["expr"], "additionalProperties": false },
    "If": { "type": "object", "properties": { "cond": { "type": "string" }, "then": { "type": "array", "items": { "$ref": "#/defs/Step" } }, "else": { "type": "array", "items": { "$ref": "#/defs/Step" } } }, "required": ["cond","then"], "additionalProperties": false },
    "For": { "type": "object", "properties": { "item": { "type": "string" }, "list": {}, "do": { "type": "array", "items": { "$ref": "#/defs/Step" } }, "maxIter": { "type": "integer" } }, "required": ["item","list","do"], "additionalProperties": false },
    "While": { "type": "object", "properties": { "cond": { "type": "string" }, "do": { "type": "array", "items": { "$ref": "#/defs/Step" } }, "maxIter": { "type": "integer" } }, "required": ["cond","do"], "additionalProperties": false },
    "Try": { "type": "object", "properties": { "steps": { "type": "array", "items": { "$ref": "#/defs/Step" } }, "catch": { "type": "object", "properties": { "on": { "type": "array", "items": { "type": "string" } }, "steps": { "type": "array", "items": { "$ref": "#/defs/Step" } }, "retry": { "$ref": "#/defs/RetryPolicy" } }, "additionalProperties": false }, "finally": { "type": "array", "items": { "$ref": "#/defs/Step" } } }, "required": ["steps"], "additionalProperties": false },
    "Parallel": { "type": "object", "properties": { "tasks": { "type": "array", "items": { "type": "array", "items": { "$ref": "#/defs/Step" } } }, "maxConcurrency": { "type": "integer", "minimum": 1 } }, "required": ["tasks"], "additionalProperties": false },
    "Call": { "type": "object", "properties": { "name": { "type": "string" }, "args": { "type": "object" }, "onError": { "enum": ["propagate","suppress"] } }, "required": ["name"], "additionalProperties": false },
    "FlowLike": { "type": "object", "properties": { "steps": { "type": "array", "items": { "$ref": "#/defs/Step" } } }, "required": ["steps"], "additionalProperties": false }
  }
}
```

## 19. AI Authoring Guide (Few-Shot)
- Always include: `dslVersion`, `id`, `name`, `steps`.
- Prefer YAML with explicit selector objects.
- Avoid free-form JS in `eval`; keep pure and small.
- Use `waitFor` before `click/type` unless selector has `waitFor: visible`.
- For loops over arrays, prefer `for` or `parallel` with `maxConcurrency`.
- Add `retry` to network-sensitive steps.

Prompt example:
"Generate a YAML Task Flow DSL v1.0 to login to example.com, search for three keywords, extract top 10 results (title/link), and emit results. Use waits, retries, and a 3-parallel search."

## 20. Validation Checklist
- Top-level required fields present
- No unknown fields
- All selectors valid
- Control-flow blocks have required children
- Timeouts within limits
- No secrets in logs/emit

## 21. Roadmap (DSL)
- V1: Core actions, control flow, schema, validator, runner
- V1.1: Upload/download robust, file dialogs, dialog-handlers
- V1.2: Built-in paginator helper, infinite scroll helper
- V2: HTTP client steps, data pipes, external API calls (guarded)
