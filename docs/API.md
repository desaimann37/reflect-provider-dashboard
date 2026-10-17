# APIs

This covers three things:
- the HTTP endpoints this extension adds
- the domain contracts behind them
- the data contracts it expects from the Reflect apps and the production pipeline: Firebase/BigQuery events and proposed Reflect.Api endpoints

## 1. Endpoints in the Provider area

Every Provider endpoint requires the `ProviderOnly` policy and returns `Cache-Control: no-store`. Every POST requires the anti-forgery token.

| Method | URL | Returns |
|---|---|---|
| GET | `/Provider/Home/Dashboard` | Dashboard page |
| GET | `/Provider/Patients/Index?filter=all\|attention\|atrisk\|ready` | Caseload page |
| GET | `/Provider/Patients/Details/{patientId}?tab=overview\|sessions\|response\|rtm\|notes\|careplan\|access` | Patient page; writes a `PhiAccessLog` row |
| GET | `/Provider/Patients/EngagementData/{patientId}?weeks=4\|12\|26` | JSON, below |
| POST | `/Provider/Patients/Brief/{patientId}` | JSON, below; writes a `PhiAccessLog` row |
| GET, POST | `/Provider/Rtm/LogTime?patientId={id}` | Form; POST redirects to `Details?tab=rtm` with a toast |
| POST | `/Provider/Rtm/SignOff` (`PatientId`, `PeriodStart` as `yyyy-MM-dd`) | Redirects to `Details?tab=rtm` with a toast |
| GET | `/Provider/Rtm/Index?filter=open\|ready\|atrisk\|ontrack\|signed\|all` | RTM worklist |
| GET, POST | `/Provider/Notes/Create?patientId={id}` | Form; POST redirects to `Details?tab=notes` |
| GET, POST | `/Provider/CarePlan/Edit/{patientId}` | Form; POST redirects to `Details?tab=careplan` |
| GET | `/Provider/Reports/Outcomes` | Outcomes report |

Stand-in only (removed on merge): `POST /DemoUser/Switch` and `GET /Account/AccessDenied`.

### `GET /Provider/Patients/EngagementData/{id}?weeks=12`

```json
{
  "weeks": 12,
  "labels": ["7/27", "8/3", "...", "10/12"],
  "vrMinutes": [0, 43.1, "..."],
  "mobileMinutes": [11.6, 14.9, "..."],
  "vrSessions": [0, 2, "..."],
  "mobileSessions": [1, 1, "..."],
  "summary": "Weekly minutes over the last 12 weeks: 478 in VR and 149 on mobile (35 sessions)."
}
```

### `POST /Provider/Patients/Brief/{id}`

Form body: `__RequestVerificationToken=…`

```json
{
  "source": "rules",
  "modelName": "",
  "headline": "3 items to review: PHQ-9 item 9 positive.",
  "highlights": ["0 sessions in the last 7 days (below the goal of 3); 11 sessions and 177 minutes in the last 30 days.", "..."],
  "suggestedTopics": ["Follow up on: PHQ-9 item 9 positive.", "..."],
  "notice": "Composed from monitoring data by fixed rules. Verify against the record before acting.",
  "generatedAt": "10/17/2026 12:20 AM"
}
```

`source` is `"ai"` (and `modelName` is set) when Claude wrote the brief.

### Errors

| Case | MVC page | AJAX |
|---|---|---|
| Patient missing or in another provider's caseload | 404 "We couldn't find that" | 404 `{"error":"The requested record was not found."}` |
| User lacks the Provider role | 302 to `/Account/AccessDenied` | same |
| Missing or invalid anti-forgery token | 400 | 400 |
| Validation failure on a form | The form re-renders with field messages | n/a |

## 2. Domain contracts

All are in `Reflect.Domain/Interfaces`. Methods are async and take a `CancellationToken`, and every query excludes soft-deleted rows.

| Interface | Methods |
|---|---|
| `IProviderProfileRepository` | `GetByUserIdAsync(userId)`, `GetByIdAsync(id)` |
| `IPatientRepository` | `GetByProviderAsync(providerId)`, `GetForProviderAsync(patientId, providerId)` (null if not in the caseload) |
| `IPatientAppLinkRepository` | `GetByPatientIdsAsync(ids)` |
| `ICarePlanRepository` | `GetByPatientIdAsync`, `GetByPatientIdsAsync`, `UpdateAsync` |
| `IObservationRepository` | `GetByPatientIdsAsync(ids, fromUtc, toUtc)`, `AddAsync` |
| `IProviderTimeEntryRepository` | `GetByPatientIdsAsync(ids, from, to)`, `AddAsync` |
| `IClinicalNoteRepository` | `GetByPatientIdAsync`, `GetByPatientIdsAsync(ids, fromUtc)`, `AddAsync` |
| `IRtmPeriodReviewRepository` | `GetByPatientIdsAsync`, `GetAsync(patientId, periodStart)`, `AddAsync`, `UpdateAsync` |
| `IPhiAccessLogRepository` | `AddAsync`, `GetRecentByPatientIdAsync(patientId, take)` |
| `IEngagementSessionRepository` | `GetByAppUserKeysAsync(keys, fromUtc, toUtc)` |
| `IAnalyticsEventSource` | `GetEventsAsync(fromUtc, toUtc)` |
| `IClinicalBriefGenerator` | `GenerateAsync(BriefInput)` |

The real app also has `ICurrentUserService`, `BaseEntity` and `EntityNotFoundException`. These are reproduced exactly as the spec defines them.

## 3. Analytics event contract (Reflect apps → Firebase)

The dashboard needs only four custom events. **Parameters must never contain PHI**: no names, free text, mood values or questionnaire answers.

| Event | Parameters | Sent when |
|---|---|---|
| `module_start` | `reflect_session_id`, `module_id`, `module_name` | A module or experience begins |
| `creation_saved` | `reflect_session_id` | The patient saves a creation |
| `module_complete` | `reflect_session_id`, `duration_sec` | The module finishes normally |
| `module_exit` | `reflect_session_id` | The patient leaves early |

- **User id:** set Firebase's `user_id` to the pseudonymous app key that `PatientAppLink.AppUserKey` stores. The demo uses `user_pseudo_id`; production should prefer `user_id`, so the key survives reinstalls and is the same across devices.
- **App id:** `app_info.id` tells the two apps apart (`com.persistenttech.reflectxr` and `com.persistenttech.reflectmobile` in the demo).

### BigQuery sync query

This is what the scheduled job in `Reflect.Infrastructure` would run before handing rows to `EngagementEventMapper.Map`:

```sql
SELECT
  user_id                                  AS app_user_key,
  event_name,
  event_timestamp,                          -- microseconds since epoch
  app_info.id                               AS app_id,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'reflect_session_id') AS reflect_session_id,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'module_id')          AS module_id,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'module_name')        AS module_name,
  (SELECT value.int_value    FROM UNNEST(event_params) WHERE key = 'duration_sec')       AS duration_sec
FROM `reflect-prod.analytics_<property_id>.events_*`
WHERE _TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', DATE_SUB(CURRENT_DATE('America/New_York'), INTERVAL 2 DAY))
                        AND FORMAT_DATE('%Y%m%d', CURRENT_DATE('America/New_York'))
  AND event_name IN ('module_start', 'creation_saved', 'module_complete', 'module_exit')
  AND user_id IS NOT NULL
```

- **Re-processing window:** the job re-processes a rolling 2-day window and upserts on `(AppUserKey, SourceSessionId)`, so late-arriving events are picked up.
- **Same-day data:** use `events_intraday_*`.
- **Access:** a dedicated service account with read-only access to the dataset.

## 4. Proposed Reflect.Api endpoints (apps → backend)

Therapeutic-response data is PHI once linked to a patient, so it goes to Reflect.Api, not Firebase. These endpoints are proposals for the PTI team; the demo seeds the equivalent rows.

### Record a mood check-in

`POST /api/v1/me/observations`

The caller is an authenticated app user, or a VR device credential from PIN pairing.

```json
{
  "code": "reflect-mood-pre",
  "system": "urn:reflect:observation",
  "value": 4,
  "unit": "score",
  "effectiveAt": "2026-10-16T22:05:00Z",
  "platform": "VirtualReality",
  "sessionId": "8f3a1c0d9b2e4f61"
}
```

The server resolves the patient from the user and rejects the request unless `PatientAppLink` consent exists. It stores an `Observation` with `Source = PatientReported`.

### Submit a questionnaire

`POST /api/v1/me/questionnaires/phq9`

```json
{ "answers": [1, 2, 1, 2, 1, 1, 2, 1, 1], "completedAt": "2026-10-15T14:10:00Z" }
```

The server scores it. It stores the total (`44261-6`) and item 9 (`44260-8`) as observations. GAD-7 works the same way, at `/questionnaires/gad7`, scored to `70274-6`. Scoring server-side keeps one implementation and lets the server raise the item-9 flag reliably.

### Future: Remote Physiologic Monitoring

`POST /api/v1/devices/{deviceId}/observations`

```json
{ "code": "8867-4", "system": "http://loinc.org", "value": 72, "unit": "/min", "effectiveAt": "2026-10-16T22:10:00Z" }
```

These land in the same `Observation` table with `Source = Device`. No schema change is needed.

## 5. External services

| Service | Used by | In the demo |
|---|---|---|
| Anthropic Messages API (official .NET SDK, `claude-opus-5-5`) | `ClaudeClinicalBriefGenerator` | Optional: only when `Anthropic:ApiKey` or `ANTHROPIC_API_KEY` is set. Server-side; sends only `BriefInput` (de-identified) |
| Firebase Analytics + BigQuery | Production sync job | Not called; replaced by synthetic GA4-shaped events |
| Microsoft Entra External ID | Real sign-in | Replaced by the demo sign-in stand-in |

An example of everything sent to the model (`BriefInput`):

```json
{
  "ageBand": "40-49",
  "programName": "Mood care",
  "diagnosisDescription": "Major depressive disorder, single episode, moderate",
  "daysSinceMonitoringStart": 95,
  "targetSessionsPerWeek": 3,
  "sessions7Days": 0,
  "sessions30Days": 11,
  "minutes30Days": 177,
  "vrSessions30Days": 5,
  "mobileSessions30Days": 6,
  "daysSinceLastSession": 7,
  "topModules": ["Learn (mobile)", "Explore Emotions (VR)", "Explore Journey (VR)"],
  "moodLift30Days": 0.7,
  "moodCheckIns30Days": 7,
  "phq9": [{ "daysAgo": 39, "score": 9, "severity": "Mild" }, { "daysAgo": 2, "score": 15, "severity": "Moderately severe" }],
  "gad7": [{ "daysAgo": 2, "score": 13, "severity": "Moderate" }],
  "flags": ["PHQ-9 item 9 positive", "PHQ-9 up 5 points", "No Reflect activity in 7 days"],
  "rtmSummary": "RTM period: 1 of 16 data days with 25 days left; 6 management minutes this month, no live call yet."
}
```
