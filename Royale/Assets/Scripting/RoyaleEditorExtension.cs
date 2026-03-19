// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using CodeGamified.Editor;

namespace Royale.Scripting
{
    /// <summary>
    /// Editor extension for Royale — provides function metadata for tap-to-code editor.
    /// </summary>
    public class RoyaleEditorExtension : IEditorExtension
    {
        public List<EditorTypeInfo> GetAvailableTypes()
        {
            return new List<EditorTypeInfo>();
        }

        public List<EditorFuncInfo> GetAvailableFunctions()
        {
            return new List<EditorFuncInfo>
            {
                // Player state
                new EditorFuncInfo { Name = "get_x",             Hint = "player X position",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_y",             Hint = "player Y position",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_health",        Hint = "current HP (0-100)",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_armor",         Hint = "current armor (0-50)",       ArgCount = 0 },
                new EditorFuncInfo { Name = "get_alive",         Hint = "alive player count",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_kills",         Hint = "kill count",                 ArgCount = 0 },

                // Zone
                new EditorFuncInfo { Name = "get_zone_x",       Hint = "zone center X",              ArgCount = 0 },
                new EditorFuncInfo { Name = "get_zone_y",       Hint = "zone center Y",              ArgCount = 0 },
                new EditorFuncInfo { Name = "get_zone_radius",  Hint = "safe zone radius",           ArgCount = 0 },
                new EditorFuncInfo { Name = "get_zone_shrinking", Hint = "is zone shrinking? (0/1)", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_in_zone",      Hint = "inside safe zone? (0/1)",    ArgCount = 0 },

                // Weapon
                new EditorFuncInfo { Name = "get_weapon",       Hint = "weapon type (0-5)",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_ammo",         Hint = "magazine ammo",              ArgCount = 0 },
                new EditorFuncInfo { Name = "get_ammo_reserve", Hint = "reserve ammo",               ArgCount = 0 },
                new EditorFuncInfo { Name = "get_reloading",    Hint = "is reloading? (0/1)",        ArgCount = 0 },
                new EditorFuncInfo { Name = "get_weapon_range", Hint = "weapon effective range",     ArgCount = 0 },

                // Enemy
                new EditorFuncInfo { Name = "get_enemy_dist",   Hint = "nearest enemy distance (-1=none)", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_enemy_angle",  Hint = "angle to nearest enemy",     ArgCount = 0 },
                new EditorFuncInfo { Name = "get_enemy_health", Hint = "nearest enemy HP",           ArgCount = 0 },

                // Crate
                new EditorFuncInfo { Name = "get_crate_dist",   Hint = "nearest crate distance (-1=none)", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_crate_angle",  Hint = "angle to nearest crate",     ArgCount = 0 },

                // Cover
                new EditorFuncInfo { Name = "get_cover_dist",   Hint = "nearest cover distance (-1=none)", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_cover_angle",  Hint = "angle to nearest cover",     ArgCount = 0 },

                // Inventory
                new EditorFuncInfo { Name = "get_slot_weapon",  Hint = "weapon in slot (0 or 1)",    ArgCount = 1 },
                new EditorFuncInfo { Name = "get_has_heal",     Hint = "has healing item? (0/1)",    ArgCount = 0 },
                new EditorFuncInfo { Name = "get_facing",       Hint = "current aim angle",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_scope",        Hint = "scope level (0-4)",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_view_range",   Hint = "view/detection radius",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_input",        Hint = "manual input value",         ArgCount = 0 },

                // Commands
                new EditorFuncInfo { Name = "move_toward",      Hint = "move in direction (angle)",  ArgCount = 1 },
                new EditorFuncInfo { Name = "set_facing",       Hint = "set aim angle",              ArgCount = 1 },
                new EditorFuncInfo { Name = "shoot",            Hint = "fire weapon",                ArgCount = 0 },
                new EditorFuncInfo { Name = "reload",           Hint = "start reload",               ArgCount = 0 },
                new EditorFuncInfo { Name = "loot",             Hint = "pick up nearby item",        ArgCount = 0 },
                new EditorFuncInfo { Name = "swap_weapon",      Hint = "switch weapon slot",         ArgCount = 0 },
                new EditorFuncInfo { Name = "use_heal",         Hint = "use heal item (3s channel)", ArgCount = 0 },
                new EditorFuncInfo { Name = "move_to_zone",     Hint = "move toward zone center",    ArgCount = 0 },

                // War Master fate commands
                new EditorFuncInfo { Name = "get_match_time",   Hint = "elapsed match time (seconds)", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_phase",        Hint = "current zone phase (0-4)",   ArgCount = 0 },
                new EditorFuncInfo { Name = "spawn_airdrop",    Hint = "drop rare crate at (x,y)",   ArgCount = 2 },
                new EditorFuncInfo { Name = "call_airstrike",   Hint = "AOE denial at (x,y)",        ArgCount = 2 },
                new EditorFuncInfo { Name = "shift_zone",       Hint = "bias zone center toward (x,y)", ArgCount = 2 },
            };
        }

        public List<EditorMethodInfo> GetMethodsForType(string typeName)
        {
            return new List<EditorMethodInfo>();
        }

        public List<string> GetVariableNameSuggestions()
        {
            return new List<string>
            {
                "hp", "dist", "angle", "cdist", "alive",
                "weapon", "ammo", "scope", "zone_r", "in_zone"
            };
        }

        public List<string> GetStringLiteralSuggestions()
        {
            return new List<string>();
        }
    }
}
