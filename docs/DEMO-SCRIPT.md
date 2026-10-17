# Demo script (about 6 minutes)

Start the app (`dotnet run --project src/Reflect.Web`) and open http://localhost:5075. For a live AI brief, set `ANTHROPIC_API_KEY` first. Without it, the rule-based brief still shows the flow.

## 1. The problem, in one screen (45 s): Dashboard

- "A clinician using Reflect today has to rely on what the patient remembers to tell them. This shows what actually happened between sessions."
- Point at the KPI row: active patients, sessions this week, how many **need attention**, and RTM periods ready to sign.
- **Needs attention** shows one row per patient, most urgent first. Darnell Brooks has a positive PHQ-9 item 9.
- **Caseload engagement**: VR is solid and mobile is striped. That pattern survives color-blind mode.

## 2. A patient who needs attention (90 s): Darnell Brooks

- The safety banner appears at the top, with the reminder that the dashboard isn't real-time crisis monitoring.
- **Clinical insights**: the safety flag, PHQ-9 up 5 points, and 7 days without activity. Each says why it fired.
- Click **Generate brief**. It's a pre-session summary built from de-identified data only. The badge says whether AI or the rules wrote it.
- **Therapeutic response** tab: the PHQ-9/GAD-7 trend over labelled severity bands, the item-9 column, and mood before and after each session.
- **Sessions** tab: every VR and mobile session with module, duration and mood change.

## 3. RTM as a clinical workflow (90 s): Naomi Fischer

- **RTM** tab: the monitoring-status table from the challenge brief, filled in live: data days, sessions, engagement, therapeutic-response entries, provider reviews, patient communications, management time, documentation status.
- **Billing readiness**: 98975 / 98978 / 98980, with the evidence for each, using the 2026 code set (including the new 2-15 day code and the 10-minute code).
- Click **Sign off period**, then confirm. The toast appears, the period moves to *Signed*, and the codes are attested.

## 4. Working the list (45 s): RTM worklist

- Filter to **At risk**. Each row gives the reason, for example "Needs 6 more data days in the next 9 days" or "No real-time interactive communication logged this month".
- Click **Log time** for an at-risk patient (Sofia Petrova, for example): 12 minutes, interactive communication, phone, then **Save**. Back on the patient's RTM tab, the minutes and live call update.

## 5. Is Reflect helping? (30 s): Outcomes report

- Average mood change per session, mood change by module (VR modules lead in this synthetic data), and PHQ-9 change by engagement level.
- Point out the caveat on the page: correlational, not causal.

## 6. Built to merge, built for HIPAA (60 s)

- Open the profile menu and switch to **Jordan Reyes**: a different caseload. Paste one of Dr. Okafor's patient URLs and it returns *not found*, so the record's existence isn't revealed.
- Switch to **Sam Patel** (End User): *Access denied*.
- Switch back, then open a patient's **Access log** tab: every view and every AI brief is recorded.
- Toggle **dark mode** and open the **accessibility panel**: text size scales everything, charts included.
- Close with the spec compliance: one Area, thin controller / fat ViewModel, stand-ins isolated for deletion, vendored libraries, strict nonce CSP with zero violations, and 83 tests that check the spec's review rules automatically.
