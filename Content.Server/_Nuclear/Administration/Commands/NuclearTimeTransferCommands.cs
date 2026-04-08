using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared.Administration;
using Content.Shared.Localizations;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Nuclear.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
internal sealed class TimeTransferAddGroupCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "timetransfer_addgroup";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("timetransfer-addgroup-error-args"));
            shell.WriteLine(Help);
            return;
        }

        if (!TimeTransferGroupCalculator.TryParseGroup(args[1], out var group))
        {
            shell.WriteError(Loc.GetString("timetransfer-addgroup-invalid-group", ("group", args[1])));
            return;
        }

        var located = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (located == null)
        {
            shell.WriteError(Loc.GetString("timetransfer-addgroup-player-not-found", ("player", args[0])));
            return;
        }

        IReadOnlyDictionary<string, TimeSpan> currentPlaytimes;
        if (_playerManager.TryGetSessionById(located.UserId, out var session) &&
            _playTimeTracking.TryGetTrackerTimes(session, out var livePlaytimes))
        {
            _playTimeTracking.FlushTracker(session);
            currentPlaytimes = livePlaytimes;
        }
        else
        {
            currentPlaytimes = await _playTimeTracking.GetTrackerTimesById(located.UserId);
        }

        var roleSystem = _entManager.EntitySysManager.GetEntitySystem<RoleSystem>();
        var calculator = new TimeTransferGroupCalculator(_proto, roleSystem);
        var result = calculator.Calculate(group, currentPlaytimes);

        foreach (var (tracker, time) in result.Additions.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (tracker == PlayTimeTrackingShared.TrackerOverall)
                await _playTimeTracking.AddTimeToOverallPlaytimeById(located.UserId, time);
            else
                await _playTimeTracking.AddTimeToTrackerById(located.UserId, tracker, time, updateOverall: false);
        }

        if (result.Additions.Count == 0)
        {
            shell.WriteLine(Loc.GetString(
                "timetransfer-addgroup-no-changes",
                ("group", TimeTransferGroupCalculator.GetGroupName(group)),
                ("player", located.Username)));
            return;
        }

        shell.WriteLine(Loc.GetString(
            "timetransfer-addgroup-success",
            ("group", TimeTransferGroupCalculator.GetGroupName(group)),
            ("player", located.Username),
            ("trackers", result.Additions.Count),
            ("time", ContentLocalizationManager.FormatPlaytime(result.TotalAdded))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "<ckey/userId>");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                TimeTransferGroupCalculator.GroupNames,
                "<all|extended>");
        }

        return CompletionResult.Empty;
    }
}

internal enum TimeTransferGroup
{
    All,
    Extended,
}

internal sealed class TimeTransferGroupCalculator
{
    private static readonly HashSet<string> CommandJobIds = new(StringComparer.Ordinal)
    {
        "Captain",
        "HeadOfPersonnel",
        "Quartermaster",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "ResearchDirector",
        "HeadOfSecurity",
        "BlueshieldOfficer",
        "NanotrasenRepresentative",
    };

    private static readonly Dictionary<string, string> PreferredDepartmentFeeders = new(StringComparer.Ordinal)
    {
        ["Cargo"] = "CargoTechnician",
        ["Civilian"] = "Passenger",
        ["Command"] = "Captain",
        ["Engineering"] = "StationEngineer",
        ["Medical"] = "MedicalDoctor",
        ["Science"] = "Scientist",
        ["Security"] = "SecurityOfficer",
        ["Silicon"] = "Borg",
        ["Specific"] = "Passenger",
    };

    public static readonly string[] GroupNames =
    {
        "all",
        "extended",
    };

    private readonly IPrototypeManager _proto;
    private readonly RoleSystem _roleSystem;

    public TimeTransferGroupCalculator(IPrototypeManager proto, RoleSystem roleSystem)
    {
        _proto = proto;
        _roleSystem = roleSystem;
    }

    public static string GetGroupName(TimeTransferGroup group)
    {
        return group switch
        {
            TimeTransferGroup.All => "all",
            TimeTransferGroup.Extended => "extended",
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };
    }

    public static bool TryParseGroup(string value, out TimeTransferGroup group)
    {
        switch (value.ToLowerInvariant())
        {
            case "all":
                group = TimeTransferGroup.All;
                return true;
            case "extended":
                group = TimeTransferGroup.Extended;
                return true;
            default:
                group = default;
                return false;
        }
    }

    public TimeTransferGrantResult Calculate(TimeTransferGroup group, IReadOnlyDictionary<string, TimeSpan> currentPlaytimes)
    {
        if (group == TimeTransferGroup.Extended)
            return CalculateExtended(currentPlaytimes);

        var visibleJobs = GetVisibleJobs();
        var plan = new TimeTransferGrantPlan();

        foreach (var job in visibleJobs)
        {
            AddAllJobRequirements(job, plan);
        }

        foreach (var job in visibleJobs)
        {
            AddRoleLoadoutRequirements(job, plan);
        }

        return BuildResult(plan, currentPlaytimes);
    }

    private TimeTransferGrantResult CalculateExtended(IReadOnlyDictionary<string, TimeSpan> currentPlaytimes)
    {
        var plan = new TimeTransferGrantPlan();
        var visibleJobs = GetVisibleJobs();

        foreach (var job in visibleJobs)
        {
            if (IsCommandJob(job))
                continue;

            AddAllJobRequirements(job, plan);
        }

        return BuildResult(plan, currentPlaytimes);
    }

    private List<JobPrototype> GetVisibleJobs()
    {
        return _proto.EnumeratePrototypes<JobPrototype>()
            .Where(job => job.SetPreference)
            .OrderBy(job => job.ID, StringComparer.Ordinal)
            .ToList();
    }

    private void AddAllJobRequirements(JobPrototype job, TimeTransferGrantPlan plan)
    {
        var requirements = _roleSystem.GetRoleRequirements(job);
        if (requirements == null)
            return;

        foreach (var requirement in requirements)
        {
            AddRequirement(plan, requirement);
        }
    }

    private void AddRoleLoadoutRequirements(JobPrototype job, TimeTransferGrantPlan plan)
    {
        if (!_proto.TryIndex<RoleLoadoutPrototype>(job.PlayTimeTracker, out var roleLoadout))
            return;

        foreach (var groupId in roleLoadout.Groups)
        {
            if (!_proto.TryIndex(groupId, out LoadoutGroupPrototype? loadoutGroup))
                continue;

            foreach (var loadoutId in loadoutGroup.Loadouts)
            {
                if (!_proto.TryIndex(loadoutId, out LoadoutPrototype? loadout))
                    continue;

                AddLoadoutRequirements(
                    loadout.Effects,
                    plan,
                    new HashSet<string>(StringComparer.Ordinal));
            }
        }
    }

    private void AddLoadoutRequirements(
        IEnumerable<LoadoutEffect> effects,
        TimeTransferGrantPlan plan,
        HashSet<string> visitedGroups)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case JobRequirementLoadoutEffect requirementEffect:
                    AddRequirement(plan, requirementEffect.Requirement);
                    break;
                case GroupLoadoutEffect groupEffect:
                {
                    var groupId = (string) groupEffect.Proto;
                    if (!visitedGroups.Add(groupId))
                        break;

                    var effectGroup = _proto.Index(groupEffect.Proto);
                    AddLoadoutRequirements(effectGroup.Effects, plan, visitedGroups);
                    break;
                }
            }
        }
    }

    private void AddRequirement(TimeTransferGrantPlan plan, JobRequirement requirement, TimeSpan? overrideTarget = null)
    {
        if (requirement.Inverted)
            return;

        switch (requirement)
        {
            case RoleTimeRequirement roleRequirement:
            {
                var tracker = (string) roleRequirement.Role;
                var target = overrideTarget ?? roleRequirement.Time;
                plan.RoleTargets[tracker] = Max(plan.RoleTargets.GetValueOrDefault(tracker), target);
                break;
            }
            case DepartmentTimeRequirement departmentRequirement:
            {
                var department = (string) departmentRequirement.Department;
                var target = overrideTarget ?? departmentRequirement.Time;
                plan.DepartmentTargets[department] = Max(plan.DepartmentTargets.GetValueOrDefault(department), target);
                break;
            }
            case OverallPlaytimeRequirement overallRequirement:
                plan.OverallTarget = Max(plan.OverallTarget, overrideTarget ?? overallRequirement.Time);
                break;
        }
    }

    private TimeTransferGrantResult BuildResult(
        TimeTransferGrantPlan plan,
        IReadOnlyDictionary<string, TimeSpan> currentPlaytimes)
    {
        var additions = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        foreach (var (tracker, target) in plan.RoleTargets)
        {
            var missing = target - currentPlaytimes.GetValueOrDefault(tracker);
            if (missing > TimeSpan.Zero)
                additions[tracker] = missing;
        }

        foreach (var (departmentId, target) in plan.DepartmentTargets.OrderBy(x => x.Key == "Command" ? 1 : 0))
        {
            var currentDepartmentTime = GetDepartmentTime(departmentId, currentPlaytimes, additions);
            var missing = target - currentDepartmentTime;
            if (missing <= TimeSpan.Zero)
                continue;

            var feederTracker = GetDepartmentFeederTracker(departmentId);
            additions[feederTracker] = additions.GetValueOrDefault(feederTracker) + missing;
        }

        var overallMissing = plan.OverallTarget - currentPlaytimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
        if (overallMissing > TimeSpan.Zero)
            additions[PlayTimeTrackingShared.TrackerOverall] = overallMissing;

        return new TimeTransferGrantResult(additions);
    }

    private TimeSpan GetDepartmentTime(
        string departmentId,
        IReadOnlyDictionary<string, TimeSpan> currentPlaytimes,
        IReadOnlyDictionary<string, TimeSpan> additions)
    {
        var total = TimeSpan.Zero;
        var department = _proto.Index<DepartmentPrototype>(departmentId);

        foreach (var jobId in department.Roles)
        {
            var tracker = _proto.Index(jobId).PlayTimeTracker;
            total += currentPlaytimes.GetValueOrDefault(tracker);
            total += additions.GetValueOrDefault(tracker);
        }

        return total;
    }

    private string GetDepartmentFeederTracker(string departmentId)
    {
        if (PreferredDepartmentFeeders.TryGetValue(departmentId, out var preferredJobId) &&
            _proto.TryIndex<JobPrototype>(preferredJobId, out var preferredJob))
        {
            return preferredJob.PlayTimeTracker;
        }

        var department = _proto.Index<DepartmentPrototype>(departmentId);
        foreach (var jobId in department.Roles)
        {
            if (_proto.TryIndex(jobId, out JobPrototype? job) && job.SetPreference)
                return job.PlayTimeTracker;
        }

        foreach (var jobId in department.Roles)
        {
            if (_proto.TryIndex(jobId, out JobPrototype? job))
                return job.PlayTimeTracker;
        }

        throw new InvalidOperationException($"Department '{departmentId}' has no available feeder job.");
    }

    private static bool IsCommandJob(JobPrototype job)
    {
        return CommandJobIds.Contains(job.ID);
    }

    private static TimeSpan Max(TimeSpan left, TimeSpan right)
    {
        return left >= right ? left : right;
    }
}

internal sealed class TimeTransferGrantPlan
{
    public Dictionary<string, TimeSpan> RoleTargets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, TimeSpan> DepartmentTargets { get; } = new(StringComparer.Ordinal);
    public TimeSpan OverallTarget { get; set; }
}

internal sealed class TimeTransferGrantResult
{
    public IReadOnlyDictionary<string, TimeSpan> Additions { get; }
    public TimeSpan TotalAdded { get; }

    public TimeTransferGrantResult(Dictionary<string, TimeSpan> additions)
    {
        Additions = additions;
        TotalAdded = additions.Values.Aggregate(TimeSpan.Zero, (total, value) => total + value);
    }
}
