// BethKEY - small dark-themed dialogs for presets.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BethKEY
{
    static class InputDialog
    {
        /// Asks for a line of text. Returns "" if cancelled.
        public static string Ask(IWin32Window owner, string title, string prompt, string initial)
        {
            using (Form f = new Form())
            {
                f.Text = title; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
                f.BackColor = Theme.Bg; f.ForeColor = Theme.Text; f.Font = Theme.Font;
                f.ClientSize = new Size(Theme.S(420), Theme.S(140));
                Label l = new Label { Text = prompt, AutoSize = false, ForeColor = Theme.Text };
                l.SetBounds(Theme.S(14), Theme.S(14), Theme.S(392), Theme.S(24));
                TextBox t = new TextBox { Text = initial, BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface2, ForeColor = Theme.Text };
                t.SetBounds(Theme.S(14), Theme.S(44), Theme.S(392), Theme.S(26));
                Button ok = Theme.Button("Сохранить", true); ok.AutoSize = false; ok.SetBounds(Theme.S(196), Theme.S(92), Theme.S(100), Theme.S(34));
                Button cancel = Theme.Button("Отмена", false); cancel.AutoSize = false; cancel.SetBounds(Theme.S(306), Theme.S(92), Theme.S(100), Theme.S(34));
                ok.DialogResult = DialogResult.OK; cancel.DialogResult = DialogResult.Cancel;
                f.AcceptButton = ok; f.CancelButton = cancel;
                f.Controls.AddRange(new Control[] { l, t, ok, cancel });
                return f.ShowDialog(owner) == DialogResult.OK ? t.Text : "";
            }
        }
    }

    static class PresetDialog
    {
        /// Lists a game's presets. Returns the chosen name, or null. Presets can be deleted here.
        public static string Pick(IWin32Window owner, string gameName, List<string> names, string root)
        {
            using (Form f = new Form())
            {
                f.Text = "Загрузка пресета - " + gameName; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
                f.BackColor = Theme.Bg; f.ForeColor = Theme.Text; f.Font = Theme.Font;
                f.ClientSize = new Size(Theme.S(460), Theme.S(360));
                Label l = new Label { AutoSize = false, ForeColor = Theme.Muted,
                    Text = names.Count == 0 ? "Сохранённых пресетов для игры " + gameName + " пока нет. Сначала воспользуйтесь кнопкой «Сохранить пресет...»." : "Выберите пресет. Загрузка скопирует его на нужное место (текущий файл сначала копируется в резервную копию)." };
                l.SetBounds(Theme.S(14), Theme.S(12), Theme.S(432), Theme.S(40));
                ListBox list = new ListBox { BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, IntegralHeight = false };
                list.SetBounds(Theme.S(14), Theme.S(56), Theme.S(432), Theme.S(240));
                foreach (string n in names) list.Items.Add(n);
                if (names.Count > 0) list.SelectedIndex = 0;

                Button load = Theme.Button("Загрузить", true); load.AutoSize = false; load.SetBounds(Theme.S(346), Theme.S(312), Theme.S(100), Theme.S(34));
                Button del = Theme.Button("Удалить", false); del.AutoSize = false; del.SetBounds(Theme.S(236), Theme.S(312), Theme.S(100), Theme.S(34));
                Button open = Theme.Button("Открыть папку", false); open.AutoSize = false; open.SetBounds(Theme.S(14), Theme.S(312), Theme.S(120), Theme.S(34));
                load.Enabled = del.Enabled = names.Count > 0;
                load.Click += (s, e) => { if (list.SelectedItem != null) { f.Tag = list.SelectedItem; f.DialogResult = DialogResult.OK; } };
                list.DoubleClick += (s, e) => load.PerformClick();
                del.Click += (s, e) =>
                {
                    string n = list.SelectedItem as string;
                    if (n == null) return;
                    if (MessageBox.Show(f, "Удалить пресет «" + n + "»?", "BethKEY", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    try { Directory.Delete(Path.Combine(root, n), true); list.Items.Remove(n); }
                    catch (Exception ex) { MessageBox.Show(f, ex.Message, "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    load.Enabled = del.Enabled = list.Items.Count > 0;
                    if (list.Items.Count > 0) list.SelectedIndex = 0;
                };
                open.Click += (s, e) => { try { Directory.CreateDirectory(root); Process.Start("explorer.exe", "\"" + root + "\""); } catch { } };
                f.Controls.AddRange(new Control[] { l, list, load, del, open });
                return f.ShowDialog(owner) == DialogResult.OK ? f.Tag as string : null;
            }
        }
    }
}
