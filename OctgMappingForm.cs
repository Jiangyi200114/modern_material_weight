using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ModernMaterialWeight
{
    public sealed class OctgMappingForm : Form
    {
        private readonly ComboBox _odCombo = new ComboBox();
        private readonly ComboBox _typeCombo = new ComboBox();
        private readonly ComboBox _endCombo = new ComboBox();
        private readonly TextBox _odMmBox = new TextBox { ReadOnly = true };
        private readonly TextBox _ppfBox = new TextBox();
        private readonly TextBox _wtBox = new TextBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _statusLabel = UiLayout.Label("", 9, false);
        private readonly TextBox _kgValue = UiLayout.CopyableValue();
        private readonly TextBox _tonValue = UiLayout.CopyableValue();
        private readonly Label _selectionLabel = UiLayout.Label("选择下方规格查看米重", 9, false);
        private readonly List<Button> _buttons = new List<Button>();
        private readonly SurfacePanel _metrics = new SurfacePanel();
        private bool _isUpdating;
        private bool _error;
        private ThemePalette _theme;
        

        public OctgMappingForm()
        {
            SuspendLayout();
            Text = "OCTG 映射 · 理论米重";
            Font = new Font("Microsoft YaHei UI", 10f);
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(850, 740);
            MinimumSize = new Size(810, 710);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            BuildUi();
            _isUpdating = true;
            _typeCombo.Items.AddRange(new object[] { "全部管材", "油管", "套管" });
            _endCombo.Items.AddRange(new object[] { "全部扣型", OctgCatalog.Plain, OctgCatalog.Eu, OctgCatalog.Integral, OctgCatalog.Casing });
            _typeCombo.SelectedIndex = 0; _endCombo.SelectedIndex = 0;
            _odCombo.Items.AddRange(OctgCatalog.Items.Select(x => x.OdInch).Distinct().Cast<object>().ToArray());
            _odCombo.SelectedItem = "3-1/2";
            _isUpdating = false;
            _odCombo.SelectedIndexChanged += delegate { RefreshDiameter(); };
            _typeCombo.SelectedIndexChanged += delegate { RefreshTypes(); };
            _endCombo.SelectedIndexChanged += delegate { Browse(); };
            _ppfBox.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Lookup(false); } };
            _wtBox.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Lookup(true); } };
            _ppfBox.TextChanged += delegate { InvalidateQuery(_wtBox); };
            _wtBox.TextChanged += delegate { InvalidateQuery(_ppfBox); };
            _grid.SelectionChanged += delegate { ShowSelected(); };
            RefreshDiameter();
            ResumeLayout(true);
            using (var graphics = CreateGraphics())
            {
                float scale = graphics.DpiY / 96f;
                _grid.ColumnHeadersHeight = (int)(38 * scale);
                _grid.RowTemplate.Height = (int)(34 * scale);
                foreach (DataGridViewRow row in _grid.Rows) row.Height = _grid.RowTemplate.Height;
            }
        }

        private void BuildUi()
        {
            var root = UiLayout.Table(1);
            root.Padding = new Padding(24, 12, 24, 16);
            int[] heights = { 54, 82, 88, 112, 36 };
            foreach (int height in heights) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowCount = 7;
            Controls.Add(root);
            root.Controls.Add(UiLayout.Label("OCTG 映射与米重", 20, true), 0, 0);
            var filters = UiLayout.Table(4, true);
            for (int i = 0; i < 4; i++) filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            filters.Controls.Add(Field("管材类型", _typeCombo), 0, 0);
            filters.Controls.Add(Field("外径  in", _odCombo), 1, 0);
            filters.Controls.Add(Field("外径  mm", _odMmBox), 2, 0);
            filters.Controls.Add(Field("扣型 / 端部形式", _endCombo), 3, 0);
            root.Controls.Add(filters, 0, 1);
            var query = UiLayout.Table(2, true);
            query.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            query.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            query.Controls.Add(QueryField("磅级  lb/ft", _ppfBox, false), 0, 0);
            query.Controls.Add(QueryField("壁厚  mm", _wtBox, true), 1, 0);
            root.Controls.Add(query, 0, 2);

            _metrics.Dock = DockStyle.Fill;
            _metrics.Margin = Padding.Empty;
            _metrics.Padding = new Padding(18, 8, 18, 8);
            var metricLayout = UiLayout.Table(2);
            metricLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            metricLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            metricLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            metricLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            metricLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            metricLayout.Controls.Add(UiLayout.Label("理论米重  kg/m", 9.5f, false), 0, 0);
            metricLayout.Controls.Add(UiLayout.Label("理论米重  吨/m", 9.5f, false), 1, 0);
            metricLayout.Controls.Add(_kgValue, 0, 1); metricLayout.Controls.Add(_tonValue, 1, 1);
            metricLayout.Controls.Add(_selectionLabel, 0, 2); metricLayout.SetColumnSpan(_selectionLabel, 2);
            _metrics.Controls.Add(metricLayout); root.Controls.Add(_metrics, 0, 3);
            root.Controls.Add(_statusLabel, 0, 4);

            _grid.Dock = DockStyle.Fill;
            _grid.Margin = Padding.Empty;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BorderStyle = BorderStyle.None;
            _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersHeight = 38;
            _grid.RowTemplate.Height = 34;
            _grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            string[] headers = { "管材", "扣型 / 端部", "磅级 lb/ft", "壁厚 mm", "米重 kg/m", "米重 吨/m" };
            int[] widths = { 65, 125, 112, 106, 120, 125 };
            for (int i = 0; i < headers.Length; i++)
            {
                var col = new DataGridViewTextBoxColumn { Name = "c" + i, HeaderText = headers[i], FillWeight = widths[i], MinimumWidth = widths[i], SortMode = DataGridViewColumnSortMode.NotSortable };
                if (i >= 2) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _grid.Columns.Add(col);
            }
            root.Controls.Add(_grid, 0, 5);
            var footer = UiLayout.Label("米重按参考表列示 · 1 吨 = 1000 kg · EU 栏为空的规格不提供 EU 选项", 9, false);
            root.Controls.Add(footer, 0, 6);
        }

        private Control Field(string name, Control input)
        {
            var field = UiLayout.Table(1);
            field.Margin = new Padding(0, 0, 12, 8);
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            field.Controls.Add(UiLayout.Label(name, 9.5f, false), 0, 0);
            UiLayout.StyleInput(input);
            field.Controls.Add(input, 0, 1);
            return field;
        }

        private Control QueryField(string name, TextBox input, bool wall)
        {
            var row = UiLayout.Table(2, true);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            UiLayout.StyleInput(input);
            row.Controls.Add(input, 0, 0);
            var button = UiLayout.Button(wall ? "按壁厚查" : "按磅级查", delegate { Lookup(wall); });
            button.Margin = new Padding(10, 5, 0, 5);
            _buttons.Add(button); row.Controls.Add(button, 1, 0);
            return Field(name, row);
        }

        private IEnumerable<OctgItem> Filtered()
        {
            return OctgCatalog.Items.Where(x => x.OdInch == _odCombo.Text
                && (_typeCombo.SelectedIndex == 0 || x.Type == _typeCombo.Text)
                && (_endCombo.SelectedIndex == 0 || x.EndFinish == _endCombo.Text));
        }

        private void RefreshTypes()
        {
            if (_isUpdating) return;
            string previous = _odCombo.Text;
            _isUpdating = true;
            _odCombo.Items.Clear();
            _odCombo.Items.AddRange(OctgCatalog.Items.Where(x => _typeCombo.SelectedIndex == 0 || x.Type == _typeCombo.Text)
                .Select(x => x.OdInch).Distinct().Cast<object>().ToArray());
            _odCombo.SelectedItem = previous;
            if (_odCombo.SelectedIndex < 0) _odCombo.SelectedIndex = 0;
            string previousEnd = _endCombo.Text;
            _endCombo.Items.Clear();
            _endCombo.Items.Add("全部扣型");
            _endCombo.Items.AddRange(OctgCatalog.Items.Where(x => _typeCombo.SelectedIndex == 0 || x.Type == _typeCombo.Text)
                .Select(x => x.EndFinish).Distinct().Cast<object>().ToArray());
            _endCombo.SelectedItem = previousEnd;
            if (_endCombo.SelectedIndex < 0) _endCombo.SelectedIndex = 0;
            _isUpdating = false;
            RefreshDiameter();
        }

        private void RefreshDiameter()
        {
            if (_isUpdating) return;
            var first = OctgCatalog.Items.FirstOrDefault(x => x.OdInch == _odCombo.Text);
            _odMmBox.Text = first == null ? "" : Format(first.OdMm);
            Browse();
        }

        private void Browse()
        {
            if (_isUpdating) return;
            _isUpdating = true; _ppfBox.Clear(); _wtBox.Clear(); _isUpdating = false;
            var items = Filtered().ToList();
            ShowRows(items, false);
            Status(items.Count > 0 ? "共 " + items.Count + " 条规格 · 输入磅级或壁厚查询，也可直接选取下方规格" : "该外径下没有对应的管材 / 扣型", false);
        }

        private void InvalidateQuery(TextBox other)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            other.Clear(); _grid.Rows.Clear(); ResetMetrics();
            _isUpdating = false;
            Status("按回车或点击查询", false);
        }

        private void Lookup(bool byWall)
        {
            decimal value;
            var input = byWall ? _wtBox : _ppfBox;
            if (!decimal.TryParse(input.Text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value) || value <= 0)
            {
                ShowRows(new List<OctgItem>(), false);
                Status("请输入有效的正数" + (byWall ? "壁厚" : "磅级"), true);
                input.Focus(); return;
            }
            bool approximate;
            var matches = OctgCatalog.Lookup(Filtered(), value, byWall, out approximate);
            ShowRows(matches, matches.Count == 1);
            if (matches.Count == 0) Status("未找到对应规格，请检查外径、管材类型和扣型", true);
            else
            {
                string prefix = approximate ? "近似匹配（输入 " + Format(value) + "，容差 0.10）" : "精确匹配";
                Status(prefix + " · " + matches.Count + " 条" + (matches.Count > 1 ? "，请选择管材与扣型" : ""), false);
            }
        }

        private void ShowRows(List<OctgItem> rows, bool selectOnly)
        {
            _isUpdating = true;
            _grid.Rows.Clear();
            foreach (var item in rows)
            {
                int index = _grid.Rows.Add(item.Type, item.EndFinish, Format(item.Ppf), Format(item.WtMm),
                    item.KgPerMeter.HasValue ? Format(item.KgPerMeter.Value) : "—",
                    item.TonnesPerMeter.HasValue ? item.TonnesPerMeter.Value.ToString("0.00000", CultureInfo.InvariantCulture) : "—");
                _grid.Rows[index].Tag = item;
            }
            _grid.ClearSelection(); _grid.CurrentCell = null;
            ResetMetrics();
            _isUpdating = false;
            if (selectOnly && rows.Count == 1)
            {
                _grid.CurrentCell = _grid.Rows[0].Cells[0];
                _grid.Rows[0].Selected = true;
                ShowSelected();
            }
        }

        private void ShowSelected()
        {
            if (_isUpdating || _grid.SelectedRows.Count == 0) return;
            var item = _grid.SelectedRows[0].Tag as OctgItem;
            if (item == null) return;
            _isUpdating = true;
            _ppfBox.Text = Format(item.Ppf); _wtBox.Text = Format(item.WtMm);
            _kgValue.Text = item.KgPerMeter.HasValue ? Format(item.KgPerMeter.Value) : "—";
            _tonValue.Text = item.TonnesPerMeter.HasValue ? item.TonnesPerMeter.Value.ToString("0.00000", CultureInfo.InvariantCulture) : "—";
            _selectionLabel.Text = item.Type + " · " + item.EndFinish + " · " + item.OdInch + " in · " + Format(item.Ppf) + " lb/ft · " + Format(item.WtMm) + " mm"
                + (item.KgPerMeter.HasValue ? "" : " · 该端部形式未列米重");
            _isUpdating = false;
        }

        private void ResetMetrics()
        {
            _kgValue.Text = "—"; _tonValue.Text = "—";
            _selectionLabel.Text = "选择下方规格查看米重";
        }
        private static string Format(decimal value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private void Status(string text, bool error)
        {
            _error = error; _statusLabel.Text = text;
            _statusLabel.ForeColor = error ? Color.FromArgb(218, 92, 65) : (_theme == null ? Color.DimGray : _theme.SubFore);
        }

        public void ApplyTheme(ThemePalette theme, Color accent)
        {
            _theme = theme;
            BackColor = theme.FormBack; ForeColor = theme.FieldFore;
            StyleChildren(this);
            _metrics.BackColor = theme.CardBack; _metrics.BorderColor = theme.Border; _metrics.Invalidate();
            _grid.BackgroundColor = theme.CardBack; _grid.GridColor = theme.Divider;
            _grid.DefaultCellStyle.BackColor = theme.CardBack; _grid.DefaultCellStyle.ForeColor = theme.InputFore;
            _grid.DefaultCellStyle.SelectionBackColor = accent; _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = theme.ReadonlyBack;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = theme.MutedButtonBack;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.SubFore;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.MutedButtonBack;
            foreach (var button in _buttons) { button.BackColor = accent; button.ForeColor = Color.White; }
            _kgValue.ForeColor = accent; _tonValue.ForeColor = accent;
            _kgValue.BackColor = theme.CardBack; _tonValue.BackColor = theme.CardBack;
            _selectionLabel.ForeColor = theme.SubFore;
            Status(_statusLabel.Text, _error);
        }

        private void StyleChildren(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Label) control.ForeColor = _theme.FieldFore;
                if (control is TextBox || control is ComboBox)
                {
                    var box = control as TextBox;
                    control.BackColor = box != null && box.ReadOnly ? _theme.ReadonlyBack : _theme.InputBack;
                    control.ForeColor = _theme.InputFore;
                }
                if (!(control is DataGridView)) StyleChildren(control);
            }
        }
    }
}
