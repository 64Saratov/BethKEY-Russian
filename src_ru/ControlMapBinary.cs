// BethKEY - Fallout 4 / Skyrim SE in-game key map support.
//
// When you rebind a key in the game's Controls menu, Fallout 4 and Skyrim SE write
// Documents\My Games\<game>\ControlMap_Custom.txt. Despite the name it is a BINARY file:
//
//   3 sections, in order: keyboard, mouse, gamepad
//   section = [u8 tag] [u16 big-endian section length, header included] [records...]
//             tag = 0x05 in Fallout 4, 0x02 in Skyrim SE/AE (both read from the games' own exes)
//   record  = [u8 event index] [u32 big-endian key code]
//
// The event index is the event's position in the "Main Gameplay" block of the game's
// default control map. Only events the game marks as remappable for that device appear.
// Fallout 4 uses Windows virtual-key codes; Skyrim uses DirectInput scancodes.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace BethKEY
{
    public class CmEvent
    {
        public int Index;
        public string Name, Kb, Mouse, Pad;
        public bool RemapKb, RemapMouse, RemapPad;
    }

    public static class CmTables
    {
        /// Set by the UI to read bundled resources (Skyrim default maps).
        public static Func<string, string> ResourceText = name => null;

        public static void Categorise(Dictionary<string, string> d, string cat, params string[] events)
        {
            foreach (string e in events) d[e] = cat;
        }

        /// Parses the "Main Gameplay" block (first block of events) of a default control map.
        public static List<CmEvent> ParseMain(string text)
        {
            List<CmEvent> list = new List<CmEvent>();
            if (string.IsNullOrEmpty(text)) return list;
            bool started = false;
            foreach (string raw in Regex.Split(text, "\r?\n"))
            {
                string t = raw.Trim();
                if (t.StartsWith("//")) continue;
                if (t.Length == 0) { if (started) break; continue; }
                string[] f = Regex.Split(t, @"\t+");
                if (f.Length < 7) continue;
                started = true;
                CmEvent e = new CmEvent();
                e.Index = list.Count; e.Name = f[0].Trim(); e.Kb = f[1].Trim(); e.Mouse = f[2].Trim(); e.Pad = f[3].Trim();
                e.RemapKb = f[4].Trim() == "1"; e.RemapMouse = f[5].Trim() == "1"; e.RemapPad = f[6].Trim() == "1";
                list.Add(e);
            }
            return list;
        }

        public static List<CmEvent> Skyrim()
        {
            return ParseMain(ResourceText("Skyrim_AE_NewFormat.txt"));
        }

        // Fallout 4 "Main Gameplay" block, as built into Fallout4.exe (keyboard = Windows virtual-key codes).
        const string Fo4Main =
@"Forward	0x57	0xff	0xff	1	1	0	0x1
Back	0x53	0xff	0xff	1	1	0	0x1
StrafeLeft	0x41	0xff	0xff	1	1	0	0x1
StrafeRight	0x44	0xff	0xff	1	1	0	0x1
Move	0xff	0xff	0x000b	0	0	0	0x1
Look	0xff	0xa00	0x000c	0	0	0	0x2
PrimaryAttack	0xff	0x0	0x000a	1	1	1	0x40
SecondaryAttack	0xff	0x1	0x0009	1	1	1	0x40
Melee	0xa4	0xff	0x0200	1	1	1	0x40
Activate	0x45	0xff	0x1000	1	1	1	0x04
ReadyWeapon	0x52	0xff	0x4000	1	1	1	0x40
Pipboy	0x09	0xff	0x2000	1	1	1	0x108
TogglePOV	0x56	0x2	0x0020	1	1	1	0x20
ZoomIn	0xff	0x800	0xff	0	0	0	0x220
ZoomOut	0xff	0x900	0xff	0	0	0	0x220
Jump	0x20	0xff	0x8000	1	1	1	0x401
Sprint	0xa0	0xff	0x0040	1	1	1	0x1
VATS	0x51	0xff	0x0100	1	1	1	0x40
Sneak	0xa2	0xff	0x0080	1	1	1	0x81
Run	0x43	0xff	0xff	1	1	0	0x1
ToggleAlwaysRun	0x14	0xff	0xff	1	1	0	0x1
AutoMove	0x58	0xff	0xff	1	1	0	0x1
Quickkeys	0x46	0xff	0xff	1	0	0	0x8
QuickkeyUp	0xff	0xff	0x0001	0	0	0	0x8
QuickkeyDown	0xff	0xff	0x0002	0	0	0	0x8
QuickkeyLeft	0xff	0xff	0x0004	0	0	0	0x8
QuickkeyRight	0xff	0xff	0x0008	0	0	0	0x8
Quickkey1	0x31	0xff	0xff	0	0	0	0x8
Quickkey2	0x32	0xff	0xff	0	0	0	0x8
Quickkey3	0x33	0xff	0xff	0	0	0	0x8
Quickkey4	0x34	0xff	0xff	0	0	0	0x8
Quickkey5	0x35	0xff	0xff	0	0	0	0x8
Quickkey6	0x36	0xff	0xff	0	0	0	0x8
Quickkey7	0x37	0xff	0xff	0	0	0	0x8
Quickkey8	0x38	0xff	0xff	0	0	0	0x8
Quickkey9	0x39	0xff	0xff	0	0	0	0x8
Quickkey10	0x30	0xff	0xff	0	0	0	0x8
Quickkey11	0xbd	0xff	0xff	0	0	0	0x8
Quickkey12	0xbb	0xff	0xff	0	0	0	0x8
Quicksave	0x74	0xff	0xff	1	0	0
Quickload	0x78	0xff	0xff	1	0	0
Pause	0x1b	0xff	0x0010	1	1	1	0x8
Screenshot	0x2c	0xff	0xff	0	0	0	0x10
Console	0xc0	0xff	0xff	0	0	0	0x10
ConsoleToggles	0x2d	0xff	0xff	0	0	0	0x10
CameraPath	0x7b	0xff	0xff	0	0	0
QuickStats	0x4b	0xff	0xff	1	1	0	0x108
QuickInventory	0x49	0xff	0xff	1	1	0	0x108
QuickData	0x4a	0xff	0xff	1	1	0	0x108
QuickMap	0x4d	0xff	0xff	1	1	0	0x108
QuickRadio	0x4f	0xff	0xff	1	1	0	0x108
";

        public static List<CmEvent> Fallout4() { return ParseMain(Fo4Main); }
    }

    public class CmBinary
    {
        public class Section
        {
            public byte Tag = 0x05;
            public List<int> Events = new List<int>();
            public List<uint> Keys = new List<uint>();
        }

        public List<Section> Sections = new List<Section>();   // 0 keyboard, 1 mouse, 2 gamepad

        /// Heuristic: a binary map contains zero bytes near the start; text (incl. UTF-8/UTF-16 with BOM) is excluded.
        public static bool LooksBinary(byte[] b)
        {
            if (b.Length < 3) return false;
            if ((b[0] == 0xFF && b[1] == 0xFE) || (b[0] == 0xFE && b[1] == 0xFF) || (b[0] == 0xEF && b[1] == 0xBB)) return false;
            for (int i = 0; i < b.Length && i < 64; i++) if (b[i] == 0) return true;
            return false;
        }

        public static CmBinary Parse(byte[] b)
        {
            CmBinary m = new CmBinary();
            int pos = 0;
            while (pos < b.Length)
            {
                if (pos + 3 > b.Length) return null;
                int len = (b[pos + 1] << 8) | b[pos + 2];
                if (len < 3 || pos + len > b.Length || (len - 3) % 5 != 0) return null;
                Section s = new Section { Tag = b[pos] };
                for (int p = pos + 3; p < pos + len; p += 5)
                {
                    s.Events.Add(b[p]);
                    s.Keys.Add((uint)((b[p + 1] << 24) | (b[p + 2] << 16) | (b[p + 3] << 8) | b[p + 4]));
                }
                m.Sections.Add(s);
                pos += len;
            }
            return m.Sections.Count == 3 ? m : null;
        }

        public byte[] Build()
        {
            MemoryStream ms = new MemoryStream();
            foreach (Section s in Sections)
            {
                int len = 3 + 5 * s.Events.Count;
                if (len > 0xFFFF) throw new InvalidDataException("Секция слишком большая.");
                ms.WriteByte(s.Tag); ms.WriteByte((byte)(len >> 8)); ms.WriteByte((byte)(len & 0xFF));
                for (int i = 0; i < s.Events.Count; i++)
                {
                    uint k = s.Keys[i];
                    ms.WriteByte((byte)s.Events[i]);
                    ms.WriteByte((byte)(k >> 24)); ms.WriteByte((byte)(k >> 16)); ms.WriteByte((byte)(k >> 8)); ms.WriteByte((byte)k);
                }
            }
            return ms.ToArray();
        }

        static bool Remap(CmEvent e, int device) { return device == 0 ? e.RemapKb : device == 1 ? e.RemapMouse : e.RemapPad; }

        /// True when every section holds exactly the events the game table says are remappable for that device.
        public bool Matches(List<CmEvent> table, out string why)
        {
            why = null;
            if (table == null || table.Count == 0) { why = "нет таблицы событий для этой игры"; return false; }
            for (int d = 0; d < 3; d++)
            {
                HashSet<int> want = new HashSet<int>();
                foreach (CmEvent e in table) if (Remap(e, d)) want.Add(e.Index);
                HashSet<int> have = new HashSet<int>(Sections[d].Events);
                if (!want.SetEquals(have) || have.Count != Sections[d].Events.Count)
                {
                    why = "секция " + (d + 1) + " содержит " + Sections[d].Events.Count + " записей, ожидалось " + want.Count;
                    return false;
                }
            }
            return true;
        }

        static uint Single(string field)
        {
            int c = Codes.ParseSingle(field);
            return c < 0 ? 0xFFu : (uint)c;
        }

        /// A fresh map with the game's default bindings (sorted by key like the game writes it).
        public static CmBinary FromDefaults(List<CmEvent> table) { return FromDefaults(table, 0x05); }

        public static CmBinary FromDefaults(List<CmEvent> table, byte tag)
        {
            CmBinary m = new CmBinary();
            for (int d = 0; d < 3; d++)
            {
                List<KeyValuePair<uint, int>> recs = new List<KeyValuePair<uint, int>>();
                foreach (CmEvent e in table)
                    if (Remap(e, d)) recs.Add(new KeyValuePair<uint, int>(Single(d == 0 ? e.Kb : d == 1 ? e.Mouse : e.Pad), e.Index));
                recs.Sort((x, y) => x.Key != y.Key ? x.Key.CompareTo(y.Key) : x.Value.CompareTo(y.Value));
                Section s = new Section { Tag = tag };
                foreach (KeyValuePair<uint, int> r in recs) { s.Events.Add(r.Value); s.Keys.Add(r.Key); }
                m.Sections.Add(s);
            }
            return m;
        }

        public List<BindRow> ToRows(GameDef g, string source) { return ToRows(g, g.MainEvents, source); }

        public List<BindRow> ToRows(GameDef g, List<CmEvent> table, string source)
        {
            List<BindRow> rows = new List<BindRow>();
            foreach (CmEvent e in table)
            {
                if (!e.RemapKb && !e.RemapMouse) continue;
                BindRow r = new BindRow();
                r.EventIndex = e.Index; r.Event = e.Name; r.Label = ControlMap.Pretty(e.Name); r.IsMain = true;
                string cat;
                r.Context = g.Categories.TryGetValue(e.Name, out cat) ? cat : "Other";
                r.Kb = e.RemapKb ? Codes.Field((int)Find(0, e.Index)) : null;
                r.Mouse = e.RemapMouse ? Codes.Field((int)Find(1, e.Index)) : null;
                r.DefKb = e.RemapKb ? Codes.Field((int)Single(e.Kb)) : null;
                r.DefMouse = e.RemapMouse ? Codes.Field((int)Single(e.Mouse)) : null;
                int pi = Sections[2].Events.IndexOf(e.Index);
                uint pad = pi >= 0 ? Sections[2].Keys[pi] : Single(e.Pad);
                r.Pad = pad == 0xFF ? "-" : "0x" + pad.ToString("X4");
                r.Source = source;
                r.Commit();
                rows.Add(r);
            }
            return rows;
        }

        uint Find(int device, int ev)
        {
            int i = Sections[device].Events.IndexOf(ev);
            return i >= 0 ? Sections[device].Keys[i] : 0xFFu;
        }

        /// Writes the rows' keyboard/mouse values back. Gamepad section is untouched.
        public void Apply(List<BindRow> rows)
        {
            foreach (BindRow r in rows)
            {
                if (r.EventIndex < 0) continue;
                if (r.Kb != null) Set(0, r.EventIndex, Single(r.Kb));
                if (r.Mouse != null) Set(1, r.EventIndex, Single(r.Mouse));
            }
        }

        void Set(int device, int ev, uint key)
        {
            int i = Sections[device].Events.IndexOf(ev);
            if (i >= 0) Sections[device].Keys[i] = key;
            else { Sections[device].Events.Add(ev); Sections[device].Keys.Add(key); }
        }
    }

    public static class FileUtil
    {
        /// Writes a file, creating its folder, and keeps a read-only flag if the file had one.
        public static void WriteAllBytes(string path, byte[] bytes)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            bool ro = false;
            if (File.Exists(path))
            {
                FileAttributes a = File.GetAttributes(path);
                if ((a & FileAttributes.ReadOnly) != 0) { ro = true; File.SetAttributes(path, a & ~FileAttributes.ReadOnly); }
            }
            try { File.WriteAllBytes(path, bytes); }
            finally { if (ro) File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly); }
        }
    }

    /// Minimal reader for Fallout 4 general (GNRL) .ba2 archives: finds and unpacks one file.
    public static class Ba2
    {
        public static byte[] Extract(string archive, string innerPath)
        {
            using (FileStream fs = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(fs))
            {
                if (fs.Length < 24) return null;
                if (new string(br.ReadChars(4)) != "BTDX") return null;
                uint version = br.ReadUInt32();
                if (new string(br.ReadChars(4)) != "GNRL") return null;
                uint count = br.ReadUInt32();
                long nameOffset = (long)br.ReadUInt64();
                if (version == 2 || version == 3) br.ReadUInt64();           // Starfield-style headers
                if (version == 3) br.ReadUInt32();
                long recordStart = fs.Position;
                if (nameOffset <= 0 || nameOffset >= fs.Length || count > 1000000) return null;
                fs.Position = nameOffset;
                int hit = -1;
                for (int i = 0; i < count; i++)
                {
                    int len = br.ReadUInt16();
                    string name = Encoding.GetEncoding(28591).GetString(br.ReadBytes(len));
                    if (string.Equals(name.Replace('/', '\\'), innerPath, StringComparison.OrdinalIgnoreCase)) { hit = i; break; }
                }
                if (hit < 0) return null;
                fs.Position = recordStart + 36L * hit + 16;
                long offset = (long)br.ReadUInt64();
                uint packed = br.ReadUInt32(), unpacked = br.ReadUInt32();
                fs.Position = offset;
                if (packed == 0) return br.ReadBytes((int)unpacked);
                byte[] z = br.ReadBytes((int)packed);
                using (MemoryStream src = new MemoryStream(z, 2, z.Length - 2))        // skip zlib header
                using (DeflateStream ds = new DeflateStream(src, CompressionMode.Decompress))
                using (MemoryStream dst = new MemoryStream())
                {
                    ds.CopyTo(dst);
                    return dst.ToArray();
                }
            }
        }
    }

    public static class Fo4Maps
    {
        public const string MapPath = @"Interface\Controls\PC\ControlMap.txt";

        /// A control map installed by a mod (loose file, or inside a mod's .ba2), or null for vanilla.
        public static string FindModded(string install, out string from)
        {
            from = null;
            if (install == null) return null;
            string data = Path.Combine(install, "Data");
            string loose = Path.Combine(data, MapPath);
            try
            {
                if (File.Exists(loose)) { from = loose; return Read(File.ReadAllBytes(loose)); }
                if (!Directory.Exists(data)) return null;
                List<string> order = PluginOrder();
                string best = null; int bestRank = -1; DateTime bestTime = DateTime.MinValue;
                foreach (string ba2 in Directory.GetFiles(data, "*.ba2"))
                {
                    string file = Path.GetFileName(ba2);
                    if (file.StartsWith("Fallout4 - ", StringComparison.OrdinalIgnoreCase) || file.StartsWith("DLC", StringComparison.OrdinalIgnoreCase)) continue;
                    int dash = file.IndexOf(" - ", StringComparison.Ordinal);
                    string plugin = dash > 0 ? file.Substring(0, dash) : Path.GetFileNameWithoutExtension(file);
                    int rank = order.IndexOf(plugin.ToLowerInvariant());
                    byte[] bytes;
                    try { bytes = Ba2.Extract(ba2, MapPath); } catch { continue; }
                    if (bytes == null) continue;
                    DateTime t = File.GetLastWriteTime(ba2);
                    if (rank > bestRank || (rank == bestRank && t > bestTime)) { best = ba2; bestRank = rank; bestTime = t; from = ba2; }
                }
                if (best != null) return Read(Ba2.Extract(best, MapPath));
            }
            catch { }
            return null;
        }

        static List<string> PluginOrder()
        {
            List<string> o = new List<string>();
            try
            {
                string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Fallout4\plugins.txt");
                if (!File.Exists(f)) return o;
                foreach (string line in File.ReadAllLines(f))
                {
                    string t = line.Trim().TrimStart('*');
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    o.Add(Path.GetFileNameWithoutExtension(t).ToLowerInvariant());
                }
            }
            catch { }
            return o;
        }

        /// The default map built into Fallout4.exe.
        public static string FromExe(string exe)
        {
            if (exe == null || !File.Exists(exe)) return null;
            byte[] b = File.ReadAllBytes(exe);
            string text = Encoding.GetEncoding(28591).GetString(b);
            Match m = Regex.Match(text, "Forward\t+0x57\t");
            if (!m.Success) return null;
            int end = text.IndexOf('\0', m.Index);
            return end > m.Index ? text.Substring(m.Index, end - m.Index) : null;
        }

        public static string FindExe(string nearPath, GameDef g)
        {
            try
            {
                DirectoryInfo d = new FileInfo(nearPath).Directory;
                for (int i = 0; i < 6 && d != null; i++, d = d.Parent)
                {
                    string exe = Path.Combine(d.FullName, g.ExeName);
                    if (File.Exists(exe)) return exe;
                }
            }
            catch { }
            string install = GamePaths.InstallFolder(g);
            return install == null ? null : Path.Combine(install, g.ExeName);
        }

        public static string Read(byte[] b) { return b == null ? null : TextLines.Decode(b); }
    }

    public static class GamePaths
    {
        /// Install folder from the registry (Steam/GOG installers write this), or null.
        public static string InstallFolder(GameDef g)
        {
            if (string.IsNullOrEmpty(g.RegistryName)) return null;
            foreach (string root in new[] { @"SOFTWARE\WOW6432Node\Bethesda Softworks\", @"SOFTWARE\Bethesda Softworks\" })
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root + g.RegistryName))
                    {
                        string p = k == null ? null : k.GetValue("installed path") as string;
                        if (!string.IsNullOrEmpty(p) && Directory.Exists(p) && (g.ExeName == null || File.Exists(Path.Combine(p, g.ExeName)))) return p;
                    }
                }
                catch { }
            }
            // GOG installs register under GOG.com\Games\<id> with a "path" value
            foreach (string root in new[] { @"SOFTWARE\WOW6432Node\GOG.com\Games", @"SOFTWARE\GOG.com\Games" })
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey games = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root))
                    {
                        if (games == null || g.ExeName == null) continue;
                        foreach (string id in games.GetSubKeyNames())
                            using (Microsoft.Win32.RegistryKey k = games.OpenSubKey(id))
                            {
                                string p = k == null ? null : k.GetValue("path") as string;
                                if (!string.IsNullOrEmpty(p) && File.Exists(Path.Combine(p, g.ExeName))) return p;
                            }
                    }
                }
                catch { }
            }
            return null;
        }

        /// Folder holding the game's exe, found by walking up from a path (falls back to the registry).
        public static string GameRootFrom(string path, GameDef g)
        {
            try
            {
                DirectoryInfo d = new FileInfo(path).Directory;
                for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                    if (File.Exists(Path.Combine(d.FullName, g.ExeName))) return d.FullName;
            }
            catch { }
            return InstallFolder(g);
        }

        /// Skyrim's full default control map: from the player's own Skyrim - Interface.bsa, else BethKEY's copy.
        public static string SkyrimDefaultMap(string targetPath, GameDef g, out string source)
        {
            string root = GameRootFrom(targetPath, g);
            string t = root == null ? null : BsaFile(Path.Combine(root, @"Data\Skyrim - Interface.bsa"), @"interface\controls\pc", "controlmap.txt");
            if (t != null) { source = "Skyrim - Interface.bsa"; return t; }
            source = "встроенная в BethKEY карта Skyrim";
            return SkyrimDefaultText(targetPath);
        }

        /// Reads one uncompressed file from a Skyrim SE BSA (v104/v105). Null if missing or compressed.
        public static string BsaFile(string bsaPath, string folder, string file)
        {
            try
            {
                if (!File.Exists(bsaPath)) return null;
                Encoding latin1 = Encoding.GetEncoding(28591);
                using (FileStream fs = new FileStream(bsaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader r = new BinaryReader(fs))
                {
                    if (latin1.GetString(r.ReadBytes(4)) != "BSA\0") return null;
                    uint version = r.ReadUInt32(); r.ReadUInt32();
                    uint flags = r.ReadUInt32(), folders = r.ReadUInt32(); r.ReadUInt32();
                    r.ReadUInt32(); uint fileNamesLen = r.ReadUInt32(); r.ReadUInt32();
                    if (version != 104 && version != 105) return null;
                    if ((flags & 0x1) == 0 || (flags & 0x2) == 0) return null;          // need folder + file names
                    uint[] counts = new uint[folders];
                    for (int i = 0; i < folders; i++)
                    {
                        r.ReadUInt64(); counts[i] = r.ReadUInt32();
                        if (version == 105) { r.ReadUInt32(); r.ReadUInt64(); } else r.ReadUInt32();
                    }
                    List<string> folderOf = new List<string>(); List<uint> sizes = new List<uint>(), offsets = new List<uint>();
                    for (int i = 0; i < folders; i++)
                    {
                        int len = r.ReadByte();
                        string name = latin1.GetString(r.ReadBytes(len)).TrimEnd('\0');
                        for (int k = 0; k < counts[i]; k++) { r.ReadUInt64(); sizes.Add(r.ReadUInt32()); offsets.Add(r.ReadUInt32()); folderOf.Add(name); }
                    }
                    string[] names = latin1.GetString(r.ReadBytes((int)fileNamesLen)).Split('\0');
                    for (int i = 0; i < folderOf.Count && i < names.Length; i++)
                    {
                        if (!string.Equals(folderOf[i], folder, StringComparison.OrdinalIgnoreCase) || !string.Equals(names[i], file, StringComparison.OrdinalIgnoreCase)) continue;
                        bool compressed = ((flags & 0x4) != 0) ^ ((sizes[i] & 0x40000000) != 0);
                        if (compressed) return null;
                        fs.Position = offsets[i];
                        int size = (int)(sizes[i] & 0x3FFFFFFF);
                        if ((flags & 0x100) != 0) { int nl = r.ReadByte(); r.ReadBytes(nl); size -= nl + 1; }
                        return TextLines.Decode(r.ReadBytes(size));
                    }
                }
            }
            catch { }
            return null;
        }

        /// Pre-AE (1.5.x) Skyrim uses the "old format" default map; everything newer uses the AE one.
        public static string SkyrimDefaultText(string controlMapPath)
        {
            string name = "Skyrim_AE_NewFormat.txt";
            try
            {
                DirectoryInfo d = new FileInfo(controlMapPath).Directory;
                for (int i = 0; i < 6 && d != null; i++, d = d.Parent)
                {
                    string exe = Path.Combine(d.FullName, "SkyrimSE.exe");
                    if (!File.Exists(exe)) continue;
                    System.Diagnostics.FileVersionInfo v = System.Diagnostics.FileVersionInfo.GetVersionInfo(exe);
                    if (v.FileMajorPart == 1 && v.FileMinorPart <= 5) name = "Skyrim_SE_OldFormat.txt";
                    break;
                }
            }
            catch { }
            return CmTables.ResourceText(name);
        }
    }
}
