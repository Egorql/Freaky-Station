using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._TT.StationHandleJob;

public sealed class TTStationHandleJobSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [typeof(ArrivalsSystem), typeof(ContainerSpawnPointSystem), typeof(SpawnPointSystem)]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult is not null)
        {
            Log.Error("The spawn result has already been received");
            return;
        }

        if (ev.Job is not { } job)
        {
            Log.Debug("The job does not exist");
            return;
        }

        var requestedStation = ev.Station;
        var handledStation = GetStation(job);
        var requestedIsHandledStation = requestedStation is { } requestedStationUid &&
            HasComp<TTStationHandleJobComponent>(requestedStationUid);

        if (handledStation is null)
        {
            if (requestedIsHandledStation)
            {
                AbortSpawn(ev,
                    $"Blocked spawn for job {job} on station {GetStationName(requestedStation)}: this station only accepts TTStationHandleJob roles.",
                    Loc.GetString("game-ticker-player-job-spawn-invalid-station"));
            }

            return;
        }

        if (requestedStation is not { } requestedStationUid2 || requestedStationUid2 != handledStation.Value)
        {
            AbortSpawn(ev,
                $"Blocked spawn for job {job}: requested station {GetStationName(requestedStation)}, expected {GetStationName(handledStation)}.",
                Loc.GetString("game-ticker-player-job-spawn-invalid-station"));
            return;
        }

        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (_station.GetOwningStation(uid, xform) != handledStation)
                continue;

            if (spawnPoint.Job != job)
                continue;

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
                possiblePositions.Add(xform.Coordinates);

            if (_gameTicker.RunLevel != GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.Job)
                possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0)
        {
            AbortSpawn(ev,
                $"No spawn points found for role {job} on station {GetStationName(handledStation)}.",
                Loc.GetString("game-ticker-player-job-spawn-no-spawn-point"));
            return;
        }

        var spawnLoc = _random.Pick(possiblePositions);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            job,
            ev.HumanoidCharacterProfile,
            handledStation);
    }

    private EntityUid? GetStation(ProtoId<JobPrototype> job)
    {
        var query = EntityQueryEnumerator<TTStationHandleJobComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Jobs.Contains(job))
                continue;

            return uid;
        }

        return null;
    }

    private void AbortSpawn(PlayerSpawningEvent ev, string reason, string failureMessage)
    {
        ev.PreventFallback = true;
        ev.FailureMessage = failureMessage;
        Log.Warning(reason);
    }

    private string GetStationName(EntityUid? station)
    {
        return station is { } stationUid
            ? Name(stationUid)
            : "<null>";
    }
}
