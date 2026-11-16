using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using PackageBank.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PackageBank.Adapters;

/// <summary>
/// Puts the spots of <see cref="FixtureSpots"/> into the game. The rows go into the slot table as soon as it exists, and
/// again before every save loads, so a piece saved on one of them keeps its spot. Once a second at home, each spot that
/// should show gets its points on the map (the fixture's point, and a stand point copied from the nearest game spot of
/// its type) and a clickable box copied from that spot's box; a spot that should no longer show loses both. While
/// Uninstall prep is on, every piece on one of these spots is packed into a yard package.
/// </summary>
internal sealed class FixtureSpotsLive
{
    private const float CheckSeconds = 1f;
    private const string SpotRootName = "BuildingPos";

    private readonly Func<long, string?> _packUp;
    private readonly Action<string> _log;
    private readonly HashSet<string> _reported = new();
    private readonly HashSet<long> _refused = new();
    private readonly Dictionary<string, GameObject> _boxes = new();

    private IntPtr _guidance;
    private IReadOnlyList<FixtureSpot> _spots = Array.Empty<FixtureSpot>();
    private IntPtr _table;
    private int _tableVersion = -1;
    private IntPtr _map;
    private Transform? _root;
    private float _nextCheck;
    private volatile bool _isOn = true;
    private volatile bool _isUninstalling;
    private volatile int _settingsVersion;
    private int _usedVersion;

    public FixtureSpotsLive(Func<long, string?> packUp, Action<string> log)
    {
        _packUp = packUp;
        _log = log;
    }

    /// <summary>Safe from any thread; the next check applies it, and tries again the pieces Uninstall prep could not pack.</summary>
    public void Use(bool isOn, bool isUninstalling)
    {
        _isOn = isOn;
        _isUninstalling = isUninstalling;
        _settingsVersion++;
        _nextCheck = 0;
    }

    public void Tick()
    {
        var now = Time.unscaledTime;

        if (now < _nextCheck) return;

        _nextCheck = now + CheckSeconds;

        if (_usedVersion != _settingsVersion)
        {
            _usedVersion = _settingsVersion;
            _refused.Clear();
        }

        try
        {
            if (GameSingleton.Existing<ConfigManager>() is not { } config) return;

            EnsureRows(config);
            ApplyHome(config);
        }
        catch (Exception ex)
        {
            ReportOnce("keeping the spots where house furniture stood", ex);
        }
    }

    /// <summary>Called from the load hook, before the game builds the saved furniture.</summary>
    public void BeforeLoad()
    {
        try
        {
            if (GameSingleton.Existing<ConfigManager>() is { } config) EnsureRows(config);
        }
        catch (Exception ex)
        {
            ReportOnce("adding the spot rows before a load", ex);
        }
    }

    // ---- rows ----

    private void EnsureRows(ConfigManager config)
    {
        var guidance = config._Config_GuidanceFurniture_Dict;
        var table = config._Config_MapSlot_Dict;

        if (guidance == null || table == null) return;

        if (guidance.Pointer != _guidance)
        {
            _guidance = guidance.Pointer;
            _spots = FixtureSpots.Plan(HomeFixtures.Rows(config, guidance));
            _tableVersion = -1;
        }

        if (table.Pointer == _table && table._version == _tableVersion) return;

        var added = 0;

        foreach (var spot in _spots)
        {
            if (table.ContainsKey(spot.Id)) continue;

            table.Add(spot.Id, NewRow(spot));
            added++;
        }

        if (added > 0) config.InitMapSlotNameMap();

        _table = table.Pointer;
        _tableVersion = table._version;

        if (added > 0 && _reported.Add("rows")) _log($"added {added} build spots for house furniture on fixed points, shown only once a piece is taken down");
    }

    /// <summary>Room 0 lets any furniture of the right size go there, as on the game's spots with no room.</summary>
    private static Config_MapSlot NewRow(FixtureSpot spot) => new()
    {
        ID = spot.Id,
        MapID = spot.Floor,
        Name = spot.Name,
        IsOpen = true,
        DemoTwoIsOpen = true,
        Type = spot.Type,
        Room = 0,
        IsExtendable = false,
        UnlockTag = "",
        TowerRow = 0,
    };

    // ---- the loaded home ----

    private void ApplyHome(ConfigManager config)
    {
        var map = config._customCache_k__BackingField?._MapConfig_k__BackingField;
        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (map == null || agents == null || role == null || !HomeFixtures.IsAtHome(config, role)) return;
        if (AgentTools.GetAgentComponent<BuildComponent>(role) is not { } build) return;

        if (map.Pointer != _map || _root == null)
        {
            // A new map is the game's, untouched, and a new scene has a box root of its own.
            _map = map.Pointer;
            _boxes.Clear();
            _root = null;
        }

        var taken = Taken(agents);

        if (_isUninstalling) PackUp(taken);

        var shown = FixtureSpots.Shown(_spots, Demolished(build), PointsOnMap(map), new HashSet<string>(taken.Keys), _isOn, _isUninstalling);

        foreach (var spot in _spots)
        {
            var hasBox = _boxes.TryGetValue(spot.Name, out var box) && box != null;
            var hasPoint = map._BuildingNodeMap_k__BackingField?.ContainsKey(spot.Name) == true;

            if (shown.Contains(spot.Name))
            {
                if (!hasBox) Show(config, map, spot);
            }
            else if (hasBox || hasPoint)
            {
                Hide(map, spot);
            }
        }
    }

    /// <summary>The pieces standing on these spots, by spot name, as the game keeps the spot for each piece.</summary>
    private static Dictionary<string, long> Taken(AgentManager agents)
    {
        var taken = new Dictionary<string, long>();
        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead) continue;

            var name = AgentTools.GetAgentComponent<SlotComponent>(furniture)?._SlotName_k__BackingField;

            if (name is { } spot && FixtureSpots.IsOurs(spot)) taken[spot] = furniture.InstanceId;
        }

        return taken;
    }

    /// <summary>A piece the game will not let go is logged once and tried again only after a settings change.</summary>
    private void PackUp(Dictionary<string, long> taken)
    {
        foreach (var (name, id) in taken)
        {
            if (_refused.Contains(id)) continue;

            if (_packUp(id) is { } reason)
            {
                _refused.Add(id);
                _log($"Uninstall prep: piece {id} on {name} was not packed up ({reason}). Pack it up or remove it yourself before you remove PackageBank.dll.");

                continue;
            }

            _log($"Uninstall prep: packed up piece {id} from {name} into a yard package");
        }
    }

    private static IReadOnlySet<int> Demolished(BuildComponent build)
    {
        var demolished = new HashSet<int>();
        var record = build.GetDemolishedGuidanceIds();

        if (record == null) return demolished;

        foreach (var id in record) demolished.Add(id);

        return demolished;
    }

    private IReadOnlySet<string> PointsOnMap(Config_Map map)
    {
        var points = new HashSet<string>();
        var nodes = map._NodeMap_k__BackingField;

        if (nodes == null) return points;

        foreach (var spot in _spots)
        {
            if (nodes.ContainsKey(spot.Point)) points.Add(spot.Point);
        }

        return points;
    }

    // ---- one spot ----

    private void Show(ConfigManager config, Config_Map map, FixtureSpot spot)
    {
        var positions = map._BuildingNodeMap_k__BackingField;
        var rotations = map._BuildingNodeRotationMap_k__BackingField;
        var actions = map._BuildingNodeActionPosMap_k__BackingField;
        var actionTurns = map._BuildingNodeActionRotationMap_k__BackingField;
        var nodes = map._NodeMap_k__BackingField;

        if (positions == null || rotations == null || actions == null || actionTurns == null || nodes == null) return;
        if (!nodes.TryGetValue(spot.Point, out var position)) return;
        if (map._NodeRotationMap_k__BackingField is not { } turns || !turns.TryGetValue(spot.Point, out var rotation)) rotation = Quaternion.identity;
        if (FindRoot() is not { } root) return;

        if (Template(config, map, root, spot, position) is not { } template)
        {
            if (_reported.Add("template " + spot.Name)) _log($"build spot {spot.Name} has no game spot nearby to copy its box from, so it stays shut");

            return;
        }

        var from = (positions[template.Name], rotations.TryGetValue(template.Name, out var turn) ? turn : Quaternion.identity);
        var to = (position, rotation);

        positions[spot.Name] = position;
        rotations[spot.Name] = rotation;

        if (actions.TryGetValue(template.Name, out var stand))
        {
            var standTurn = actionTurns.TryGetValue(template.Name, out var t) ? t : Quaternion.identity;
            var moved = Follow(from, to, (stand, standTurn));

            actions[spot.Name] = moved.Position;
            actionTurns[spot.Name] = moved.Rotation;
        }

        var box = Object.Instantiate(template.Box.gameObject, root);
        var boxPose = Follow(from, to, (template.Box.position, template.Box.rotation));

        box.name = spot.Name;
        box.transform.SetPositionAndRotation(boxPose.Position, boxPose.Rotation);
        _boxes[spot.Name] = box;
        _log($"build spot {spot.Name} opened where furniture {spot.FurnitureId} stood (box copied from {template.Name})");
    }

    private void Hide(Config_Map map, FixtureSpot spot)
    {
        map._BuildingNodeMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeRotationMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeActionPosMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeActionRotationMap_k__BackingField?.Remove(spot.Name);

        if (_boxes.Remove(spot.Name, out var box) && box != null) Object.Destroy(box);

        _log($"build spot {spot.Name} closed");
    }

    /// <summary>
    /// The game spot whose box and stand point the new spot copies: the nearest one of the same type that has a box and
    /// points, else the nearest of any type.
    /// </summary>
    private static (string Name, Transform Box)? Template(ConfigManager config, Config_Map map, Transform root, FixtureSpot spot, Vector3 position)
    {
        var slots = config._mapSlotByName;
        var positions = map._BuildingNodeMap_k__BackingField;
        (string, Transform)? best = null;
        var bestScore = float.MaxValue;

        if (slots == null || positions == null) return null;

        for (var i = 0; i < root.childCount; i++)
        {
            var box = root.GetChild(i);
            var name = box.name;

            if (FixtureSpots.IsOurs(name) || !slots.TryGetValue(name, out var slot) || slot == null || !positions.TryGetValue(name, out var point)) continue;

            var score = Vector3.Distance(point, position) + (slot.Type == spot.Type ? 0 : 10_000);

            if (score < bestScore) (best, bestScore) = ((name, box), score);
        }

        return best;
    }

    /// <summary>Carries a pose that sat relative to one spot to the same place relative to another.</summary>
    private static (Vector3 Position, Quaternion Rotation) Follow((Vector3 Position, Quaternion Rotation) from, (Vector3 Position, Quaternion Rotation) to, (Vector3 Position, Quaternion Rotation) pose)
    {
        var back = Quaternion.Inverse(from.Rotation);
        var offset = back * (pose.Position - from.Position);

        return (to.Position + to.Rotation * offset, to.Rotation * back * pose.Rotation);
    }

    /// <summary>The scene's box root. A copy a former visit left behind is found again by name, so it goes.</summary>
    private Transform? FindRoot()
    {
        if (_root != null) return _root;

        _root = SpotRoot();

        if (_root == null) return null;

        for (var i = _root.childCount - 1; i >= 0; i--)
        {
            var child = _root.GetChild(i);

            if (FixtureSpots.IsOurs(child.name) && !_boxes.ContainsKey(child.name)) Object.Destroy(child.gameObject);
        }

        return _root;
    }

    private static Transform? SpotRoot()
    {
        for (var s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);

            if (!scene.isLoaded) continue;

            foreach (var top in scene.GetRootGameObjects())
            {
                var transform = top.transform;

                if (transform.Find(SpotRootName) is { } direct) return direct;

                for (var c = 0; c < transform.childCount; c++)
                {
                    if (transform.GetChild(c).Find(SpotRootName) is { } nested) return nested;
                }
            }
        }

        return null;
    }

    private void ReportOnce(string what, Exception ex)
    {
        if (_reported.Add(what + ex.GetType().FullName)) _log($"{what} failed (reported once): {ex}");
    }
}
