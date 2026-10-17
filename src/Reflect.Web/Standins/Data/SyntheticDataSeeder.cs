using System.Globalization;
using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Standins.Auth;

namespace Reflect.Web.Standins.Data;

/// <summary>
/// STAND-IN. Generates realistic, entirely synthetic data relative to "now", so the demo always
/// looks current. Names, MRNs and e-mail domains are fictional (.example). App activity is emitted
/// as raw analytics events in the Firebase / GA4 BigQuery export shape, and the sync step turns those
/// into sessions exactly as production would.
/// </summary>
public static class SyntheticDataSeeder
{
    public static readonly Guid RiverbendFacilityId = Guid.Parse("6b0d7f2a-1c3e-4a5b-8c9d-0e1f2a3b4c01");
    public static readonly Guid HarborLightFacilityId = Guid.Parse("6b0d7f2a-1c3e-4a5b-8c9d-0e1f2a3b4c02");

    private static readonly (string Key, string Name, double LiftBonus)[] VrModules =
    {
        ("xr.creative_calm", "Creative Calm", 0.4),
        ("xr.explore_emotions", "Explore Emotions", 0.2),
        ("xr.explore_self", "Explore Self", 0.1),
        ("xr.explore_journey", "Explore Journey", 0.3)
    };

    private static readonly (string Key, string Name, double LiftBonus)[] MobileModules =
    {
        ("mobile.daily_creation", "Daily Creation", 0.1),
        ("mobile.create", "Create", 0.0),
        ("mobile.reflect", "Reflect", 0.2),
        ("mobile.learn", "Learn", -0.3),
        ("mobile.creative_calm", "Creative Calm", 0.3)
    };

    private enum Persona
    {
        SteadyImprover,
        SafetyConcern,
        VrFocused,
        Declining,
        MobileDaily,
        Lapsed,
        Plateau,
        NewEnrollee,
        Sporadic,
        ReadyToSign,
        MoodDecline,
        Gad7Improver,
        Paused
    }

    private sealed record PatientSpec(
        string First, string Last, string Pronouns, int Age, Persona Persona, string Dx, string Program,
        int StartDaysAgo, double VrShare, bool OkaforCaseload);

    private sealed record Profile(
        double WeeklySessions,
        Func<int, double> Multiplier,
        int IdleDays,
        Func<int, double> Phq,
        Func<int, double> Gad,
        Func<int, double> MoodPre,
        double MoodLift,
        double CheckInRate,
        int[] ExtraMeasureDaysAgo,
        bool Item9Positive,
        int ReviewMinutes,
        int CallMinutes,
        bool SetupDocumented,
        string Goals,
        string RecentNote);

    private static readonly PatientSpec[] Specs =
    {
        new("Alicia", "Moreno", "she/her", 34, Persona.SteadyImprover, "F41.1", "Anxiety care", 118, 0.5, true),
        new("Darnell", "Brooks", "he/him", 41, Persona.SafetyConcern, "F32.1", "Mood care", 95, 0.4, true),
        new("Priya", "Raman", "she/her", 27, Persona.VrFocused, "F43.10", "Trauma recovery", 72, 0.85, true),
        new("Ethan", "Caldwell", "he/him", 52, Persona.Declining, "F33.1", "Mood care", 140, 0.45, true),
        new("Grace", "Liu", "she/her", 23, Persona.MobileDaily, "F41.1", "Anxiety care", 64, 0.05, true),
        new("Marcus", "Hale", "he/him", 38, Persona.Lapsed, "F43.23", "Adjustment support", 101, 0.35, true),
        new("Sofia", "Petrova", "she/her", 45, Persona.Plateau, "F32.1", "Mood care", 88, 0.3, true),
        new("Jamal", "Wright", "he/him", 30, Persona.NewEnrollee, "F41.1", "Anxiety care", 8, 0.5, true),
        new("Hannah", "Kim", "she/her", 61, Persona.Sporadic, "F43.23", "Adjustment support", 47, 0.2, true),
        new("Luis", "Ortega", "he/him", 29, Persona.SteadyImprover, "F43.10", "Trauma recovery", 130, 0.7, true),
        new("Naomi", "Fischer", "she/her", 36, Persona.ReadyToSign, "F41.1", "Anxiety care", 58, 0.5, true),
        new("Owen", "Gallagher", "he/him", 47, Persona.MoodDecline, "F32.1", "Mood care", 76, 0.4, true),
        new("Rosa", "Delgado", "she/her", 55, Persona.Gad7Improver, "F41.1", "Anxiety care", 110, 0.35, true),
        new("Theo", "Nakamura", "they/them", 25, Persona.MobileDaily, "F43.23", "Adjustment support", 39, 0.1, true),
        new("Brianna", "Scott", "she/her", 33, Persona.Paused, "F32.1", "Mood care", 120, 0.3, true),
        new("Kevin", "O'Neil", "he/him", 66, Persona.VrFocused, "F33.1", "Mood care", 83, 0.8, true),
        new("Amara", "Nwosu", "she/her", 31, Persona.Gad7Improver, "F41.1", "Anxiety care", 70, 0.4, false),
        new("Daniel", "Weiss", "he/him", 44, Persona.Declining, "F32.1", "Mood care", 92, 0.3, false),
        new("Leah", "Thompson", "she/her", 28, Persona.SteadyImprover, "F43.10", "Trauma recovery", 55, 0.2, false),
        new("Victor", "Alvarez", "he/him", 58, Persona.VrFocused, "F43.23", "Adjustment support", 66, 0.9, false),
        new("Mei", "Chen", "she/her", 39, Persona.Sporadic, "F41.1", "Anxiety care", 40, 0.3, false),
        new("Isaac", "Turner", "he/him", 21, Persona.NewEnrollee, "F32.1", "Mood care", 12, 0.5, false),
        new("Fatima", "Haddad", "she/her", 50, Persona.Plateau, "F33.1", "Mood care", 99, 0.35, false)
    };

    public static void Seed(InMemoryStore store, DateTimeOffset now)
    {
        var idRng = new Random(7_771);
        var rng = new Random(20_261_016);
        var today = now.ToEasternDate();

        lock (store.Sync)
        {
            store.Providers.Clear();
            store.Patients.Clear();
            store.AppLinks.Clear();
            store.CarePlans.Clear();
            store.Observations.Clear();
            store.TimeEntries.Clear();
            store.Notes.Clear();
            store.Reviews.Clear();
            store.AccessLog.Clear();
            store.AnalyticsEvents.Clear();
            store.Sessions.Clear();

            var okafor = new ProviderProfile
            {
                Id = NewGuid(idRng),
                UserId = DemoUsers.Okafor.UserId,
                FacilityId = RiverbendFacilityId,
                FacilityName = "Riverbend Behavioral Health",
                FirstName = "Maya",
                LastName = "Okafor",
                Credentials = "PhD",
                Title = "Licensed Psychologist",
                Email = DemoUsers.Okafor.Email,
                DateCreated = now.AddDays(-400)
            };
            var reyes = new ProviderProfile
            {
                Id = NewGuid(idRng),
                UserId = DemoUsers.Reyes.UserId,
                FacilityId = HarborLightFacilityId,
                FacilityName = "Harbor Light Counseling",
                FirstName = "Jordan",
                LastName = "Reyes",
                Credentials = "LCSW",
                Title = "Licensed Clinical Social Worker",
                Email = DemoUsers.Reyes.Email,
                DateCreated = now.AddDays(-400)
            };
            store.Providers.Add(okafor);
            store.Providers.Add(reyes);

            var mrn = 204_811;
            foreach (var spec in Specs)
            {
                var provider = spec.OkaforCaseload ? okafor : reyes;
                var profile = ProfileFor(spec);
                var start = today.AddDays(-spec.StartDaysAgo);
                var patientId = NewGuid(idRng);
                var appKey = "rx_" + Convert.ToHexString(BitConverter.GetBytes(idRng.NextInt64())).ToLowerInvariant()[..12];
                mrn += 137 + idRng.Next(40);

                var patient = new Patient
                {
                    Id = patientId,
                    FacilityId = provider.FacilityId,
                    PrimaryProviderId = provider.Id,
                    FirstName = spec.First,
                    LastName = spec.Last,
                    Pronouns = spec.Pronouns,
                    DateOfBirth = today.AddYears(-spec.Age).AddDays(-idRng.Next(1, 360)),
                    Mrn = (spec.OkaforCaseload ? "RB-" : "HL-") + mrn.ToString(CultureInfo.InvariantCulture),
                    Status = PatientStatus.Active,
                    DateCreated = start.AddDays(-7).StartOfEasternDayUtc()
                };
                store.Patients.Add(patient);

                var diagnosis = DiagnosisCatalog.Find(spec.Dx);
                store.CarePlans.Add(new CarePlan
                {
                    Id = NewGuid(idRng),
                    PatientId = patientId,
                    ProgramName = spec.Program,
                    PrimaryDiagnosisCode = diagnosis.Code,
                    PrimaryDiagnosisDescription = diagnosis.Description,
                    ConsentDate = start.AddDays(-2),
                    MonitoringStartDate = start,
                    SetupCompletedOn = profile.SetupDocumented ? start : null,
                    TargetSessionsPerWeek = spec.Persona == Persona.MobileDaily ? 5 : 3,
                    ReviewCadenceDays = spec.Persona is Persona.SafetyConcern or Persona.Declining ? 7 : 14,
                    Goals = profile.Goals,
                    Status = spec.Persona == Persona.Paused ? CarePlanStatus.Paused : CarePlanStatus.Active,
                    DateCreated = start.StartOfEasternDayUtc()
                });

                store.AppLinks.Add(new PatientAppLink
                {
                    Id = NewGuid(idRng),
                    PatientId = patientId,
                    AppUserKey = appKey,
                    LinkedAt = start.AddDays(-1).StartOfEasternDayUtc().AddHours(15),
                    ConsentedAt = start.AddDays(-2).StartOfEasternDayUtc().AddHours(14),
                    ConsentScope = "Reflect XR and Reflect Mobile engagement, mood check-ins, and questionnaires"
                });

                GenerateActivity(store, rng, patientId, appKey, spec, profile, start, today, now);
                GenerateTime(store, rng, patientId, provider, spec, profile, start, today);
                GenerateNotes(store, rng, patientId, provider, spec, profile, start, today, now);
                GenerateAccessLog(store, rng, patientId, provider, today, now);
            }
        }
    }

    /// <summary>Signs past periods. Runs after the session read model exists so data days are real.</summary>
    public static void SeedSignedReviews(InMemoryStore store, DateTimeOffset now)
    {
        var rules = RtmRules.Default;
        var today = now.ToEasternDate();
        lock (store.Sync)
        {
            foreach (var plan in store.CarePlans)
            {
                var patient = store.Patients.First(p => p.Id == plan.PatientId);
                var provider = store.Providers.First(p => p.Id == patient.PrimaryProviderId);
                var key = store.AppLinks.First(l => l.PatientId == patient.Id).AppUserKey;
                var sessions = store.Sessions.Where(s => s.AppUserKey == key).ToList();
                var observations = store.Observations.Where(o => o.PatientId == patient.Id).ToList();
                var qualifying = PatientMonitoringSummaryBuilder.QualifyingDays(sessions, observations).ToList();

                foreach (var period in RtmCalculator.PeriodsThrough(plan.MonitoringStartDate, today, rules).Where(p => p.End < today))
                {
                    var dataDays = RtmCalculator.CountDataDays(qualifying, period);
                    var month = RtmCalculator.CalendarMonth(period.End);
                    var entries = store.TimeEntries.Where(t => t.PatientId == patient.Id && t.ServiceDate >= month.Start && t.ServiceDate <= month.End).ToList();
                    var minutes = entries.Sum(t => t.Minutes);
                    var codes = new List<string>();
                    if (dataDays >= rules.FullSupplyMinDays) codes.Add(RtmCodes.DeviceSupplyFull);
                    else if (dataDays >= rules.ShortSupplyMinDays) codes.Add(RtmCodes.DeviceSupplyShort);
                    codes.AddRange(RtmCalculator.ManagementCodes(minutes, entries.Any(e => e.IsInteractiveCommunication), rules).Select(c => c.Label));

                    var signedDay = period.End.AddDays(2);
                    var signed = signedDay <= today && plan.Status == CarePlanStatus.Active || signedDay <= today.AddDays(-20);
                    store.Reviews.Add(new RtmPeriodReview
                    {
                        PatientId = patient.Id,
                        PeriodStart = period.Start,
                        PeriodEnd = period.End,
                        Status = signed ? DocumentationStatus.Signed : DocumentationStatus.InProgress,
                        DataDaysAtSignOff = dataDays,
                        ManagementMinutesAtSignOff = minutes,
                        CodesAttested = signed ? string.Join(", ", codes) : string.Empty,
                        SignedByProviderId = signed ? provider.Id : null,
                        SignedByName = signed ? provider.DisplayName : string.Empty,
                        SignedAt = signed ? signedDay.StartOfEasternDayUtc().AddHours(10).AddMinutes(15) : null,
                        DateCreated = period.End.StartOfEasternDayUtc()
                    });
                }
            }
        }
    }

    private static Profile ProfileFor(PatientSpec spec)
    {
        var s = spec.StartDaysAgo;
        Func<int, double> steady = _ => 1.0;
        return spec.Persona switch
        {
            Persona.SteadyImprover => new Profile(4, steady, 0, Linear(15, 7, s), Linear(14, 7, s), Linear(4.5, 6.5, s), 1.4, 0.8,
                Array.Empty<int>(), false, 8, 14, true,
                "Reduce worry intensity and avoidance. Use Creative Calm at least 3 times a week; practice grounding between sessions.",
                "Engagement steady at about four sessions a week. Mood lift after sessions remains consistent. PHQ-9 continues to trend down. Continue current plan."),
            Persona.SafetyConcern => new Profile(3, d => d < 10 ? 0.35 : 1.0, 0, d => d > 6 ? 11 - (s - d) / (double)s : 16, Linear(11, 13, s),
                d => d < 14 ? 3.9 : 5.6, 0.8, 0.75, new[] { 2 }, true, 6, 0, true,
                "Improve mood and daily activation. Weekly check-ins while symptoms are elevated; use Reflect Mobile on difficult evenings.",
                "Reported increased stress at work and poorer sleep. Fewer sessions over the last ten days. Plan to call this week to review."),
            Persona.VrFocused => new Profile(3, steady, 0, Linear(14, 9, s), Linear(12, 8, s), Linear(5, 6.5, s), 1.8, 0.8,
                Array.Empty<int>(), false, 7, 15, true,
                "Process difficult memories at a tolerable pace using Explore Journey in VR; build a calming routine with Creative Calm.",
                "Prefers VR sessions at home. Explore Journey sessions are longer and followed by the largest mood improvements. Discussed pacing."),
            Persona.Declining => new Profile(4, d => d < 7 ? 0.1 : d < 14 ? 0.5 : 1.0, 0, d => d > 6 ? 9.5 : 15, Linear(9, 12, s),
                d => d < 14 ? 4.4 : 6.0, 0.9, 0.75, new[] { 3 }, false, 5, 0, true,
                "Maintain gains from prior treatment episode; early warning plan for relapse signs; Reflect use 3 times a week.",
                "Engagement was strong until two weeks ago. Not yet reached by phone. Relapse-prevention plan on file."),
            Persona.MobileDaily => new Profile(6, steady, 0, Linear(11, 7, s), Linear(13, 8, s), Linear(5.5, 6.8, s), 0.9, 0.6,
                Array.Empty<int>(), false, 6, 12, true,
                "Short daily practice to interrupt worry spirals; Daily Creation each evening; reduce reassurance seeking.",
                "Uses Reflect Mobile almost daily, mostly short Daily Creation sessions. GAD-7 down 4 points since baseline."),
            Persona.Lapsed => new Profile(3, steady, 16, Linear(12, 11, s), Linear(10, 10, s), Linear(5, 5, s), 0.8, 0.7,
                Array.Empty<int>(), false, 0, 0, true,
                "Adjust to job loss; rebuild routine and social contact; Reflect sessions twice a week.",
                "Missed last appointment. Last app activity over two weeks ago. Outreach message left with front desk."),
            Persona.Plateau => new Profile(2.5, steady, 0, Linear(13, 12, s), Linear(11, 11, s), Linear(5, 5.2, s), 0.6, 0.7,
                Array.Empty<int>(), false, 8, 12, true,
                "Increase behavioral activation; schedule two pleasant activities a week; Reflect sessions twice a week.",
                "Scores stable for six weeks. Consider adjusting the plan: add a second VR session or a different module mix."),
            Persona.NewEnrollee => new Profile(4, steady, 0, Linear(14, 13, s), Linear(12, 12, s), Linear(5, 5.5, s), 1.0, 0.8,
                Array.Empty<int>(), false, 5, 0, true,
                "Onboard to Reflect XR and Reflect Mobile; establish baseline; aim for 3 sessions a week.",
                "Enrolled and completed app setup and education. Baseline PHQ-9 and GAD-7 collected in the app."),
            Persona.Sporadic => new Profile(0.9, steady, 0, Linear(10, 9, s), Linear(12, 11, s), Linear(5, 5, s), 1.1, 0.7,
                Array.Empty<int>(), false, 5, 0, spec.First != "Hannah",
                "Manage stress after relocation; practice Creative Calm when overwhelmed; aim for 2 to 3 sessions a week.",
                "Uses Reflect irregularly, mostly after stressful days. Discussed setting a reminder."),
            Persona.ReadyToSign => new Profile(6, steady, 0, Linear(16, 8, s), Linear(15, 7, s), Linear(4, 7, s), 1.6, 0.9,
                Array.Empty<int>(), false, 8, 16, true,
                "Reduce panic frequency and anticipatory anxiety; daily practice alternating VR and mobile.",
                "Highly engaged. PHQ-9 and GAD-7 both down by half since baseline. Period documentation ready for review."),
            Persona.MoodDecline => new Profile(3.5, steady, 0, Linear(10, 12, s), Linear(9, 11, s), d => d < 14 ? 4.6 : 6.6, 0.7, 0.8,
                Array.Empty<int>(), false, 6, 15, true,
                "Improve mood regulation during custody transition; Reflect sessions 3 times a week; journaling in Reflect.",
                "Pre-session mood check-ins lower over the last two weeks. Discussed stressors on last call."),
            Persona.Gad7Improver => new Profile(3.5, steady, 0, Linear(12, 6, s), Linear(16, 8, s), Linear(4.8, 6.6, s), 1.3, 0.8,
                Array.Empty<int>(), false, 7, 14, true,
                "Reduce generalized worry; use Explore Emotions to label feelings; practice worry postponement.",
                "GAD-7 down 8 points since baseline. Reports using Creative Calm before bed."),
            _ => new Profile(3, d => d < 20 ? 0 : 1.0, 20, Linear(14, 10, s), Linear(12, 10, s), Linear(5, 6, s), 1.0, 0.75,
                Array.Empty<int>(), false, 0, 0, true,
                "Paused at patient request during travel; resume monitoring on return.",
                "Monitoring paused at patient request while traveling. Plan to resume next month.")
        };
    }

    private static Func<int, double> Linear(double start, double end, int startDaysAgo) =>
        daysAgo => start + (end - start) * (1 - Math.Clamp(daysAgo / (double)Math.Max(1, startDaysAgo), 0, 1));

    private static void GenerateActivity(InMemoryStore store, Random rng, Guid patientId, string appKey, PatientSpec spec,
        Profile profile, DateOnly start, DateOnly today, DateTimeOffset now)
    {
        for (var day = start; day <= today; day = day.AddDays(1))
        {
            var daysAgo = today.DayNumber - day.DayNumber;
            if (daysAgo < profile.IdleDays)
            {
                continue;
            }

            var p = profile.WeeklySessions / 7.0 * profile.Multiplier(daysAgo);
            var count = rng.NextDouble() < p ? 1 : 0;
            if (count == 1 && p > 0.7 && rng.NextDouble() < 0.12)
            {
                count = 2;
            }

            for (var i = 0; i < count; i++)
            {
                var minuteOfDay = rng.NextDouble() < 0.7 ? 17 * 60 + rng.Next(0, 300) : 7 * 60 + rng.Next(0, 360);
                var startedAt = day.StartOfEasternDayUtc().AddMinutes(minuteOfDay + i * 95);
                if (startedAt > now.AddMinutes(-40))
                {
                    continue;
                }
                AddSession(store, rng, patientId, appKey, profile, startedAt, rng.NextDouble() < spec.VrShare, daysAgo);
            }
        }

        // Questionnaires every 14 days from the start, plus persona-specific extra points.
        var measureDays = new SortedSet<int>();
        for (var d = spec.StartDaysAgo; d >= 0; d -= 14)
        {
            measureDays.Add(d);
        }
        foreach (var extra in profile.ExtraMeasureDaysAgo)
        {
            measureDays.Add(extra);
        }
        if (profile.ExtraMeasureDaysAgo.Length > 0)
        {
            // Drop scheduled points that would crowd the extra one.
            measureDays.RemoveWhere(d => profile.ExtraMeasureDaysAgo.Any(e => d != e && Math.Abs(d - e) < 8));
        }

        var latestMeasure = measureDays.Count == 0 ? -1 : measureDays.Min;
        foreach (var daysAgo in measureDays)
        {
            if (daysAgo < profile.IdleDays)
            {
                continue;
            }
            var at = today.AddDays(-daysAgo).StartOfEasternDayUtc().AddHours(9).AddMinutes(rng.Next(0, 180));
            if (at > now)
            {
                at = now.AddMinutes(-90);
            }
            var phq = Clamp(Math.Round(profile.Phq(daysAgo) + rng.Next(-1, 2)), 0, 27);
            var gad = Clamp(Math.Round(profile.Gad(daysAgo) + rng.Next(-1, 2)), 0, 21);
            var item9 = profile.Item9Positive && daysAgo == latestMeasure ? 1 : 0;
            store.Observations.Add(Obs(patientId, ObservationCodes.Phq9Total, (decimal)phq, "score", at, ReflectPlatform.Mobile, string.Empty));
            store.Observations.Add(Obs(patientId, ObservationCodes.Phq9Item9, item9, "score", at, ReflectPlatform.Mobile, string.Empty));
            store.Observations.Add(Obs(patientId, ObservationCodes.Gad7Total, (decimal)gad, "score", at.AddMinutes(3), ReflectPlatform.Mobile, string.Empty));
        }
    }

    private static void AddSession(InMemoryStore store, Random rng, Guid patientId, string appKey, Profile profile,
        DateTimeOffset startedAt, bool vr, int daysAgo)
    {
        var modules = vr ? VrModules : MobileModules;
        var module = modules[rng.Next(modules.Length)];
        var minutes = vr ? 14 + rng.NextDouble() * 22 : 4 + rng.NextDouble() * 12;
        var completed = rng.NextDouble() < (vr ? 0.93 : 0.86);
        var actualSeconds = (int)(minutes * 60 * (completed ? 1 : 0.4 + rng.NextDouble() * 0.4));
        var creations = completed ? (vr ? rng.Next(1, 4) : rng.Next(0, 3)) : 0;
        var sessionId = Convert.ToHexString(BitConverter.GetBytes(rng.NextInt64())).ToLowerInvariant();
        var appId = vr ? AnalyticsEventNames.ReflectXrAppId : AnalyticsEventNames.ReflectMobileAppId;

        AddEvent(store, appKey, AnalyticsEventNames.ModuleStart, startedAt, appId, sessionId,
            (AnalyticsEventNames.ParamModuleId, module.Key), (AnalyticsEventNames.ParamModuleName, module.Name));
        for (var c = 0; c < creations; c++)
        {
            AddEvent(store, appKey, AnalyticsEventNames.CreationSaved, startedAt.AddSeconds(actualSeconds * (c + 1) / (creations + 1.0)), appId, sessionId);
        }
        var endAt = startedAt.AddSeconds(actualSeconds);
        if (completed)
        {
            AddEvent(store, appKey, AnalyticsEventNames.ModuleComplete, endAt, appId, sessionId,
                (AnalyticsEventNames.ParamDurationSeconds, actualSeconds.ToString(CultureInfo.InvariantCulture)));
        }
        else
        {
            AddEvent(store, appKey, AnalyticsEventNames.ModuleExit, endAt, appId, sessionId);
        }

        if (rng.NextDouble() < profile.CheckInRate)
        {
            var platform = vr ? ReflectPlatform.VirtualReality : ReflectPlatform.Mobile;
            var pre = Clamp(Math.Round(profile.MoodPre(daysAgo) + (rng.NextDouble() * 2 - 1)), 1, 9);
            store.Observations.Add(Obs(patientId, ObservationCodes.MoodPre, (decimal)pre, "score", startedAt, platform, sessionId));
            if (completed)
            {
                var lift = profile.MoodLift + module.LiftBonus + (vr ? 0.4 : 0) + (rng.NextDouble() * 2 - 1);
                var post = Clamp(Math.Round(pre + lift), 1, 10);
                store.Observations.Add(Obs(patientId, ObservationCodes.MoodPost, (decimal)post, "score", endAt, platform, sessionId));
            }
        }
    }

    private static void AddEvent(InMemoryStore store, string appKey, string name, DateTimeOffset at, string appId, string sessionId,
        params (string Key, string Value)[] extra)
    {
        var parameters = new Dictionary<string, string> { [AnalyticsEventNames.ParamSessionId] = sessionId };
        foreach (var (key, value) in extra)
        {
            parameters[key] = value;
        }
        store.AnalyticsEvents.Add(new AnalyticsEvent(appKey, name, at.ToUnixTimeMilliseconds() * 1000, appId, parameters));
    }

    private static void GenerateTime(InMemoryStore store, Random rng, Guid patientId, ProviderProfile provider, PatientSpec spec,
        Profile profile, DateOnly start, DateOnly today)
    {
        var currentMonth = RtmCalculator.CalendarMonth(today);
        for (var month = new DateOnly(start.Year, start.Month, 1); month < currentMonth.Start; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            if (spec.Persona == Persona.Paused && monthEnd > today.AddDays(-20))
            {
                continue;
            }
            AddTime(store, patientId, provider, Within(month.AddDays(8), start, monthEnd), 6 + rng.Next(0, 4), TimeActivity.DataReview, CommunicationMethod.None,
                "Reviewed engagement and check-in trends.");
            AddTime(store, patientId, provider, Within(month.AddDays(17), start, monthEnd), 12 + rng.Next(0, 7), TimeActivity.InteractiveCommunication,
                rng.NextDouble() < 0.5 ? CommunicationMethod.Phone : CommunicationMethod.Video, "Reviewed progress and adjusted goals with patient.");
        }

        var from = start > currentMonth.Start ? start : currentMonth.Start;
        if (from > today)
        {
            return;
        }
        if (profile.ReviewMinutes > 0)
        {
            AddTime(store, patientId, provider, Within(currentMonth.Start.AddDays(4), from, today), profile.ReviewMinutes, TimeActivity.DataReview,
                CommunicationMethod.None, spec.Persona == Persona.NewEnrollee ? "Reviewed baseline questionnaires and first sessions." : "Reviewed engagement and check-in trends.");
        }
        if (profile.CallMinutes > 0)
        {
            AddTime(store, patientId, provider, Within(currentMonth.Start.AddDays(10), from, today.AddDays(-1)), profile.CallMinutes, TimeActivity.InteractiveCommunication,
                spec.Persona == Persona.VrFocused ? CommunicationMethod.Video : CommunicationMethod.Phone, "Discussed recent sessions, mood check-ins, and next steps.");
        }
    }

    private static DateOnly Within(DateOnly preferred, DateOnly min, DateOnly max) =>
        preferred < min ? min : preferred > max ? (max < min ? min : max) : preferred;

    private static void AddTime(InMemoryStore store, Guid patientId, ProviderProfile provider, DateOnly date, int minutes, TimeActivity activity,
        CommunicationMethod method, string notes)
    {
        store.TimeEntries.Add(new ProviderTimeEntry
        {
            PatientId = patientId,
            ProviderId = provider.Id,
            ServiceDate = date,
            Minutes = minutes,
            Activity = activity,
            IsInteractiveCommunication = method != CommunicationMethod.None,
            CommunicationMethod = method,
            Notes = notes,
            DateCreated = date.StartOfEasternDayUtc().AddHours(16)
        });
    }

    private static void GenerateNotes(InMemoryStore store, Random rng, Guid patientId, ProviderProfile provider, PatientSpec spec,
        Profile profile, DateOnly start, DateOnly today, DateTimeOffset now)
    {
        void Add(DateOnly day, NoteType type, string title, string body)
        {
            var at = day.StartOfEasternDayUtc().AddHours(13).AddMinutes(rng.Next(0, 240));
            store.Notes.Add(new ClinicalNote
            {
                PatientId = patientId,
                ProviderId = provider.Id,
                AuthorName = provider.DisplayName,
                NoteType = type,
                Title = title,
                Body = body,
                AuthoredAt = at > now ? now.AddMinutes(-30) : at,
                DateCreated = at > now ? now.AddMinutes(-30) : at
            });
        }

        Add(start, NoteType.TreatmentPlanChange, "Enrolled in Reflect RTM",
            $"Patient consented to share Reflect XR and Reflect Mobile data with the care team. App setup and education completed. Program: {spec.Program}.");
        if (spec.StartDaysAgo > 30)
        {
            Add(start.AddDays(Math.Min(28, spec.StartDaysAgo - 10)), NoteType.DataReview, "Monthly data review",
                "Reviewed four weeks of engagement and mood check-ins. Patient is using both VR and mobile modules. Baseline questionnaires recorded.");
        }
        Add(today.AddDays(-Math.Min(spec.StartDaysAgo, 3 + rng.Next(0, 5))), NoteType.DataReview, "Data review", profile.RecentNote);
    }

    private static void GenerateAccessLog(InMemoryStore store, Random rng, Guid patientId, ProviderProfile provider, DateOnly today, DateTimeOffset now)
    {
        var views = 2 + rng.Next(0, 4);
        for (var i = 0; i < views; i++)
        {
            var at = today.AddDays(-rng.Next(1, 28)).StartOfEasternDayUtc().AddHours(9 + rng.Next(0, 9)).AddMinutes(rng.Next(0, 60));
            store.AccessLog.Add(new PhiAccessLog
            {
                PatientId = patientId,
                UserId = provider.UserId,
                UserDisplayName = provider.DisplayName,
                Action = i % 3 == 2 ? "Logged management time" : "Viewed patient record",
                Path = "/Provider/Patients/Details",
                OccurredAt = at > now ? now.AddHours(-2) : at,
                DateCreated = at > now ? now.AddHours(-2) : at
            });
        }
    }

    private static Observation Obs(Guid patientId, string code, decimal value, string unit, DateTimeOffset at, ReflectPlatform platform, string sessionId) => new()
    {
        PatientId = patientId,
        CodeSystem = ObservationCodes.SystemFor(code),
        Code = code,
        Display = ObservationCodes.DisplayFor(code),
        Value = value,
        Unit = unit,
        Source = ObservationSource.PatientReported,
        Platform = platform,
        SourceSessionId = sessionId,
        EffectiveAt = at,
        DateCreated = at
    };

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private static Guid NewGuid(Random rng)
    {
        var bytes = new byte[16];
        rng.NextBytes(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
