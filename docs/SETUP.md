# Setup

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should print `10.0.x`).
- That's all. There is no database, Node or npm toolchain, and no secrets. Every front-end library is vendored under `src/Reflect.Web/wwwroot/assets/libs/`.

## Build, run, test

```bash
dotnet build                               # whole solution (ReflectProvider.slnx)
dotnet run --project src/Reflect.Web       # http://localhost:5075
dotnet test                                # 83 tests
```

To use a different port: `dotnet run --project src/Reflect.Web --urls http://localhost:5080`.

Razor runtime compilation is on, as in the real app, so `.cshtml` edits apply on refresh without a rebuild. C# changes need a restart.

## Demo users

The demo signs you in automatically as **Dr. Maya Okafor**. To change user, open the profile menu (avatar, top right) and choose **Switch demo user**:

- **Dr. Maya Okafor, PhD**: Riverbend Behavioral Health, 16 patients.
- **Jordan Reyes, LCSW**: Harbor Light Counseling, 7 different patients. Opening one of Dr. Okafor's patient URLs returns *not found*.
- **Sam Patel**: an End User with no Provider role. Every Provider page redirects to *Access denied*.

## Synthetic data

On each start, `SyntheticDataSeeder` generates two providers and 23 fictional patients. The data covers 8 to 140 days of history, ending today:
- raw GA4-shaped app events, turned into sessions by the real mapper
- mood check-ins, PHQ-9 and GAD-7 every two weeks
- management time, notes and signed past periods

The generation is deterministic, so the same personas appear every time.

| To see | Open |
|---|---|
| Safety flag, PHQ-9 worsening, low engagement | Darnell Brooks |
| Disengagement and relapse risk | Ethan Caldwell |
| Lapsed (16 days without activity) | Marcus Hale |
| RTM ready to sign | Naomi Fischer |
| Mood decline | Owen Gallagher |
| New enrollee | Jamal Wright |
| Paused care plan (not monitoring) | Brianna Scott |
| Steady improvement | Alicia Moreno, Rosa Delgado |

Changes you make (time entries, notes, care-plan edits, sign-offs) live in memory until the app stops. Restart to reset.

## Configuration (`src/Reflect.Web/appsettings.json`)

| Setting | Default | Effect |
|---|---|---|
| `Csp:StrictNonce` | `true` | Sends the real app's CSP with `script-src 'self' 'nonce-…'` (the stricter rule PTI is moving to). Set `false` to send the current policy with `'unsafe-inline'` exactly as written in the spec |
| `AiBrief:Enabled` | `true` | Use Claude for the pre-session brief *when a key is present*. Without a key the brief is always rule-based |
| `AiBrief:Model` | `claude-opus-5-5` | Model id for the brief |
| `Anthropic:ApiKey` | empty | API key. Prefer the `ANTHROPIC_API_KEY` environment variable or user secrets; don't commit a key |

To turn on the AI brief:

```bash
# PowerShell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
dotnet run --project src/Reflect.Web

# or, with user secrets
dotnet user-secrets init --project src/Reflect.Web
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..." --project src/Reflect.Web
```

The badge on the brief card then reads **AI draft · claude-opus-5-5**. Only de-identified aggregates are sent (see [API.md](API.md#5-external-services)). If the call fails, the rule-based brief is shown with a notice.

## Checking the UI rules yourself

- **Theme:** the moon/sun icon in the header toggles light and dark (`data-theme-mode` on `<html>`).
- **Accessibility:** the eye icon opens the accessibility panel: theme, color-blind mode (`data-a11y-colorblind="true"`), and text size from 87.5% to 150%.
- **Phone and tablet:** narrow the window below 992px for the off-canvas sidebar, and below 576px for full-width page buttons.
- **CSP:** with DevTools open, the console stays free of CSP violations on every page.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `The file is locked by: "Reflect.Web"` during build | The app is still running; stop it (Ctrl+C) and rebuild |
| Port 5075 in use | Pass `--urls http://localhost:<port>` |
| Fonts look like Arial | Hard-refresh; the `.woff2` files are served from `/fonts` |
| Charts empty after switching tabs | The tab-shown handler resizes them; if a browser extension blocks scripts, allow `localhost` |
