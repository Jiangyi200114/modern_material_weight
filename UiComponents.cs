using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ModernMaterialWeight
{
    public sealed class SurfacePanel : Panel
    {
        public Color BorderColor { get; set; }
        public SurfacePanel()
        {
            DoubleBuffered = true;
            BorderColor = Color.FromArgb(226, 232, 240);
            Padding = new Padding(24);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Control ancestor = Parent;
            while (ancestor != null && ancestor.BackColor == Color.Transparent) ancestor = ancestor.Parent;
            e.Graphics.Clear(ancestor == null ? BackColor : ancestor.BackColor);
            if (Width < 2 || Height < 2) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float r = 16;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(Width - r - 1, 0, r, r, 270, 90);
                path.AddArc(Width - r - 1, Height - r - 1, r, r, 0, 90);
                path.AddArc(0, Height - r - 1, r, r, 90, 90);
                path.CloseFigure();
                using (var brush = new SolidBrush(BackColor)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(BorderColor)) e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal static class UiLayout
    {
        public static TableLayoutPanel Table(int columns, bool singleRow = false)
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columns, Margin = Padding.Empty, BackColor = Color.Transparent };
            if (singleRow) { table.RowCount = 1; table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); }
            return table;
        }
        public static Label Label(string text, float size, bool bold)
        {
            return new Label { Text = text, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = Padding.Empty };
        }
        public static TextBox CopyableValue()
        {
            var box = new TextBox { Text = "—", ReadOnly = true, BorderStyle = BorderStyle.None,
                Multiline = true, WordWrap = false, Dock = DockStyle.Fill, Margin = Padding.Empty,
                Font = new Font("Microsoft YaHei UI", 25f, FontStyle.Bold), HideSelection = false,
                ShortcutsEnabled = true, Cursor = Cursors.IBeam };
            box.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.A) { box.SelectAll(); e.SuppressKeyPress = true; }
            };
            return box;
        }

        public static void StyleInput(Control input)
        {
            input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            input.Margin = new Padding(0, 4, 0, 4);
            input.Font = new Font("Microsoft YaHei UI", 11f);
            var combo = input as ComboBox;
            if (combo != null) { combo.DropDownStyle = ComboBoxStyle.DropDownList; combo.FlatStyle = FlatStyle.Flat;
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += delegate(object sender, DrawItemEventArgs e)
                {
                    bool selected = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
                    using (var brush = new SolidBrush(selected ? SystemColors.Highlight : combo.BackColor)) e.Graphics.FillRectangle(brush, e.Bounds);
                    string text = e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
                    TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, selected ? Color.White : combo.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    e.DrawFocusRectangle();
                };
                combo.FontChanged += delegate { combo.ItemHeight = Math.Max(combo.Font.Height + 6, 24); };
                combo.ItemHeight = Math.Max(combo.Font.Height + 6, 24); }
            var box = input as TextBox;
            if (box != null) box.BorderStyle = BorderStyle.FixedSingle;
        }
        public static Button Button(string text, Action action)
        {
            var button = new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0), Font = new Font("Microsoft YaHei UI", 10f), UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate { action(); };
            return button;
        }
    }
}
