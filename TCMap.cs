
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
 /*
  * Fork Notice:
  * - Original plugin: "TC Map Markers" by TheBandolero(Wolfleader101)
  * - Original resource: https://umod.org/plugins/tcmap (v1.0.1/MIT License)
  *
  * This fork is maintained by usagi and includes compatibility fixes and
  * configuration improvements for modern Carbon/uMod environments.
  * == 2.0.0 # 2026-03-05 ==
  * - Updated for compatibility with latest Rust and uMod versions
  * - Added configuration options for marker appearance and visibility
  * - Addded compatibility check for Raidable Bases plugin to optionally exclude its cupboards
  * - Code refactoring and cleanup for maintainability
  */

 [Info("TCMap", "usagi (original: TheBandolero)", "2.0.0", ResourceId = 0)]
 [Description("Fork of TC Map Markers. Shows tool cupboards on the map and displays authorized player names in the tooltip.")]
 public class TCMap : RustPlugin
 {
  private const string Prefab = "cupboard.tool.deployed";

  [PluginReference]
  private Plugin RaidableBases;

  private PluginConfig config;
  private readonly HashSet<int> listMarkersIDs = new HashSet<int>();
  private bool showToAllPlayers = false;

  void Init()
  {
   permission.RegisterPermission("tcmap.admin", this);
   LoadConfigValues();
  }

  void OnServerInitialized()
  {
   DoTheMagic();
  }

  #region Configuration
  private class MarkerConfig
  {
   [JsonProperty("Enabled")]
   public bool Enabled = true;

   [JsonProperty("Radius")]
   public float Radius = 0.25f;

   [JsonProperty("Alpha")]
   public float Alpha = 0.85f;

   [JsonProperty("Color1 (hex)")]
   public string Color1 = "#00FFFFFF";

   [JsonProperty("Color2 (hex)")]
   public string Color2 = "#000000FF";
  }

  private class PluginConfig
  {
   [JsonProperty("Outer Circle")]
   public MarkerConfig OuterCircle = new MarkerConfig
   {
    Enabled = true,
    Radius = 0.16f,
    Alpha = 0.65f,
    Color1 = "#FF00FFFF",
    Color2 = "#FF00FFFF"
   };

   [JsonProperty("Inner Circle")]
   public MarkerConfig InnerCircle = new MarkerConfig
   {
    Enabled = false,
    Radius = 0.12f,
    Alpha = 0.65f,
    Color1 = "#C000FFFF",
    Color2 = "#C000FFFF"
   };

   [JsonProperty("Show Name Tooltip Marker")]
   public bool ShowNameTooltipMarker = true;

   [JsonProperty("Exclude Raidable Bases Cupboards")]
   public bool ExcludeRaidableBasesCupboards = true;
  }

  protected override void LoadDefaultConfig()
  {
   config = new PluginConfig();
   SaveConfig();
  }

  protected override void SaveConfig() => Config.WriteObject(config, true);

  private void LoadConfigValues()
  {
   try
   {
    config = Config.ReadObject<PluginConfig>();
    if (config == null)
    {
     throw new JsonException();
    }
   }
   catch
   {
    PrintWarning("Failed to load TCMap config. Generating default config.");
    LoadDefaultConfig();
   }

   SaveConfig();
  }
  #endregion

  object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
  {
   ScheduleRefresh();
   return null;
  }

  object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
  {
   ScheduleRefresh();
   return null;
  }

  object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
  {
   ScheduleRefresh();
   return null;
  }

  void OnEntityKill(BaseNetworkable entity)
  {
   if (entity != null && entity.ShortPrefabName == Prefab)
   {
    ScheduleRefresh();
   }
  }

  void Unload()
  {
   KillAllMarkers();
  }

  private void ScheduleRefresh()
  {
   timer.Once(0.5f, () =>
   {
    KillAllMarkers();
    DoTheMagic();
   });
  }

  private void DoTheMagic()
  {
   foreach (BaseEntity tc in GetTCList())
   {
    if (tc != null)
    {
     List<string> authNames = GetAuthorizedNames(tc);
     if (authNames != null && authNames.Count > 0)
     {
      PutTCMarksOnMap(tc.ServerPosition, string.Join("\n", authNames));
     }
    }
   }
  }

  private List<BaseEntity> GetTCList()
  {
   List<BaseEntity> tcList = new List<BaseEntity>();

   foreach (var tcb in GameObject.FindObjectsOfType<BaseEntity>())
   {
    if (tcb != null && IsCupboardEntity(tcb))
    {
     if (config != null && config.ExcludeRaidableBasesCupboards && IsRaidableBasesCupboard(tcb))
     {
      continue;
     }

     tcList.Add(tcb);
    }
   }
   return tcList;
  }

  private bool IsCupboardEntity(BaseEntity entity) => entity != null && entity.ShortPrefabName == Prefab;

  private bool IsRaidableBasesCupboard(BaseEntity entity)
  {
   if (entity == null || RaidableBases == null)
   {
    return false;
   }

   object result = RaidableBases.Call("EventTerritory", entity.transform.position, 0f);
   return result is bool isInsideRaidable && isInsideRaidable;
  }

  private List<string> GetAuthorizedNames(BaseEntity tc)
  {
   if (tc == null)
   {
    return null;
   }

   BuildingPrivlidge tcPrivilege = tc.gameObject.GetComponentInParent<BuildingPrivlidge>();
   if (tcPrivilege == null)
   {
    return null;
   }

   if (tcPrivilege.authorizedPlayers == null || tcPrivilege.authorizedPlayers.Count == 0)
   {
    return new List<string>();
   }

   var authNames = new List<string>();
   foreach (ulong userId in tcPrivilege.authorizedPlayers)
   {
    authNames.Add(GetPlayerName(userId));
   }

   return authNames;
  }

  private string GetPlayerName(ulong userId)
  {
   BasePlayer online = BasePlayer.FindByID(userId);
   if (online != null && !string.IsNullOrEmpty(online.displayName))
   {
    return online.displayName;
   }

   BasePlayer sleeping = BasePlayer.FindSleeping(userId);
   if (sleeping != null && !string.IsNullOrEmpty(sleeping.displayName))
   {
    return sleeping.displayName;
   }

   return userId.ToString();
  }

  // MAP MARKERS
  private void PutTCMarksOnMap(Vector3 tcPos, string markerAuthNames)
  {
   if (config?.OuterCircle != null && config.OuterCircle.Enabled)
   {
    MapMarkerGenericRadius outer = CreateRadiusMarker(tcPos, config.OuterCircle);
    if (outer != null)
    {
     listMarkersIDs.Add(outer.GetInstanceID());
    }
   }

   if (config?.InnerCircle != null && config.InnerCircle.Enabled)
   {
    MapMarkerGenericRadius inner = CreateRadiusMarker(tcPos, config.InnerCircle);
    if (inner != null)
    {
     listMarkersIDs.Add(inner.GetInstanceID());
    }
   }

   if (config == null || config.ShowNameTooltipMarker)
   {
    VendingMachineMapMarker nameMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", tcPos) as VendingMachineMapMarker;
    if (nameMarker != null)
    {
     nameMarker.markerShopName = markerAuthNames;
     nameMarker.Spawn();
     listMarkersIDs.Add(nameMarker.GetInstanceID());
    }
   }
  }

  private MapMarkerGenericRadius CreateRadiusMarker(Vector3 position, MarkerConfig markerConfig)
  {
   MapMarkerGenericRadius marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;
   if (marker == null)
   {
    return null;
   }

   marker.alpha = Mathf.Clamp01(markerConfig.Alpha);
   marker.color1 = ParseColor(markerConfig.Color1, Color.cyan);
   marker.color2 = ParseColor(markerConfig.Color2, Color.black);
   marker.radius = Mathf.Max(0.01f, markerConfig.Radius);

   marker.Spawn();
   marker.SendUpdate();

   return marker;
  }

  private Color ParseColor(string colorString, Color fallback)
  {
   if (!string.IsNullOrEmpty(colorString) && ColorUtility.TryParseHtmlString(colorString, out Color parsedColor))
   {
    return parsedColor;
   }

   return fallback;
  }

  object CanNetworkTo(BaseNetworkable entity, BasePlayer player)
  {
   if (entity == null || player == null)
   {
    return null;
   }

   if (!showToAllPlayers)
   {
    if ((entity.ShortPrefabName == "genericradiusmarker" || entity.ShortPrefabName == "vending_mapmarker") && !permission.UserHasPermission(player.userID.ToString(), "tcmap.admin"))
    {
     return false;
    }
   }

   return null;
  }

  private void KillAllMarkers()
  {
   foreach (var mark in GameObject.FindObjectsOfType<MapMarkerGenericRadius>())
   {
    if (listMarkersIDs.Contains(mark.GetInstanceID()))
    {
     mark.Kill();
     mark.SendUpdate();
    }
   }
   foreach (var mark in GameObject.FindObjectsOfType<VendingMachineMapMarker>())
   {
    if (listMarkersIDs.Contains(mark.GetInstanceID()))
    {
     mark.Kill();
    }
   }

   listMarkersIDs.Clear();
  }

  // CHAT COMMANDS
  [ChatCommand("tcmap"), Permission("tcmap.admin")]
  private void TCMapChatCommands(BasePlayer player, string command, string[] args)
  {
   if (permission.UserHasPermission(player.userID.ToString(), "tcmap.admin"))
   {
    if (args == null || args.Length == 0)
    {

    }
    else if (args[0].Equals("clear"))
    {
     KillAllMarkers();
     player.ChatMessage("<color=#ffa500ff>TCMap markers <b>REMOVED!</b></color>");
    }
    else if (args[0].Equals("update"))
    {
     KillAllMarkers();
     DoTheMagic();
     player.ChatMessage("<color=#ffa500ff>TCMap markers <b>UPDATED!</b></color>");
    }
    else if (args[0].Equals("showtoall"))
    {
     if (showToAllPlayers)
     {
      showToAllPlayers = false;
      KillAllMarkers();
      DoTheMagic();
      player.ChatMessage("<color=#ffa500ff>TCMap show everyone: </color><b>" + showToAllPlayers + "</b>");
     }
     else
     {
      showToAllPlayers = true;
      KillAllMarkers();
      DoTheMagic();
      player.ChatMessage("<color=#ffa500ff>TCMap show everyone: </color><b>" + showToAllPlayers + "</b>");
     }
    }
    else if (args[0].Equals("help"))
    {
     player.ChatMessage("<color=#ffa500ff><b><size=22>TCMapHelp</size></b></color>"
         + "\n\n<color=#add8e6ff><b>/tcmap clear</b></color>  Removes all TC marks from map."
         + "\n<color=#add8e6ff><b>/tcmap update</b></color>  Updates all TC marks on the map."
         + "\n<color=#add8e6ff><b>/tcmap showtoall</b></color>  Shows all TC marks to ALL players."
         + "\n<color=#add8e6ff><b>/tcmap help</b></color>  Shows this help menu."
         + "\n\n<color=#ffa500ff><b><size=20>•</size></b></color> <color=#add8e6ff><b>Show to all is set to: </b></color> " + showToAllPlayers);
    }
   }
  }

 }
}
