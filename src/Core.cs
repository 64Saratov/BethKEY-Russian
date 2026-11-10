// BethKEY - key binding editor for Bethesda games
// Core: scancode tables, game definitions, and the file readers/writers.
// Nothing in here touches any line of a file other than the binding values.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BethKEY
{
    public enum FileFormat { NewVegasIni, ControlMap }
    public enum KeySet { DirectInput, VirtualKey }

    public static class Codes
    {
        public const string None = "(none)";

        public static readonly List<KeyValuePair<int, string>> Keys = new List<KeyValuePair<int, string>>();
        public static readonly Dictionary<int, string> KeyByCode = new Dictionary<int, string>();
        public static readonly Dictionary<string, int> CodeByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        static void K(int code, string name)
        {
            Keys.Add(new KeyValuePair<int, string>(code, name));
            KeyByCode[code] = name;
            CodeByKey[name] = code;
        }

        static Codes()
        {
            // DirectInput (DIK_*) scancodes
            K(0x01, "Escape"); K(0x02, "1"); K(0x03, "2"); K(0x04, "3"); K(0x05, "4"); K(0x06, "5");
            K(0x07, "6"); K(0x08, "7"); K(0x09, "8"); K(0x0A, "9"); K(0x0B, "0"); K(0x0C, "- (Minus)");
            K(0x0D, "= (Equals)"); K(0x0E, "Backspace"); K(0x0F, "Tab"); K(0x10, "Q"); K(0x11, "W");
            K(0x12, "E"); K(0x13, "R"); K(0x14, "T"); K(0x15, "Y"); K(0x16, "U"); K(0x17, "I");
            K(0x18, "O"); K(0x19, "P"); K(0x1A, "[ (Left Bracket)"); K(0x1B, "] (Right Bracket)");
            K(0x1C, "Enter"); K(0x1D, "Left Ctrl"); K(0x1E, "A"); K(0x1F, "S"); K(0x20, "D");
            K(0x21, "F"); K(0x22, "G"); K(0x23, "H"); K(0x24, "J"); K(0x25, "K"); K(0x26, "L");
            K(0x27, "; (Semicolon)"); K(0x28, "' (Apostrophe)"); K(0x29, "` (Tilde)");
            K(0x2A, "Left Shift"); K(0x2B, "\\ (Backslash)"); K(0x2C, "Z"); K(0x2D, "X"); K(0x2E, "C");
            K(0x2F, "V"); K(0x30, "B"); K(0x31, "N"); K(0x32, "M"); K(0x33, ", (Comma)");
            K(0x34, ". (Period)"); K(0x35, "/ (Slash)"); K(0x36, "Right Shift"); K(0x37, "Numpad *");
            K(0x38, "Left Alt"); K(0x39, "Space"); K(0x3A, "Caps Lock"); K(0x3B, "F1"); K(0x3C, "F2");
            K(0x3D, "F3"); K(0x3E, "F4"); K(0x3F, "F5"); K(0x40, "F6"); K(0x41, "F7"); K(0x42, "F8");
            K(0x43, "F9"); K(0x44, "F10"); K(0x45, "Num Lock"); K(0x46, "Scroll Lock");
            K(0x47, "Numpad 7"); K(0x48, "Numpad 8"); K(0x49, "Numpad 9"); K(0x4A, "Numpad -");
            K(0x4B, "Numpad 4"); K(0x4C, "Numpad 5"); K(0x4D, "Numpad 6"); K(0x4E, "Numpad +");
            K(0x4F, "Numpad 1"); K(0x50, "Numpad 2"); K(0x51, "Numpad 3"); K(0x52, "Numpad 0");
            K(0x53, "Numpad ."); K(0x56, "OEM 102 (< > key)"); K(0x57, "F11"); K(0x58, "F12");
            K(0x64, "F13"); K(0x65, "F14"); K(0x66, "F15"); K(0x70, "Kana"); K(0x79, "Convert");
            K(0x7B, "NoConvert"); K(0x7D, "Yen"); K(0x8D, "Numpad ="); K(0x94, "Kanji (JP keyboard)"); K(0x90, "Prev Track");
            K(0x99, "Next Track"); K(0x9C, "Numpad Enter"); K(0x9D, "Right Ctrl"); K(0xA0, "Mute");
            K(0xA2, "Play / Pause"); K(0xA4, "Media Stop"); K(0xAE, "Volume Down"); K(0xB0, "Volume Up");
            K(0xB5, "Numpad /"); K(0xB7, "Print Screen"); K(0xB8, "Right Alt"); K(0xC5, "Pause");
            K(0xC7, "Home"); K(0xC8, "Up Arrow"); K(0xC9, "Page Up"); K(0xCB, "Left Arrow");
            K(0xCD, "Right Arrow"); K(0xCF, "End"); K(0xD0, "Down Arrow"); K(0xD1, "Page Down");
            K(0xD2, "Insert"); K(0xD3, "Delete"); K(0xDB, "Left Windows"); K(0xDC, "Right Windows");
            K(0xDD, "Menu (Apps)");
            BuildVk();
        }

        // Windows virtual-key codes (Fallout 4 control maps use these instead of DirectInput scancodes)
        public static readonly Dictionary<int, string> VkByCode = new Dictionary<int, string>();
        static void V(int code, string name) { VkByCode[code] = name; }
        static void BuildVk()
        {
            V(0x08, "Backspace"); V(0x09, "Tab"); V(0x0D, "Enter"); V(0x13, "Pause"); V(0x14, "Caps Lock");
            V(0x1B, "Escape"); V(0x20, "Space"); V(0x21, "Page Up"); V(0x22, "Page Down"); V(0x23, "End");
            V(0x24, "Home"); V(0x25, "Left Arrow"); V(0x26, "Up Arrow"); V(0x27, "Right Arrow"); V(0x28, "Down Arrow");
            V(0x2C, "Print Screen"); V(0x2D, "Insert"); V(0x2E, "Delete");
            for (int i = 0; i <= 9; i++) V(0x30 + i, i.ToString());
            for (int i = 0; i < 26; i++) V(0x41 + i, ((char)('A' + i)).ToString());
            V(0x5B, "Left Windows"); V(0x5C, "Right Windows"); V(0x5D, "Menu (Apps)");
            for (int i = 0; i <= 9; i++) V(0x60 + i, "Numpad " + i);
            V(0x6A, "Numpad *"); V(0x6B, "Numpad +"); V(0x6D, "Numpad -"); V(0x6E, "Numpad ."); V(0x6F, "Numpad /");
            for (int i = 1; i <= 24; i++) V(0x6F + i, "F" + i);
            V(0x90, "Num Lock"); V(0x91, "Scroll Lock");
            V(0xA0, "Left Shift"); V(0xA1, "Right Shift"); V(0xA2, "Left Ctrl"); V(0xA3, "Right Ctrl");
            V(0xA4, "Left Alt"); V(0xA5, "Right Alt");
            V(0xBA, "; (Semicolon)"); V(0xBB, "= (Equals)"); V(0xBC, ", (Comma)"); V(0xBD, "- (Minus)");
            V(0xBE, ". (Period)"); V(0xBF, "/ (Slash)"); V(0xC0, "` (Tilde)"); V(0xDB, "[ (Left Bracket)");
            V(0xDC, "\\ (Backslash)"); V(0xDD, "] (Right Bracket)"); V(0xDE, "' (Apostrophe)"); V(0xE2, "OEM 102 (< > key)");
        }

        public static Dictionary<int, string> Table(KeySet k) { return k == KeySet.VirtualKey ? VkByCode : KeyByCode; }
        public static string Prefix(KeySet k) { return k == KeySet.VirtualKey ? "Key code" : "Scancode"; }

        static readonly Regex SingleHex = new Regex("^0x([0-9a-fA-F]{1,8})$");

        public static int ParseSingle(string field)
        {
            if (field == null) return -1;
            Match m = SingleHex.Match(field.Trim());
            if (!m.Success) return -1;
            return Convert.ToInt32(m.Groups[1].Value, 16);
        }

        public static string Field(int code) { return "0x" + code.ToString("x"); }

        public static string FieldToName(string field, Dictionary<int, string> table, string unknownPrefix)
        {
            int c = ParseSingle(field);
            if (c < 0) return field;                       // combo / unusual value: shown as-is
            if (c == 0xFF) return None;
            string n;
            if (table.TryGetValue(c, out n)) return n;
            return unknownPrefix + " 0x" + c.ToString("X2");
        }

        public static bool IsModifier(int code, KeySet k)
        {
            if (k == KeySet.VirtualKey) return code >= 0xA0 && code <= 0xA5;
            return code == 0x2A || code == 0x36 || code == 0x1D || code == 0x9D || code == 0x38 || code == 0xB8;
        }

        /// Readable text for a key field, including combos ("0x2a+0x10" -> "Left Shift + Q")
        /// and alternatives ("0x02,0x4f" -> "1 or Numpad 1"). Returns null if any part is not a plain code.
        public static string FieldToDisplay(string field, Dictionary<int, string> table, string unknownPrefix)
        {
            if (field == null) return null;
            if (ParseSingle(field) >= 0) return FieldToName(field, table, unknownPrefix);
            List<string> alts = new List<string>();
            foreach (string alt in field.Split(','))
            {
                List<string> keys = new List<string>();
                foreach (string k in alt.Split('+'))
                {
                    int c = ParseSingle(k);
                    if (c < 0) return null;
                    keys.Add(c == 0xFF ? None : FieldToName(Field(c), table, unknownPrefix));
                }
                alts.Add(string.Join(" + ", keys.ToArray()));
            }
            return string.Join("  or  ", alts.ToArray());
        }

        /// Splits a field into its alternatives, normalised ("0x2A+0x10" -> "0x2a+0x10").
        public static List<string> Alternatives(string field)
        {
            List<string> o = new List<string>();
            if (field == null) return o;
            foreach (string alt in field.Split(','))
            {
                List<string> keys = new List<string>();
                bool ok = true;
                foreach (string k in alt.Split('+'))
                {
                    int c = ParseSingle(k);
                    if (c < 0) { ok = false; break; }
                    keys.Add(Field(c));
                }
                if (ok && !(keys.Count == 1 && keys[0] == "0xff")) o.Add(string.Join("+", keys.ToArray()));
            }
            return o;
        }

        public static string NameToField(string name, Dictionary<int, string> table, string unknownPrefix)
        {
            if (string.IsNullOrEmpty(name) || name == None) return "0xff";
            foreach (KeyValuePair<int, string> kv in table) if (kv.Value == name) return Field(kv.Key);
            Match m = Regex.Match(name, "^" + Regex.Escape(unknownPrefix) + " 0x([0-9A-Fa-f]{1,4})$");
            if (m.Success) return Field(Convert.ToInt32(m.Groups[1].Value, 16));
            return name;                                     // raw value typed/kept by the user
        }
    }

    public class NvAction
    {
        public string Key, Label, Category, Default;
        public NvAction(string key, string label, string cat, string def) { Key = key; Label = label; Category = cat; Default = def; }
    }

    public class GameDef
    {
        public string Name, ShortName, ProcessName, FileName, SecondaryFileName;
        public string[] MyGamesFolders;
        public FileFormat Format;
        public Dictionary<int, string> Mouse = new Dictionary<int, string>();
        public List<NvAction> NvActions;
        public string MissingFileHelp;
        public KeySet Keys = KeySet.DirectInput;
        public string RegistryName;        // HKLM\SOFTWARE\WOW6432Node\Bethesda Softworks\<name> "installed path"
        public string ExeName;
        public string TextMapRelPath;      // text control map inside the game folder (Skyrim)
        public List<CmEvent> MainEvents;
        public byte BinaryTag = 0x05;        // first byte of each binary section: Fallout 4 = 0x05, Skyrim = 0x02
        public string TextMapLabel;          // name of the text-map option in the Map menu
        public string[] ContextNames;        // text maps without "// Section" headers: section names in file order
        public Dictionary<string, string> Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public override string ToString() { return Name; }

        public string DefaultFolder()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string myGames = Path.Combine(docs, "My Games");
            foreach (string f in MyGamesFolders)
            {
                string p = Path.Combine(myGames, f);
                if (Directory.Exists(p)) return p;
            }
            return Path.Combine(myGames, MyGamesFolders[0]);
        }

        public static List<GameDef> All()
        {
            List<GameDef> list = new List<GameDef>();

            GameDef nv = new GameDef();
            nv.Name = "Fallout: New Vegas"; nv.ShortName = "Fallout New Vegas"; nv.ProcessName = "FalloutNV";
            nv.FileName = "FalloutPrefs.ini"; nv.SecondaryFileName = "Fallout.ini";
            nv.MyGamesFolders = new[] { "FalloutNV" };
            nv.Format = FileFormat.NewVegasIni;
            for (int i = 0; i < 8; i++) nv.Mouse[i] = i == 0 ? "Left Button" : i == 1 ? "Right Button" : i == 2 ? "Middle Button" : "Mouse Button " + (i + 1);
            nv.Mouse[8] = "Wheel Up"; nv.Mouse[9] = "Wheel Down";
            nv.NvActions = new List<NvAction> {
                new NvAction("Forward", "Move Forward", "Movement", "0011FF13"),
                new NvAction("Back", "Move Back", "Movement", "001FFF14"),
                new NvAction("Slide Left", "Strafe Left", "Movement", "001EFF17"),
                new NvAction("Slide Right", "Strafe Right", "Movement", "0020FF16"),
                new NvAction("Run", "Run (hold)", "Movement", "002AFFFF"),
                new NvAction("Always Run", "Always Run (toggle)", "Movement", "003AFFFF"),
                new NvAction("Auto Move", "Auto Move", "Movement", "0010FFFF"),
                new NvAction("Jump", "Jump", "Movement", "0039FF0D"),
                new NvAction("Crouch/Sneak", "Crouch / Sneak", "Movement", "001DFF08"),
                new NvAction("Use", "Attack / Shoot", "Combat", "00FF0011"),
                new NvAction("Block", "Aim / Block", "Combat", "00380110"),
                new NvAction("Ready Item", "Reload / Holster", "Combat", "0013FF0C"),
                new NvAction("Ammo Swap", "Ammo Swap", "Combat", "0003FF01"),
                new NvAction("Vats", "V.A.T.S.", "Combat", "002FFF0E"),
                new NvAction("Activate", "Activate / Talk", "Actions", "0012FF0A"),
                new NvAction("Grab", "Grab / Pick Up", "Actions", "002CFF09"),
                new NvAction("Toggle POV", "Toggle 1st / 3rd Person", "Actions", "0021020F"),
                new NvAction("Rest", "Wait", "Actions", "0014FF07"),
                new NvAction("Menu Mode", "Pip-Boy", "Menus", "000FFF0B"),
                new NvAction("QuickSave", "Quick Save", "Menus", "003FFFFF"),
                new NvAction("QuickLoad", "Quick Load", "Menus", "0043FFFF"),
                new NvAction("Hotkey1", "Hotkey 1", "Hotkeys", "0002FFFF"),
                new NvAction("Hotkey3", "Hotkey 3", "Hotkeys", "0004FFFF"),
                new NvAction("Hotkey4", "Hotkey 4", "Hotkeys", "0005FFFF"),
                new NvAction("Hotkey5", "Hotkey 5", "Hotkeys", "0006FFFF"),
                new NvAction("Hotkey6", "Hotkey 6", "Hotkeys", "0007FFFF"),
                new NvAction("Hotkey7", "Hotkey 7", "Hotkeys", "0008FFFF"),
                new NvAction("Hotkey8", "Hotkey 8", "Hotkeys", "0009FFFF"),
            };
            nv.MissingFileHelp = "FalloutPrefs.ini was not found in this folder.\n\nRun Fallout: New Vegas (or its launcher) once so it creates the file, or click Browse... and pick the folder that contains it.";
            list.Add(nv);

            // Fallout 3: same engine and INI format as New Vegas. No Ammo Swap, so the 2 key is Hotkey 2.
            GameDef fo3 = new GameDef();
            fo3.Name = "Fallout 3"; fo3.ShortName = "Fallout 3"; fo3.ProcessName = "Fallout3";
            fo3.FileName = "FalloutPrefs.ini"; fo3.SecondaryFileName = "Fallout.ini";
            fo3.MyGamesFolders = new[] { "Fallout3" };
            fo3.Format = FileFormat.NewVegasIni;
            fo3.Mouse = new Dictionary<int, string>(nv.Mouse);
            fo3.NvActions = new List<NvAction>();
            foreach (NvAction a in nv.NvActions)
            {
                if (a.Key == "Ammo Swap") { fo3.NvActions.Add(new NvAction("Hotkey2", "Hotkey 2", "Hotkeys", "0003FFFF")); continue; }
                fo3.NvActions.Add(a);
            }
            fo3.MissingFileHelp = "FalloutPrefs.ini was not found in this folder.\n\nRun Fallout 3 (or its launcher) once so it creates the file, or click Browse... and pick the folder that contains it.";
            list.Add(fo3);

            Dictionary<int, string> skyMouse = new Dictionary<int, string> {
                {0x0, "Left Button"}, {0x1, "Right Button"}, {0x2, "Middle Button"},
                {0x3, "Mouse Button 4"}, {0x4, "Mouse Button 5"}, {0x5, "Mouse Button 6"},
                {0x6, "Mouse Button 7"}, {0x7, "Mouse Button 8"},
                {0x8, "Wheel Up"}, {0x9, "Wheel Down"}, {0xA, "Mouse Move"}
            };
            Dictionary<int, string> fo4Mouse = new Dictionary<int, string> {
                {0x0, "Left Button"}, {0x1, "Right Button"}, {0x2, "Middle Button"},
                {0x3, "Mouse Button 4"}, {0x4, "Mouse Button 5"}, {0x5, "Mouse Button 6"},
                {0x6, "Mouse Button 7"}, {0x7, "Mouse Button 8"},
                {0x800, "Wheel Up"}, {0x900, "Wheel Down"}, {0xA00, "Mouse Move"}
            };

            GameDef sse = new GameDef();
            sse.Name = "Skyrim Special Edition"; sse.ShortName = "Skyrim Special Edition"; sse.ProcessName = "SkyrimSE";
            sse.FileName = "ControlMap_Custom.txt";
            sse.MyGamesFolders = new[] { "Skyrim Special Edition", "Skyrim Special Edition GOG", "Skyrim Special Edition EPIC", "Skyrim Special Edition MS" };
            sse.Format = FileFormat.ControlMap;
            sse.Keys = KeySet.DirectInput;
            sse.Mouse = skyMouse;
            sse.RegistryName = "Skyrim Special Edition";
            sse.ExeName = "SkyrimSE.exe";
            sse.TextMapRelPath = @"Data\Interface\Controls\PC\controlmap.txt";
            sse.BinaryTag = 0x02;
            sse.TextMapLabel = "Text control map (Data\\Interface\\Controls\\PC\\controlmap.txt)";
            sse.MainEvents = CmTables.Skyrim();
            CmTables.Categorise(sse.Categories, "Movement", "Forward", "Back", "Strafe Left", "Strafe Right", "Jump", "Sprint", "Sneak", "Run", "Toggle Always Run", "Auto-Move");
            CmTables.Categorise(sse.Categories, "Combat", "Left Attack/Block", "Right Attack/Block", "Ready Weapon", "Shout");
            CmTables.Categorise(sse.Categories, "Actions", "Activate", "Toggle POV", "Wait");
            CmTables.Categorise(sse.Categories, "Actions", "Zoom In", "Zoom Out");
            CmTables.Categorise(sse.Categories, "Hotkeys", "Hotkey1", "Hotkey2", "Hotkey3", "Hotkey4", "Hotkey5", "Hotkey6", "Hotkey7", "Hotkey8");
            CmTables.Categorise(sse.Categories, "System", "Console", "Screenshot", "Multi-Screenshot", "CameraPath");
            CmTables.Categorise(sse.Categories, "Menus", "Tween Menu", "Favorites", "Journal", "Pause", "Quicksave", "Quickload", "Quick Inventory", "Quick Magic", "Quick Stats", "Quick Map");
            sse.MissingFileHelp = "No Skyrim control map was found.\n\nSkyrim keeps key bindings in a control map file, not in its INIs. Click Browse... and either:\n\n"
                + "  - pick ControlMap_Custom.txt in Documents\\My Games\\Skyrim Special Edition (made when you rebind a key in-game), or\n"
                + "  - go to your Skyrim folder's Data\\Interface\\Controls\\PC and type controlmap.txt as the file name. BethKEY will create it from the default Skyrim map.";
            list.Add(sse);

            GameDef fo4 = new GameDef();
            fo4.Name = "Fallout 4"; fo4.ShortName = "Fallout 4"; fo4.ProcessName = "Fallout4";
            fo4.FileName = "ControlMap_Custom.txt";
            fo4.MyGamesFolders = new[] { "Fallout4" };
            fo4.Format = FileFormat.ControlMap;
            fo4.Keys = KeySet.VirtualKey;
            fo4.Mouse = fo4Mouse;
            fo4.RegistryName = "Fallout4";
            fo4.ExeName = "Fallout4.exe";
            fo4.TextMapRelPath = @"Data\F4SE\CustomControlMap.txt";
            fo4.BinaryTag = 0x05;
            // Fallout 4's map has no section headers; sections follow the engine's INPUT_CONTEXT_ID order.
            fo4.ContextNames = new[] {
                "Main Gameplay", "Menu Navigation", "Thumbstick Navigation", "Menu Buttons", "Cursor", "Left Stick Cursor",
                "Console", "Debug Text", "Books", "Debug Overlay", "Free Camera (TFC)", "Debug Map", "Lockpicking",
                "V.A.T.S.", "V.A.T.S. Playback", "Multi-Activate Prompt", "Workshop", "Scope", "Sit / Wait", "Looks Menu",
                "Workshop (Extra)", "Pause Menu", "Level Up Menu", "Level Up Menu Paging", "Main Menu", "Transfer Menu (Quick Container)",
                "Special Activate Prompt", "Two-Button Prompt", "Quick Container Perk", "Vertibird", "Video Playback",
                "Robot Workbench", "Creations Menu" };
            fo4.TextMapLabel = "F4SE control map (Data\\F4SE\\CustomControlMap.txt)";
            fo4.MainEvents = CmTables.Fallout4();
            CmTables.Categorise(fo4.Categories, "Movement", "Forward", "Back", "StrafeLeft", "StrafeRight", "Jump", "Sprint", "Sneak", "Run", "ToggleAlwaysRun", "AutoMove");
            CmTables.Categorise(fo4.Categories, "Combat", "PrimaryAttack", "SecondaryAttack", "Melee", "ReadyWeapon", "VATS");
            CmTables.Categorise(fo4.Categories, "Actions", "Activate", "TogglePOV");
            CmTables.Categorise(fo4.Categories, "Actions", "ZoomIn", "ZoomOut");
            CmTables.Categorise(fo4.Categories, "Hotkeys", "Quickkey1", "Quickkey2", "Quickkey3", "Quickkey4", "Quickkey5", "Quickkey6",
                "Quickkey7", "Quickkey8", "Quickkey9", "Quickkey10", "Quickkey11", "Quickkey12");
            CmTables.Categorise(fo4.Categories, "System", "Console", "ConsoleToggles", "Screenshot", "CameraPath");
            CmTables.Categorise(fo4.Categories, "Menus", "Pipboy", "Pause", "Quickkeys", "Quicksave", "Quickload", "QuickStats", "QuickInventory", "QuickData", "QuickMap", "QuickRadio");
            fo4.MissingFileHelp = "Could not open that Fallout 4 control map.\n\nThe normal file is ControlMap_Custom.txt in Documents\\My Games\\Fallout4. BethKEY creates it if it doesn't exist yet.\n\nUsing F4SE? Click Browse... and pick Data\\F4SE\\CustomControlMap.txt in your Fallout 4 folder.";
            list.Add(fo4);
            return new List<GameDef> { fo3, nv, fo4, sse };
        }
    }

    public class BindRow
    {
        public string Context, Event, Label;
        public int Line = -1;          // ControlMap line index
        public string Kb, Mouse;       // field text, e.g. "0x11", "0xff", "0x2a+0x0f"
        public string OrigKb, OrigMouse;
        public string Pad;             // display only, never written differently
        public int NvPad = 0xFF;       // New Vegas gamepad byte, preserved
        public string NvDefault;
        public string Source;
        public int EventIndex = -1;            // binary map event index
        public bool IsMain;                    // row is in the "Main Gameplay" block of a control map
        public string DefKb, DefMouse;         // defaults for "Vanilla defaults" (control map games)
        public bool KbLocked, MouseLocked;     // text maps: device the game doesn't use for this action
        public bool KbEditable { get { return Kb != null && !KbLocked; } }
        public bool MouseEditable { get { return Mouse != null && !MouseLocked; } }

        public bool Changed { get { return Kb != OrigKb || Mouse != OrigMouse; } }
        public void Commit() { OrigKb = Kb; OrigMouse = Mouse; }
        public void Revert() { Kb = OrigKb; Mouse = OrigMouse; }

        public string NvValue()
        {
            int kb = Codes.ParseSingle(Kb); if (kb < 0 || kb > 0xFF) kb = 0xFF;
            int m = Codes.ParseSingle(Mouse); if (m < 0 || m > 0xFF) m = 0xFF;
            return "00" + kb.ToString("X2") + m.ToString("X2") + (NvPad & 0xFF).ToString("X2");
        }
    }

    /// Text file that round-trips byte-for-byte (Latin-1) and keeps its line endings.
    public class TextLines
    {
        static readonly Encoding Enc = Encoding.GetEncoding(28591);
        public List<string> Lines = new List<string>();
        public string NewLine = "\r\n";
        public bool EndsWithNewLine = true;

        Encoding fileEnc = Enc;
        byte[] preamble = new byte[0];

        public static TextLines Load(string path) { return FromBytes(File.ReadAllBytes(path)); }

        static Encoding Detect(byte[] b, out int bomLen)
        {
            bomLen = 0;
            if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { bomLen = 2; return new UnicodeEncoding(false, false); }
            if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { bomLen = 2; return new UnicodeEncoding(true, false); }
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { bomLen = 3; return new UTF8Encoding(false); }
            return Enc;
        }

        public static string Decode(byte[] b) { int n; Encoding e = Detect(b, out n); return e.GetString(b, n, b.Length - n); }

        public static TextLines FromBytes(byte[] bytes)
        {
            TextLines t = new TextLines();
            int bom;
            t.fileEnc = Detect(bytes, out bom);
            t.preamble = new byte[bom];
            Array.Copy(bytes, t.preamble, bom);
            string text = t.fileEnc.GetString(bytes, bom, bytes.Length - bom);
            if (text.IndexOf("\r\n", StringComparison.Ordinal) < 0 && text.IndexOf('\n') >= 0) t.NewLine = "\n";
            t.EndsWithNewLine = text.Length == 0 || text.EndsWith("\n");
            string[] parts = Regex.Split(text, "\r?\n");
            t.Lines.AddRange(parts);
            if (t.EndsWithNewLine && t.Lines.Count > 0 && t.Lines[t.Lines.Count - 1] == "") t.Lines.RemoveAt(t.Lines.Count - 1);
            return t;
        }

        public void Save(string path)
        {
            string text = string.Join(NewLine, Lines.ToArray()) + (EndsWithNewLine ? NewLine : "");
            bool ro = false;
            if (File.Exists(path))
            {
                FileAttributes a = File.GetAttributes(path);
                if ((a & FileAttributes.ReadOnly) != 0) { ro = true; File.SetAttributes(path, a & ~FileAttributes.ReadOnly); }
            }
            byte[] body = fileEnc.GetBytes(text);
            byte[] all = new byte[preamble.Length + body.Length];
            Array.Copy(preamble, all, preamble.Length);
            Array.Copy(body, 0, all, preamble.Length, body.Length);
            try { File.WriteAllBytes(path, all); }
            finally { if (ro) File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly); }
        }
    }

    public static class NvIni
    {
        static readonly Regex KeyLine = new Regex(@"^(\s*)([^;#=\[\s][^=]*?)(\s*)=(.*)$");

        static int[] SectionRange(List<string> lines, string section)
        {
            int start = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (start < 0) { if (string.Equals(t, "[" + section + "]", StringComparison.OrdinalIgnoreCase)) start = i + 1; }
                else if (t.StartsWith("[")) return new[] { start, i };
            }
            return start < 0 ? null : new[] { start, lines.Count };
        }

        public static Dictionary<string, string> ReadSection(TextLines t, string section)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int[] r = SectionRange(t.Lines, section);
            if (r == null) return d;
            for (int i = r[0]; i < r[1]; i++)
            {
                Match m = KeyLine.Match(t.Lines[i]);
                if (m.Success) d[m.Groups[2].Value] = m.Groups[4].Value.Trim();
            }
            return d;
        }

        /// Rewrites existing key lines in place; appends missing keys to the end of the section.
        public static void WriteSection(TextLines t, string section, List<KeyValuePair<string, string>> values)
        {
            List<string> lines = t.Lines;
            int[] r = SectionRange(lines, section);
            if (r == null)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Trim() != "") lines.Add("");
                lines.Add("[" + section + "]");
                r = new[] { lines.Count, lines.Count };
            }
            Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in values) lookup[kv.Key] = kv.Value;
            HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = r[0]; i < r[1]; i++)
            {
                Match m = KeyLine.Match(lines[i]);
                if (!m.Success) continue;
                string k = m.Groups[2].Value;
                string v;
                if (lookup.TryGetValue(k, out v))
                {
                    lines[i] = m.Groups[1].Value + k + "=" + v;
                    done.Add(k);
                }
            }
            int at = r[1];
            while (at > r[0] && lines[at - 1].Trim() == "") at--;
            foreach (KeyValuePair<string, string> kv in values)
            {
                if (done.Contains(kv.Key)) continue;
                lines.Insert(at++, kv.Key + "=" + kv.Value);
                done.Add(kv.Key);
            }
        }

        public static void Parse(string value, out int kb, out int mouse, out int pad)
        {
            kb = mouse = pad = -1;
            string v = Regex.Replace(value ?? "", @"\s", "");
            if (!Regex.IsMatch(v, "^[0-9A-Fa-f]{1,8}$")) return;
            v = v.PadLeft(8, '0');
            kb = Convert.ToInt32(v.Substring(0, 4), 16);
            mouse = Convert.ToInt32(v.Substring(4, 2), 16);
            pad = Convert.ToInt32(v.Substring(6, 2), 16);
            if (kb == 0 || kb > 0xFE) kb = 0xFF;
        }

        public static List<BindRow> Load(GameDef g, string prefsPath, string mainPath)
        {
            Dictionary<string, string> prefs = new Dictionary<string, string>(), main = new Dictionary<string, string>();
            if (prefsPath != null && File.Exists(prefsPath)) prefs = ReadSection(TextLines.Load(prefsPath), "Controls");
            if (mainPath != null && File.Exists(mainPath)) main = ReadSection(TextLines.Load(mainPath), "Controls");
            List<BindRow> rows = new List<BindRow>();
            foreach (NvAction a in g.NvActions)
            {
                string raw = null, src = "vanilla default";
                if (prefs.TryGetValue(a.Key, out raw)) src = "FalloutPrefs.ini";
                else if (main.TryGetValue(a.Key, out raw)) src = "Fallout.ini";
                int kb, m, p;
                Parse(raw, out kb, out m, out p);
                if (kb < 0) { Parse(a.Default, out kb, out m, out p); src = "vanilla default"; }
                BindRow r = new BindRow();
                r.Context = a.Category; r.Event = a.Key; r.Label = a.Label; r.NvDefault = a.Default; r.Source = src;
                r.Kb = Codes.Field(kb); r.Mouse = Codes.Field(m); r.NvPad = p;
                r.Pad = p == 0xFF ? "-" : "0x" + p.ToString("X2");
                r.Commit();
                rows.Add(r);
            }
            return rows;
        }

        public static void Save(string path, List<BindRow> rows)
        {
            TextLines t = File.Exists(path) ? TextLines.Load(path) : new TextLines();
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            foreach (BindRow r in rows) values.Add(new KeyValuePair<string, string>(r.Event, r.NvValue()));
            WriteSection(t, "Controls", values);
            t.Save(path);
        }
    }

    /// Text control maps (Skyrim controlmap.txt, F4SE CustomControlMap.txt).
    public static class ControlMap
    {
        static readonly Regex Sep = new Regex(@"(\t+| {2,}\t*)");

        static string[] Split(string line) { return Sep.Split(line); }   // fields at even indexes

        public static List<BindRow> Load(TextLines doc, string source, GameDef g)
        {
            List<BindRow> rows = new List<BindRow>();
            string pending = null, context = null;
            bool newBlock = true, sawEvent = false;
            Dictionary<string, List<string>> blockEvents = new Dictionary<string, List<string>>();
            int blockNo = 0;
            for (int i = 0; i < doc.Lines.Count; i++)
            {
                string line = doc.Lines[i];
                string t = line.Trim();
                if (t.Length == 0) { if (sawEvent) newBlock = true; continue; }
                if (t.StartsWith("//"))
                {
                    string c = t.TrimStart('/').Trim();
                    if (c.Length > 0 && c.Length <= 40 && !c.Contains(":")) pending = c;
                    continue;
                }
                string[] f = Split(line);
                if (f.Length < 5 || f[0].Trim().Length == 0) continue;
                string kb = f[2].Trim();
                if (!kb.StartsWith("0x") && !kb.StartsWith("!")) continue;
                if (newBlock || pending != null)
                {
                    blockNo++;
                    if (g != null && g.ContextNames != null)      // fixed order: ignore comments, name by position
                        context = blockNo - 1 < g.ContextNames.Length ? g.ContextNames[blockNo - 1] : "Section " + blockNo;
                    else
                        context = pending != null ? Canon(pending) : (blockNo == 1 ? "Main Gameplay" : "Section " + blockNo);
                    if (!blockEvents.ContainsKey(context)) blockEvents[context] = new List<string>();
                    pending = null; newBlock = false;
                }
                sawEvent = true;
                List<string> be;
                if (blockEvents.TryGetValue(context, out be)) be.Add(f[0].Trim());
                BindRow r = new BindRow();
                r.Line = i; r.Event = f[0].Trim(); r.Label = Pretty(r.Event);
                r.IsMain = context == "Main Gameplay";
                r.Kb = kb; r.Mouse = f[4].Trim();
                r.Pad = f.Length > 6 ? f[6].Trim() : "";
                // A device is offered only if the map lets the action be remapped to it (columns 5/6)
                // or the game already binds it there (e.g. Console = keyboard only, Zoom = mouse wheel only).
                bool remapKb = f.Length > 8 && f[8].Trim() == "1", remapMouse = f.Length > 10 && f[10].Trim() == "1";
                r.KbLocked = !remapKb && !Bound(kb);
                r.MouseLocked = !remapMouse && !Bound(r.Mouse);
                r.Source = source;
                string cat;
                r.Context = context == "Main Gameplay" && g != null && g.Categories.TryGetValue(r.Event, out cat) ? cat : context;
                if (context == "Main Gameplay" && g != null && g.MainEvents != null)
                    foreach (CmEvent e in g.MainEvents) if (e.Name == r.Event) { r.DefKb = e.Kb; r.DefMouse = e.Mouse; }
                if (kb.StartsWith("!")) continue;   // "!Activate" style links to other events: leave alone
                if (IsAnalog(r)) continue;            // stick / mouse-movement events: nothing to bind
                if (r.KbLocked && r.MouseLocked) continue;   // controller-only (e.g. LTrigger): nothing for keyboard or mouse
                if (PadNamed.Contains(r.Event)) r.Label = Pretty(r.Event) + " (menu key)";
                r.Commit();
                rows.Add(r);
            }
            foreach (KeyValuePair<string, List<string>> kv in blockEvents)
            {
                if (!kv.Key.StartsWith("Section ")) continue;
                string guess = GuessSection(kv.Value);
                if (guess == null) continue;
                foreach (BindRow r in rows) if (r.Context == kv.Key) r.Context = guess;
            }
            return rows;
        }

        /// Section names shared by every game (Skyrim's own headers are mapped onto them).
        static readonly Dictionary<string, string> CanonNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Menu Mode", "Menu Navigation" }, { "TFC mode", "Free Camera (TFC)" }, { "Debug overlay", "Debug Overlay" },
            { "Book", "Books" }, { "Creation Club", "Creations Menu" }, { "Favorites menu", "Favorites Menu" },
            { "Stats", "Stats Menu" }, { "Favor", "Follower Commands" }
        };

        static string Canon(string name)
        {
            string c;
            return CanonNames.TryGetValue(name, out c) ? c : name;
        }

        /// Names an unlabelled section from what's in it (Skyrim's debug map section has no header).
        static string GuessSection(List<string> events)
        {
            bool look = false, move = false, zoom = false;
            foreach (string e in events)
            {
                string k = e.Replace(" ", "").ToLowerInvariant();
                if (k == "look") look = true; else if (k == "move") move = true; else if (k == "zoomin" || k == "zoomout") zoom = true;
            }
            return look && move && zoom && events.Count <= 6 ? "Debug Map" : null;
        }

        /// Menu actions named after the gamepad button they mirror; on PC they're ordinary keys (XButton = R in Fallout 4).
        static readonly HashSet<string> PadNamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "XButton", "YButton", "AButton", "BButton", "LShoulder", "RShoulder", "Select", "L3", "R3", "X", "Y", "A", "B", "LTrigger", "RTrigger" };

        /// True if the field binds anything (a plain code other than 0xff, or a multi-key value).
        static bool Bound(string field)
        {
            int c = Codes.ParseSingle(field);
            return c < 0 ? !string.IsNullOrEmpty(field) : c != 0xFF;
        }

        static bool IsAnalog(BindRow r)
        {
            int kb = Codes.ParseSingle(r.Kb), m = Codes.ParseSingle(r.Mouse), pad = Codes.ParseSingle(r.Pad);
            if (kb != 0xFF) return false;
            if (m == 0xA || m == 0xA00) return true;                       // mouse movement
            return m == 0xFF && (pad == 0x000B || pad == 0x000C);          // thumbsticks
        }

        public static string Pretty(string ev)
        {
            if (ev.Contains(" ")) return ev;
            return Regex.Replace(ev, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        }

        public static void Save(string path, TextLines doc, List<BindRow> rows)
        {
            foreach (BindRow r in rows)
            {
                if (r.Line < 0 || r.Line >= doc.Lines.Count) continue;
                string[] f = Split(doc.Lines[r.Line]);
                if (f.Length < 5) continue;
                if (f[2].Trim() == r.Kb && f[4].Trim() == r.Mouse) continue;
                f[2] = r.Kb; f[4] = r.Mouse;
                doc.Lines[r.Line] = string.Concat(f);
            }
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            doc.Save(path);
        }
    }

    public static class Backup
    {
        public static string Make(string file, string stamp)
        {
            if (!File.Exists(file)) return null;
            string dir = Path.Combine(Path.Combine(Path.GetDirectoryName(file), "BethKEY Backups"), stamp);
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, Path.GetFileName(file));
            File.Copy(file, dest, true);
            File.SetAttributes(dest, File.GetAttributes(dest) & ~FileAttributes.ReadOnly);
            Prune(Path.GetDirectoryName(dir), 10);
            return dest;
        }

        static void Prune(string root, int keep)
        {
            try
            {
                string[] dirs = Directory.GetDirectories(root);
                Array.Sort(dirs, StringComparer.Ordinal);
                for (int i = 0; i < dirs.Length - keep; i++) Directory.Delete(dirs[i], true);
            }
            catch { }
        }
    }
}
