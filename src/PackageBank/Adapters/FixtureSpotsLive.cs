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
/// again before every save loads, so a piece saved on one of them keeps its spot. A pass runs only after something that
/// can change the spots: a load or a trip (both run the game's load), a piece packed up or removed, or a settings change.
/// At home, each spot that should show gets its points on the map (the fixture's point, and a stand point copied from the
/// nearest game spot of its type) and a clickable box copied from that spot's box; a spot that should no longer show loses
/// both. While Uninstall prep is on, every piece on one of these spots is packed into a yard package. A pass that comes
/// too early in a load tries again once a second until it is done (<see cref="FixtureSpots.IsPassDone"/>); otherwise
/// nothing runs.
/// </summary>
internal sealed class FixtureSpotsLive
{
    private const float RetrySeconds = 1f;
    private const string SpotRootName = "BuildingPos";

    private readonly Func<long, string?> _packUp;
    private readonly Action<string> _log;
    private readonly HashSet<string> _reported = new();
    private readonly HashSet<long> _refused = new();
    private readonly Dictionary<string, GameObject> _boxes = new();
    private readonly Dictionary<string, long> _taken = new();
    private readonly HashSet<string> _takenNames = new();
    private readonly HashSet<int> _demolished = new();
    private readonly HashSet<string> _pointsOnMap = new();
    private readonly HashSet<string> _shown = new();

    private IntPtr _guidance;
    private IReadOnlyList<FixtureSpot> _spots = Array.Empty<FixtureSpot>();
    private IntPtr _table;
    private int _tableVersion = -1;
    private IntPtr _map;
    private Transform? _root;
    private float _nextTry;
    private volatile bool _isChanged = true;
    private volatile bool _isOn;
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
        Changed();
    }

    /// <summary>Something that can open or close a spot happened; the next frame runs a pass. Safe from any thread.</summary>
    public void Changed()
    {
        _nextTry = 0;
        _isChanged = true;
    }

    public void Tick()
    {
        if (!_isChanged) return;

        var now = Time.unscaledTime;

        if (now < _nextTry) return;

        // Cleared before the pass, so a change made during it (a piece Uninstall prep packs up) runs one more.
        _isChanged = false;
        _nextTry = now + RetrySeconds;

        if (_usedVersion != _settingsVersion)
        {
            _usedVersion = _settingsVersion;
            _refused.Clear();
        }

        try
        {
            if (GameSingleton.Existing<ConfigManager>() is not { } config)
            {
                _isChanged = true;

                return;
            }

            EnsureRows(config);

            if (!ApplyHome(config)) _isChanged = true;
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

            Changed();
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

    /// <summary>Whether the pass is done; false when it came too early in a load and has to try again.</summary>
    private bool ApplyHome(ConfigManager config)
    {
        var map = config._customCache_k__BackingField?._MapConfig_k__BackingField;
        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var role = agents?.GetLeadingRole();
        var build = role == null ? null : AgentTools.GetAgentComponent<BuildComponent>(role);

        if (map == null || agents == null || role == null || build == null) return FixtureSpots.IsPassDone(hasWorld: false, isAtHome: false, hasBoxRoot: false);
        if (!HomeFixtures.IsAtHome(config, role)) return FixtureSpots.IsPassDone(hasWorld: true, isAtHome: false, hasBoxRoot: false);

        if (map.Pointer != _map || _root == null)
        {
            // A new map is the game's, untouched, and a new scene has a box root of its own.
            _map = map.Pointer;
            _boxes.Clear();
            _root = null;
        }

        ReadTaken(agents);

        if (_isUninstalling) PackUp();

        ReadDemolished(build);
        ReadPointsOnMap(map);
        FixtureSpots.Shown(_spots, _demolished, _pointsOnMap, _takenNames, _isOn, _isUninstalling, _shown);
        OpenAndClose(config, map);

        return FixtureSpots.IsPassDone(hasWorld: true, isAtHome: true, FindRoot() != null);
    }

    private void OpenAndClose(ConfigManager config, Config_Map map)
    {
        var positions = map._BuildingNodeMap_k__BackingField;

        foreach (var spot in _spots)
        {
            var hasBox = _boxes.TryGetValue(spot.Name, out var box) && box != null;
            var hasPoint = positions?.ContainsKey(spot.Name) == true;

            if (_shown.Contains(spot.Name))
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
    private void ReadTaken(AgentManager agents)
    {
        var furnitures = agents.GetAllFurnitures();

        _taken.Clear();
        _takenNames.Clear();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead) continue;

            var name = AgentTools.GetAgentComponent<SlotComponent>(furniture)?._SlotName_k__BackingField;

            if (name is not { } spot || !FixtureSpots.IsOurs(spot)) continue;

            _taken[spot] = furniture.InstanceId;
            _takenNames.Add(spot);
        }
    }

    /// <summary>A piece the game will not let go is logged once and tried again only after a settings change.</summary>
    private void PackUp()
    {
        foreach (var (name, id) in _taken)
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

    private void ReadDemolished(BuildComponent build)
    {
        var record = build.GetDemolishedGuidanceIds();

        _demolished.Clear();

        if (record == null) return;

        foreach (var id in record) _demolished.Add(id);
    }

    private void ReadPointsOnMap(Config_Map map)
    {
        var nodes = map._NodeMap_k__BackingField;

        _pointsOnMap.Clear();

        if (nodes == null) return;

        foreach (var spot in _spots)
        {
            if (nodes.ContainsKey(spot.Point)) _pointsOnMap.Add(spot.Point);
        }
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
            if (_reported.Add("template " + spot.Name)) _log($"build spot {spot.Name} has no game spot of its size to copy its box from, so it stays shut");

            return;
        }

        var from = PoseOf(positions[template.Name], rotations.TryGetValue(template.Name, out var turn) ? turn : Quaternion.identity);
        var to = PoseOf(position, rotation);

        positions[spot.Name] = position;
        rotations[spot.Name] = rotation;

        if (actions.TryGetValue(template.Name, out var stand))
        {
            var standTurn = actionTurns.TryGetValue(template.Name, out var t) ? t : Quaternion.identity;
            var moved = SpotMath.Follow(from, to, PoseOf(stand, standTurn));

            actions[spot.Name] = ToUnity(moved.Position);
            actionTurns[spot.Name] = ToUnity(moved.Rotation);
        }

        var box = Object.Instantiate(template.Box.gameObject, root);
        var boxPose = SpotMath.Follow(from, to, PoseOf(template.Box.position, template.Box.rotation));

        box.name = spot.Name;
        box.transform.SetPositionAndRotation(ToUnity(boxPose.Position), ToUnity(boxPose.Rotation));
        _boxes[spot.Name] = box;
        LayoutScreen.Join(root, box);
        _log($"build spot {spot.Name} opened where furniture {spot.FurnitureId} stood (box copied from {template.Name})");
    }

    private void Hide(Config_Map map, FixtureSpot spot)
    {
        map._BuildingNodeMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeRotationMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeActionPosMap_k__BackingField?.Remove(spot.Name);
        map._BuildingNodeActionRotationMap_k__BackingField?.Remove(spot.Name);
        LayoutScreen.Leave(spot.Name);

        if (_boxes.Remove(spot.Name, out var box) && box != null) Object.Destroy(box);

        _log($"build spot {spot.Name} closed");
    }

    /// <summary>
    /// The game spot whose box and stand point the new spot copies: the best by <see cref="FixtureSpots.TemplateScore"/>
    /// among those with a box and points. None when no game spot of the same type has a box, since a box of another size
    /// would take clicks meant for the spots around it.
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
            if (FixtureSpots.TemplateScore(spot, slot.Type, slot.MapID, Vector3.Distance(point, position)) is not { } score || score >= bestScore) continue;

            (best, bestScore) = ((name, box), score);
        }

        return best;
    }

    private static SpotPose PoseOf(Vector3 position, Quaternion rotation) =>
        new(new System.Numerics.Vector3(position.x, position.y, position.z), new System.Numerics.Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));

    private static Vector3 ToUnity(System.Numerics.Vector3 position) => new(position.X, position.Y, position.Z);

    private static Quaternion ToUnity(System.Numerics.Quaternion rotation) => new(rotation.X, rotation.Y, rotation.Z, rotation.W);

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
