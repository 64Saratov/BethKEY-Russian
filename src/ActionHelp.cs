using System;
using System.Collections.Generic;
using System.Text;

namespace BethKEY
{
    /// Plain-English descriptions shown when hovering over an action.
    /// Lookup order: game + section + action, section + action, game + action (main), action in any section, whole section.
    public static class ActionHelp
    {
        static readonly HashSet<string> MainCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Movement", "Combat", "Actions", "Menus", "Hotkeys", "System", "Main Gameplay", "Other" };

        static readonly Dictionary<string, string> D = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static string N(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char c in s ?? "") if (char.IsLetterOrDigit(c)) b.Append(char.ToLowerInvariant(c));
            return b.ToString();
        }

        static void A(string key, string text)
        {
            // key: "[game:]section|action"   section "main" = normal gameplay, "*" = any section, action "*" = whole section
            string game = "", rest = key;
            int c = key.IndexOf(':');
            if (c > 0) { game = key.Substring(0, c) + ":"; rest = key.Substring(c + 1); }
            string[] p = rest.Split('|');
            foreach (string ev in p[1].Split(','))
                D[game + (p[0] == "*" || p[0] == "main" ? p[0] : N(p[0])) + "|" + (ev == "*" ? "*" : N(ev))] = text;
        }

        public static string Get(GameDef g, BindRow r)
        {
            string game = g == null ? "" : g.ProcessName + ":";
            string ctx = MainCats.Contains(r.Context ?? "") ? "main" : N(r.Context);
            string ev = N(r.Event);
            string t;
            foreach (string k in new[] { game + ctx + "|" + ev, ctx + "|" + ev, game + "*|" + ev, "*|" + ev, game + ctx + "|*", ctx + "|*" })
                if (D.TryGetValue(k, out t)) return t;
            if (r.Label != null && r.Label.EndsWith("(menu key)"))
                return "Menu command the game puts on a controller button. On PC, use the key shown next to the on-screen prompt.";
            return null;
        }

        static ActionHelp()
        {
            const string Dev = "Leftover developer key. Normally does nothing in the released game.";

            // ---- Movement ----
            A("main|Forward", "Walk or run forward.");
            A("main|Back", "Walk or run backward.");
            A("main|StrafeLeft,Slide Left", "Step sideways to the left without turning.");
            A("main|StrafeRight,Slide Right", "Step sideways to the right without turning.");
            A("main|Run", "Hold to switch between running and walking (the opposite of what Always Run is set to).");
            A("main|ToggleAlwaysRun,Always Run", "Switch between always running and always walking.");
            A("main|AutoMove,Auto Move", "Keep moving forward without holding a key. Press again, or press Forward/Back, to stop.");
            A("main|Jump", "Jump.");
            A("main|Sprint", "Hold to sprint.");
            A("Fallout4:main|Sprint", "Hold to sprint. Uses Action Points.");
            A("SkyrimSE:main|Sprint", "Hold to sprint. Uses stamina (on horseback: gallop).");
            A("main|Sneak,Crouch/Sneak", "Toggle sneaking (crouch).");

            // ---- Combat ----
            A("main|PrimaryAttack,Use", "Fire or swing the equipped weapon. Hold to keep firing automatic weapons.");
            A("main|SecondaryAttack,Block", "Hold to aim down the sights or scope, or to block with a melee weapon.");
            A("main|Melee", "Bash with your weapon. Hold to throw the equipped grenade or mine.");
            A("main|ReadyWeapon,Ready Item", "Tap to reload. Hold to holster or draw your weapon.");
            A("SkyrimSE:main|Ready Weapon", "Draw or sheathe your weapons and spells.");
            A("main|Ammo Swap", "Switch the equipped weapon to the next ammo type you carry.");
            A("main|VATS", "Enter V.A.T.S. targeting.");
            A("main|Left Attack/Block", "Attack or cast with your left hand. With a two-handed weapon, or a weapon in the right hand only, hold to block.");
            A("main|Right Attack/Block", "Attack or cast with your right hand. Hold for a power attack.");
            A("main|Shout", "Use your equipped shout or power. Hold for a stronger shout.");

            // ---- Actions ----
            A("main|Activate", "Interact with what you're looking at: talk, open, pick up, use, sit.");
            A("main|Grab", "Hold to grab and carry a physics object or body; release to drop it.");
            A("main|TogglePOV,Toggle POV", "Switch between first- and third-person view.");
            A("main|ZoomIn", "Move the camera closer to your character. All the way in switches to first person.");
            A("main|ZoomOut", "Move the camera away from your character. From first person, switches to third person.");
            A("main|Wait", "Open the Wait menu to pass time.");
            A("main|Rest", "Open the Wait menu to pass time (\"Rest\" in the INI).");

            // ---- Menus ----
            A("main|Pipboy,Menu Mode", "Open or close the Pip-Boy.");
            A("main|Quickkeys", "Open the Favorites wheel to use or assign favorites.");
            A("main|Quicksave", "Save the game instantly to the quicksave slot.");
            A("main|Quickload", "Load your most recent quicksave.");
            A("main|Pause", "Open the pause menu (save, load, settings, quit).");
            A("main|QuickStats", "Open the Pip-Boy straight to the STAT tab.");
            A("main|QuickInventory", "Open the Pip-Boy straight to the INV tab.");
            A("main|QuickData", "Open the Pip-Boy straight to the DATA tab (quests, workshops).");
            A("main|QuickMap", "Open the Pip-Boy straight to the MAP tab.");
            A("main|QuickRadio", "Open the Pip-Boy straight to the RADIO tab.");
            A("SkyrimSE:main|Quick Inventory", "Open your inventory directly.");
            A("SkyrimSE:main|Quick Magic", "Open the Magic menu directly.");
            A("SkyrimSE:main|Quick Stats", "Open the Skills (constellation) menu directly.");
            A("SkyrimSE:main|Quick Map", "Open the world map directly.");
            A("main|Tween Menu", "Open the menu that leads to Items, Magic, Map and Skills.");
            A("main|Favorites", "Open the Favorites menu to quickly equip favourited items and spells.");
            A("main|Journal", "Open the Journal (quests, stats, saving and settings).");

            // ---- Hotkeys ----
            for (int i = 1; i <= 12; i++)
                A("main|Quickkey" + i, "Use the favorite in slot " + i + " of the Favorites wheel (assign favorites in the Pip-Boy).");
            for (int i = 1; i <= 8; i++)
            {
                A("SkyrimSE:main|Hotkey" + i, "Equip the favorite assigned to hotkey " + i + " (in the Favorites menu, highlight an item or spell and press this key).");
                A("FalloutNV:main|Hotkey" + i, "Use or equip the item assigned to hotkey " + i + " (assign it in the Pip-Boy).");
                A("Fallout3:main|Hotkey" + i, "Use or equip the item assigned to hotkey " + i + " (assign it in the Pip-Boy).");
            }

            // ---- System ----
            A("main|Screenshot", "Save a screenshot.");
            A("main|Multi-Screenshot", Dev);
            A("main|Console", "Open or close the developer console (~ by default).");
            A("main|ConsoleToggles", Dev);
            A("main|CameraPath", Dev);

            // ---- Menu navigation ----
            A("Menu Navigation|Accept", "Confirm or select the highlighted option in menus.");
            A("Menu Navigation|Cancel", "Go back or close the current menu.");
            A("Menu Navigation|Up,Down,Left,Right", "Move the highlight in menus.");
            A("*|Console", "Open the console while this menu is open.");
            A("Menu Buttons|LShoulder", "Switch to the previous tab or category in menus.");
            A("Menu Buttons|RShoulder", "Switch to the next tab or category in menus.");
            A("Cursor|Click", "Click with the menu cursor.");
            A("Left Stick Cursor|Click", "Click with the menu cursor when it's driven by the stick (Pip-Boy map, perk chart).");

            // ---- Console ----
            A("Console|PickPrevious", "With the console open, select the previous object under the crosshair.");
            A("Console|PickNext", "With the console open, select the next object under the crosshair.");
            A("Console|Up", "Show the previous command you typed.");
            A("Console|Down", "Show the next command you typed.");
            A("Console|PageUp", "Scroll the console output up.");
            A("Console|PageDown", "Scroll the console output down.");
            A("Console|NextFocus", "Move focus to the next console panel.");
            A("Console|PreviousFocus", "Move focus to the previous console panel.");
            A("Console|Console", "Close the console.");

            // ---- Debug ----
            A("Debug Text|PrevPage", "Previous page of the on-screen debug text (console command tdt).");
            A("Debug Text|NextPage", "Next page of the on-screen debug text (console command tdt).");
            A("Debug Text|PrevSubPage", "Previous sub-page of the on-screen debug text.");
            A("Debug Text|NextSubPage", "Next sub-page of the on-screen debug text.");
            A("Debug Overlay|*", "Control for the developer debug overlay. Normally unused in the released game.");
            A("Debug Map|ZoomIn", "Zoom in on the developer debug map.");
            A("Debug Map|ZoomOut", "Zoom out on the developer debug map.");

            // ---- Books ----
            A("Books|PrevPage", "Turn back a page in a book, note or terminal entry.");
            A("Books|NextPage", "Turn to the next page in a book, note or terminal entry.");

            // ---- Free camera ----
            A("Free Camera (TFC)|CameraZUp", "Free camera (console command tfc): move up, relative to where the camera faces.");
            A("Free Camera (TFC)|CameraZDown", "Free camera (console command tfc): move down, relative to where the camera faces.");
            A("Free Camera (TFC)|WorldZUp", "Free camera (console command tfc): move straight up.");
            A("Free Camera (TFC)|WorldZDown", "Free camera (console command tfc): move straight down.");

            // ---- Lockpicking ----
            A("Lockpicking|RotateLock", "Turn the lock to try to open it.");
            A("Lockpicking|DebugMode", Dev);
            A("Lockpicking|Cancel", "Stop picking the lock.");

            // ---- V.A.T.S. ----
            A("V.A.T.S.|Pick", "Queue a shot on the highlighted body part.");
            A("V.A.T.S.|Accept", "Carry out the queued shots.");
            A("V.A.T.S.|Cancel", "Remove a queued shot, or leave V.A.T.S.");
            A("V.A.T.S.|NextTarget", "Switch to the next target.");
            A("V.A.T.S.|PrevTarget", "Switch to the previous target.");
            A("V.A.T.S.|NextPart", "Highlight the next body part.");
            A("V.A.T.S.|PrevPart", "Highlight the previous body part.");
            A("V.A.T.S.|ZoomIn", "Zoom the V.A.T.S. view in on the target.");
            A("V.A.T.S. Playback|Cancel", "Skip the V.A.T.S. camera.");
            A("V.A.T.S. Playback|ApplyCritical", "Use your critical hit when the critical meter is full.");

            // ---- Prompts ----
            A("Multi-Activate Prompt|*", "Pick one of the options on a prompt with several actions (e.g. Transfer, Take All, Talk). The prompt shows which key does what.");

            // ---- Workshop ----
            A("Workshop|Accept", "Place, build or select in Workshop mode.");
            A("Workshop|Cancel", "Go back or put the held object down in Workshop mode.");
            A("Workshop|Up,Down,Left,Right", "Move through the Workshop build menu.");
            A("Workshop|CloseMenu", "Leave Workshop mode.");
            A("Workshop|AlternateControlsKey", "Hold to use the Workshop's alternate controls (shown on screen while held).");
            A("Workshop|Jump", "Jump while in Workshop mode.");
            A("Workshop|*", "Workshop command the game puts on a controller button. The on-screen prompts show what it does.");
            A("Workshop (Extra)|*", "Workshop command the game puts on a controller button. The on-screen prompts show what it does.");

            // ---- Misc FO4 ----
            A("Scope|Steady", "Hold your breath to steady a scoped weapon.");
            A("Sit / Wait|StartWait", "While sitting, open the Wait menu to pass time.");
            A("Looks Menu|Done", "Finish editing your appearance.");
            A("Looks Menu|Cancel", "Go back in the Looks menu.");
            A("Looks Menu|Accept", "Select the highlighted option in the Looks menu.");
            A("Looks Menu|KeyLeft,KeyRight,KeyUp,KeyDown", "Adjust the selected feature in the Looks menu.");
            A("Looks Menu|*", "Looks menu (character creation) command. The on-screen prompts show what it does.");
            A("Pause Menu|DeleteSave", "Delete the highlighted save.");
            A("Pause Menu|ResetToDefault", "Reset the settings page to its defaults.");
            A("Pause Menu|Accept", "Select in the pause menu.");
            A("Pause Menu|Cancel", "Go back or close the pause menu.");
            A("Level Up Menu|ZoomIn", "Zoom in on the perk chart.");
            A("Level Up Menu|ZoomOut", "Zoom out on the perk chart.");
            A("Level Up Menu Paging|PrevPerk", "Show the previous rank of the highlighted perk.");
            A("Level Up Menu Paging|NextPerk", "Show the next rank of the highlighted perk.");
            A("Main Menu|DeleteSave", "Delete the highlighted save.");
            A("Main Menu|Start", "Continue past the title screen (\"Press start\").");
            A("Transfer Menu (Quick Container)|QuickContainerUp", "Move up the list in the quick loot window shown when looking at a container.");
            A("Transfer Menu (Quick Container)|QuickContainerDown", "Move down the list in the quick loot window shown when looking at a container.");
            A("Vertibird|Land", "Order the Vertibird to land.");
            A("Vertibird|BailOut", "Jump out of the Vertibird.");
            A("Video Playback|Start", "Skip the video.");
            A("Robot Workbench|RotateLeft", "Rotate the robot left at the Robot Workbench.");
            A("Robot Workbench|RotateRight", "Rotate the robot right at the Robot Workbench.");

            // ---- Creations ----
            A("Creations Menu|SearchEdit", "Type in the Creations search box.");
            A("Creations Menu|LoadOrderAndDelete", "Open the load order, or delete the selected creation.");
            A("Creations Menu|LoadOrderArchive", "Archive the selected creation in the load order.");
            A("Creations Menu|CategorySideBar", "Show the category list.");
            A("Creations Menu|Options", "Show the options for the selected creation.");
            A("Creations Menu|PurchaseCredits", "Open the page to buy Creation credits.");
            A("Creations Menu|Filter", "Filter the list of creations.");
            A("Creations Menu|LikeUnlike", "Like or unlike the selected creation.");
            A("Creations Menu|MouseScrollUp", "Scroll the Creations list up.");
            A("Creations Menu|MouseScrollDown", "Scroll the Creations list down.");
            A("Creations Menu|ShowMissingContent", "Show creations your saves need that aren't installed.");
            A("Creations Menu|Accept,MouseClickAccept", "Select the highlighted creation or button.");
            A("Creations Menu|Cancel", "Go back or close the Creations menu.");
            A("Creations Menu|Up,Down,Left,Right", "Move the highlight in the Creations menu.");
            A("Creations Menu|HomeKey", "Jump to the start of the list or text.");
            A("Creations Menu|EndKey", "Jump to the end of the list or text.");
            A("Creations Menu|DeleteKey", "Delete text in the search box.");
            A("Creations Menu|*", "Creations menu command.");

            // ---- Skyrim menus ----
            A("Item Menus|Item Zoom", "Zoom in on the 3D view of the highlighted item.");
            A("Map Menu|ZoomIn", "Zoom in on the map.");
            A("Map Menu|ZoomOut", "Zoom out on the map.");
            A("Map Menu|MapLookMode", "Hold to rotate and tilt the map view.");
            A("Map Menu|PlacePlayerMarker", "Place or remove your custom marker on the map.");
            A("Map Menu|PlayerPosition", "Centre the map on your position.");
            A("Map Menu|LocalMap", "Switch between the world map and the local map.");
            A("Map Menu|LocalMapMoveMode", "Hold to pan the local map.");
            A("Map Menu|Journal", "Open the Journal from the map.");
            A("Journal|ZoomIn,ZoomOut", "Mouse-wheel scrolling in the Journal.");
        }
    }
}
