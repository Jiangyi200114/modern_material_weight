using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("HopeAgent")]
[assembly: AssemblyProduct("HopeAgent")]
[assembly: AssemblyCompany("周泉宇")]
[assembly: AssemblyVersion("3.0.1.0")]
[assembly: AssemblyFileVersion("3.0.1.0")]
[assembly: AssemblyInformationalVersion("v3.0.1")]

namespace ModernMaterialWeight
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public sealed class ShapeDefinition
    {
        public string Key { get; private set; }
        public string Label { get; private set; }
        public string[] Fields { get; private set; }

        public ShapeDefinition(string key, string label, params string[] fields)
        {
            Key = key;
            Label = label;
            Fields = fields;
        }
    }

    public sealed class ThemePalette
    {
        public Color FormBack { get; set; }
        public Color CardBack { get; set; }
        public Color TitleFore { get; set; }
        public Color SubFore { get; set; }
        public Color FieldFore { get; set; }
        public Color InputBack { get; set; }
        public Color InputFore { get; set; }
        public Color Border { get; set; }
        public Color Divider { get; set; }
        public Color MutedButtonBack { get; set; }
        public Color MutedButtonFore { get; set; }
        public Color ResultLabelFore { get; set; }
        public Color ReadonlyBack { get; set; }
    }

    public sealed partial class MainForm : Form
    {
        private const string AppVersion = "v3.0.1";
        private const string AppAuthor = "周泉宇";

        private readonly FontFamily uiFontFamily;
        private readonly Dictionary<string, decimal> materialDensity = new Dictionary<string, decimal>
        {
            { "碳钢", 7.85m },
            { "铜", 8.9m },
            { "灰铸铁", 7.2m },
            { "不锈钢", 7.9m },
            { "铝", 2.7m },
            { "铸钢", 7.8m },
            { "其它", 7.8m }
        };

        private readonly List<ShapeDefinition> shapes = new List<ShapeDefinition>
        {
            new ShapeDefinition("rect_plate", "矩形板材", "长度(mm)", "宽度(mm)", "厚度(mm)"),
            new ShapeDefinition("cylinder", "圆柱体", "直径(mm)", "长度(mm)"),
            new ShapeDefinition("tube_od_id", "外/内径管材", "外径(mm)", "内径(mm)", "长度(mm)"),
            new ShapeDefinition("tube_od_t", "外径×壁厚管材", "外径(mm)", "壁厚(mm)", "长度(mm)"),
            new ShapeDefinition("square_bar", "正方形截面形材", "边长(mm)", "长度(mm)"),
            new ShapeDefinition("cone", "圆锥体", "底半径(mm)", "高度(mm)"),
            new ShapeDefinition("frustum", "圆台", "大半径(mm)", "小半径(mm)", "高度(mm)"),
            new ShapeDefinition("wire_by_mpm", "每米重量线材", "每米重(kg/m)", "长度(m)")
        };

        private readonly Dictionary<string, Color> brandColors = new Dictionary<string, Color>
        {
            { "企业蓝", Color.FromArgb(45, 140, 255) },
            { "科技青", Color.FromArgb(18, 166, 166) },
            { "商务紫", Color.FromArgb(96, 92, 240) },
            { "稳重灰", Color.FromArgb(91, 103, 128) }
        };

        private readonly ComboBox materialCombo = new ComboBox();
        private readonly ComboBox shapeCombo = new ComboBox();
        private readonly ComboBox themeCombo = new ComboBox();
        private readonly ComboBox brandCombo = new ComboBox();
        private readonly TextBox densityBox = new TextBox();
        private readonly TextBox quantityBox = new TextBox();
        private readonly Label[] parameterLabels = new Label[3];
        private readonly TextBox[] parameterBoxes = new TextBox[3];
        private readonly Label singleResult = new Label();
        private readonly Label totalResult = new Label();
        private readonly Label finishedResult = new Label();
        private readonly TextBox[] finishedBoxes = new TextBox[5];
        private readonly string[] finishedNames = { "单根长度 (m)", "根数", "米数 (m)", "米重 (t/m)", "吨数 (t)" };
        private readonly long[] finishedEdits = new long[5];
        private long editSequence;
        private bool updatingFinished;
        private FinishedPipeModel finishedModel = new FinishedPipeModel();
        private Label finishedContext;
        private Label finishedKgResult;
        private readonly List<Label> fieldLabels = new List<Label>();
        private readonly List<Label> hintLabels = new List<Label>();
        private readonly List<Button> mutedButtons = new List<Button>();
        private readonly List<Panel> dividerLines = new List<Panel>();
        private readonly List<Panel> cards = new List<Panel>();

        private Label titleLabel;
        private OctgMappingForm octgWindow;
        private Label resultContext;
        private Label resultTitleSingle;
        private Label resultTitleTotal;
        
        private Label authorVersionLabel;
        private Panel parameterGroup;
        private Button calculateButton;
        private ThemePalette currentTheme;
        private Color currentBrandColor;

        public MainForm()
        {
            SuspendLayout();
            uiFontFamily = ResolveUiFontFamily();
            Text = "规则材料重量计算器 - 企业版 " + AppVersion;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(1120, 860);
            ClientSize = new Size(1180, 850);
            DoubleBuffered = true;
            Font = UiFont(10.5f);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            BuildUi();
            BindExpressionHandlers();
            BindFinishedHandlers();
            BindData();
            ApplyTheme();
            UpdateDensityState();
            UpdateParameterFields();
            ResumeLayout(true);
        }

        private void BuildUi()
        {
            var shell = UiLayout.Table(1);
            shell.Padding = new Padding(24, 12, 24, 12);
            shell.RowCount = 4;
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(shell);

            var header = UiLayout.Table(2, true);
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            titleLabel = UiLayout.Label("规则材料重量计算器", 22, true);
            header.Controls.Add(titleLabel, 0, 0);
            var appearance = UiLayout.Table(4, true);
            appearance.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            appearance.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            appearance.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
            appearance.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            var themeLabel = UiLayout.Label("主题", 9, false);
            var brandLabel = UiLayout.Label("配色", 9, false);
            brandLabel.Padding = new Padding(10, 0, 0, 0);
            fieldLabels.Add(themeLabel); fieldLabels.Add(brandLabel);
            UiLayout.StyleInput(themeCombo); UiLayout.StyleInput(brandCombo);
            themeCombo.Font = UiFont(9); brandCombo.Font = UiFont(9);
            appearance.Controls.Add(themeLabel, 0, 0); appearance.Controls.Add(themeCombo, 1, 0);
            appearance.Controls.Add(brandLabel, 2, 0); appearance.Controls.Add(brandCombo, 3, 0);
            header.Controls.Add(appearance, 1, 0);
            shell.Controls.Add(header, 0, 0);

            var body = UiLayout.Table(2);
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            body.RowCount = 2;
            for (int row = 0; row < 2; row++)
            {
                var leftCard = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(20), Margin = new Padding(0, 0, 16, row == 0 ? 12 : 0) };
                var rightCard = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(20), Margin = new Padding(0, 0, 0, row == 0 ? 12 : 0) };
                body.Controls.Add(leftCard, 0, row); body.Controls.Add(rightCard, 1, row);
                cards.Add(leftCard); cards.Add(rightCard);
                if (row == 0) { BuildInputPanel(leftCard); BuildResultPanel(rightCard); }
                else { BuildFinishedPanel(leftCard); BuildFinishedResult(rightCard); }
            }
            shell.Controls.Add(body, 0, 1);
            var actions = UiLayout.Table(5, true);
            actions.Padding = new Padding(0, 12, 0, 4);
            foreach (int width in new[] { 180, 140, 120, 120 }) actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            calculateButton = UiLayout.Button("开始计算", Compute);
            calculateButton.Font = UiFont(11, FontStyle.Bold);
            var octg = UiLayout.Button("OCTG 映射", OpenOctg);
            var clear = UiLayout.Button("全部清空", ClearInput);
            var close = UiLayout.Button("退出", Close);
            mutedButtons.Add(octg); mutedButtons.Add(clear); mutedButtons.Add(close);
            actions.Controls.Add(calculateButton, 0, 0); actions.Controls.Add(octg, 1, 0);
            actions.Controls.Add(clear, 2, 0); actions.Controls.Add(close, 3, 0);
            var actionHint = UiLayout.Label("原料、成品可分别计算", 9, false);
            actionHint.TextAlign = ContentAlignment.MiddleRight;
            hintLabels.Add(actionHint); actions.Controls.Add(actionHint, 4, 0);
            shell.Controls.Add(actions, 0, 2);

            authorVersionLabel = UiLayout.Label("HopeAgent  " + AppVersion + "    ·    作者：" + AppAuthor, 9, false);
            shell.Controls.Add(authorVersionLabel, 0, 3);
        }

        private void BuildInputPanel(Panel panel)
        {
            var layout = UiLayout.Table(1);
            foreach (int height in new[] { 32, 100, 20, 78, 24 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);
            var heading = UiLayout.Label("原料参数", 13, true);
            fieldLabels.Add(heading); layout.Controls.Add(heading, 0, 0);
            var fields = UiLayout.Table(4);
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            string[] names = { "材料类型", "密度 g/cm³", "形状类型", "数量 件" };
            Control[] inputs = { materialCombo, densityBox, shapeCombo, quantityBox };
            for (int i = 0; i < inputs.Length; i++)
            {
                var label = UiLayout.Label(names[i], 9.5f, false);
                if (i % 2 == 1) label.Padding = new Padding(16, 0, 0, 0);
                fieldLabels.Add(label); UiLayout.StyleInput(inputs[i]);
                fields.Controls.Add(label, (i % 2) * 2, i / 2);
                fields.Controls.Add(inputs[i], (i % 2) * 2 + 1, i / 2);
            }
            layout.Controls.Add(fields, 0, 1);
            var geometry = UiLayout.Table(3);
            geometry.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            geometry.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            parameterGroup = geometry;
            for (int i = 0; i < 3; i++)
            {
                geometry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                parameterLabels[i] = UiLayout.Label("参数", 10, false);
                fieldLabels.Add(parameterLabels[i]);
                parameterBoxes[i] = new TextBox(); UiLayout.StyleInput(parameterBoxes[i]);
                parameterBoxes[i].Margin = new Padding(0, 4, i < 2 ? 16 : 0, 4);
                geometry.Controls.Add(parameterLabels[i], i, 0); geometry.Controls.Add(parameterBoxes[i], i, 1);
            }
            layout.Controls.Add(geometry, 0, 3);
            var note = UiLayout.Label("支持表达式：1000*2、(100+20)/2", 9, false);
            hintLabels.Add(note); layout.Controls.Add(note, 0, 4);
        }

        private void ConfigureResult(Label value)
        {
            value.Text = "0.00"; value.Font = UiFont(32, FontStyle.Bold);
            value.Dock = DockStyle.Fill; value.AutoSize = false; value.Margin = Padding.Empty;
            value.TextAlign = ContentAlignment.MiddleLeft;
            value.SizeChanged += delegate { FitResult(value); };
            value.TextChanged += delegate { FitResult(value); };
        }

        private void BuildResultPanel(Panel panel)
        {
            var layout = UiLayout.Table(1);
            foreach (int height in new[] { 34, 22, 58, 22, 58 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);
            var heading = UiLayout.Label("原料重量", 13, true);
            fieldLabels.Add(heading); layout.Controls.Add(heading, 0, 0);
            resultTitleSingle = UiLayout.Label("单件重量  /  kg", 9.5f, false);
            resultTitleTotal = UiLayout.Label("总重量  /  kg", 9.5f, false);
            layout.Controls.Add(resultTitleSingle, 0, 1); layout.Controls.Add(resultTitleTotal, 0, 3);
            ConfigureResult(singleResult); ConfigureResult(totalResult);
            layout.Controls.Add(singleResult, 0, 2); layout.Controls.Add(totalResult, 0, 4);
            resultContext = UiLayout.Label("填写原料参数后开始计算", 9, false);
            hintLabels.Add(resultContext); layout.Controls.Add(resultContext, 0, 5);
        }

        private void BuildFinishedPanel(Panel panel)
        {
            var layout = UiLayout.Table(1);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);
            var header = UiLayout.Table(2, true);
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            var heading = UiLayout.Label("成品参数", 13, true); fieldLabels.Add(heading);
            header.Controls.Add(heading, 0, 0);
            var reset = UiLayout.Button("清空成品", ClearFinished);
            reset.Font = UiFont(9); reset.Margin = new Padding(0, 0, 0, 2);
            mutedButtons.Add(reset); header.Controls.Add(reset, 1, 0);
            layout.Controls.Add(header, 0, 0);
            var fields = UiLayout.Table(2);
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
                var label = UiLayout.Label(finishedNames[i], 10, false); fieldLabels.Add(label);
                finishedBoxes[i] = new TextBox(); UiLayout.StyleInput(finishedBoxes[i]);
                fields.Controls.Add(label, 0, i); fields.Controls.Add(finishedBoxes[i], 1, i);
            }
            layout.Controls.Add(fields, 0, 1);
            var note = UiLayout.Label("回车联动 · 米数保留 1 位小数 · 反推根数向上取整", 9, false);
            hintLabels.Add(note); layout.Controls.Add(note, 0, 2);
        }

        private void BuildFinishedResult(Panel panel)
        {
            var layout = UiLayout.Table(1);
            foreach (int height in new[] { 34, 26, 78, 30 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);
            var heading = UiLayout.Label("成品重量", 13, true); fieldLabels.Add(heading);
            layout.Controls.Add(heading, 0, 0);
            var unit = UiLayout.Label("总重量  /  t", 9.5f, false); hintLabels.Add(unit);
            layout.Controls.Add(unit, 0, 1);
            ConfigureResult(finishedResult); finishedResult.Text = "—";
            layout.Controls.Add(finishedResult, 0, 2);
            finishedKgResult = UiLayout.Label("", 10, false); hintLabels.Add(finishedKgResult);
            layout.Controls.Add(finishedKgResult, 0, 3);
            finishedContext = UiLayout.Label("填写米数与米重，或吨数与米重", 9, false);
            hintLabels.Add(finishedContext); layout.Controls.Add(finishedContext, 0, 4);
        }

        private void FitResult(Label label)
        {
            if (label.Width < 10) return;
            float size = 32;
            Font candidate = UiFont(size, FontStyle.Bold);
            while (size > 9 && TextRenderer.MeasureText(label.Text, candidate).Width > label.Width - 4)
            {
                candidate.Dispose(); size -= 1; candidate = UiFont(size, FontStyle.Bold);
            }
            Font old = label.Font; label.Font = candidate; old.Dispose();
        }

        private void OpenOctg()
        {
            if (octgWindow == null || octgWindow.IsDisposed)
            {
                octgWindow = new OctgMappingForm();
                octgWindow.ApplyTheme(currentTheme, currentBrandColor);
                octgWindow.Show(this);
            }
            else
            {
                if (octgWindow.WindowState == FormWindowState.Minimized) octgWindow.WindowState = FormWindowState.Normal;
                octgWindow.Activate();
            }
        }

        private FontFamily ResolveUiFontFamily()
        {
            var preferred = new[] { "微软雅黑", "Microsoft YaHei", "Microsoft YaHei UI" };
            var installed = new InstalledFontCollection();
            foreach (var name in preferred)
            {
                var family = installed.Families.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                if (family != null)
                {
                    return family;
                }
            }
            return FontFamily.GenericSansSerif;
        }

        private Font UiFont(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(uiFontFamily, size, style, GraphicsUnit.Point);
        }

        private void BindExpressionHandlers()
        {
            HookExpressionInput(quantityBox, delegate { return "数量"; });
            HookExpressionInput(densityBox, delegate { return "密度"; });
            for (int i = 0; i < parameterBoxes.Length; i++)
            {
                int idx = i;
                HookExpressionInput(parameterBoxes[i], delegate { return parameterLabels[idx].Text; });
            }
        }

        private void HookExpressionInput(TextBox box, Func<string> fieldProvider)
        {
            box.Leave += delegate
            {
                NormalizeExpressionInBox(box, fieldProvider(), false);
            };
            box.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Enter)
                {
                    return;
                }
                e.SuppressKeyPress = true;
                e.Handled = true;
                bool ok = NormalizeExpressionInBox(box, fieldProvider(), true);
                if (ok)
                {
                    SelectNextControl(box, true, true, true, true);
                }
            };
        }

        private bool NormalizeExpressionInBox(TextBox box, string field, bool showWarning)
        {
            if (box.ReadOnly)
            {
                return true;
            }
            string raw = box.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }
            try
            {
                decimal value = ParseDecimal(raw, field);
                box.Text = value.ToString("0.######", CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                if (showWarning)
                {
                    MessageBox.Show(this, ex.Message, "输入有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        private ThemePalette BuildLightTheme()
        {
            return new ThemePalette
            {
                FormBack = Color.FromArgb(245, 247, 252),
                CardBack = Color.White,
                TitleFore = Color.FromArgb(23, 33, 56),
                SubFore = Color.FromArgb(98, 109, 133),
                FieldFore = Color.FromArgb(47, 57, 82),
                InputBack = Color.White,
                InputFore = Color.FromArgb(36, 43, 61),
                Border = Color.FromArgb(211, 220, 234),
                Divider = Color.FromArgb(226, 232, 242),
                MutedButtonBack = Color.FromArgb(243, 246, 251),
                MutedButtonFore = Color.FromArgb(52, 62, 87),
                ResultLabelFore = Color.FromArgb(98, 109, 133),
                ReadonlyBack = Color.FromArgb(247, 250, 255),
            };
        }

        private ThemePalette BuildDarkTheme()
        {
            return new ThemePalette
            {
                FormBack = Color.FromArgb(16, 20, 29),
                CardBack = Color.FromArgb(27, 32, 46),
                TitleFore = Color.FromArgb(236, 241, 251),
                SubFore = Color.FromArgb(172, 181, 205),
                FieldFore = Color.FromArgb(216, 223, 239),
                InputBack = Color.FromArgb(41, 49, 71),
                InputFore = Color.FromArgb(235, 240, 252),
                Border = Color.FromArgb(75, 85, 112),
                Divider = Color.FromArgb(73, 84, 111),
                MutedButtonBack = Color.FromArgb(53, 63, 90),
                MutedButtonFore = Color.FromArgb(223, 231, 246),
                ResultLabelFore = Color.FromArgb(177, 187, 212),
                ReadonlyBack = Color.FromArgb(49, 58, 82),
            };
        }

        private void BindData()
        {
            materialCombo.Items.AddRange(materialDensity.Keys.ToArray());
            shapeCombo.Items.AddRange(shapes.Select(x => x.Label).ToArray());
            themeCombo.Items.AddRange(new object[] { "浅色", "深色" });
            brandCombo.Items.AddRange(brandColors.Keys.ToArray());

            materialCombo.SelectedIndex = 0;
            shapeCombo.SelectedIndex = shapes.FindIndex(x => x.Key == "tube_od_t");
            themeCombo.SelectedIndex = 0;
            brandCombo.SelectedIndex = 0;

            quantityBox.Text = "1";
            densityBox.Text = materialDensity["碳钢"].ToString(CultureInfo.InvariantCulture);

            materialCombo.SelectedIndexChanged += delegate { UpdateDensityState(); };
            shapeCombo.SelectedIndexChanged += delegate { UpdateParameterFields(); };
            themeCombo.SelectedIndexChanged += delegate { ApplyTheme(); };
            brandCombo.SelectedIndexChanged += delegate { ApplyTheme(); };
        }

        private void ApplyTheme()
        {
            currentTheme = themeCombo.Text == "深色" ? BuildDarkTheme() : BuildLightTheme();
            currentBrandColor = brandColors.ContainsKey(brandCombo.Text) ? brandColors[brandCombo.Text] : Color.FromArgb(45, 140, 255);

            BackColor = currentTheme.FormBack;
            titleLabel.ForeColor = currentTheme.TitleFore;
            authorVersionLabel.ForeColor = currentTheme.SubFore;

            foreach (var card in cards)
            {
                card.BackColor = currentTheme.CardBack;
                ((SurfacePanel)card).BorderColor = currentTheme.Border;
                card.Invalidate();
            }

            foreach (var label in fieldLabels)
            {
                label.ForeColor = currentTheme.FieldFore;
            }

            foreach (var label in hintLabels)
            {
                label.ForeColor = currentTheme.SubFore;
            }

            resultTitleSingle.ForeColor = currentTheme.ResultLabelFore;
            resultTitleTotal.ForeColor = currentTheme.ResultLabelFore;
            singleResult.ForeColor = currentBrandColor;
            totalResult.ForeColor = currentBrandColor;
            finishedResult.ForeColor = currentBrandColor;

            foreach (var line in dividerLines)
            {
                line.BackColor = currentTheme.Divider;
            }

            parameterGroup.ForeColor = currentTheme.FieldFore;
            themeCombo.BackColor = currentTheme.InputBack;
            themeCombo.ForeColor = currentTheme.InputFore;
            brandCombo.BackColor = currentTheme.InputBack;
            brandCombo.ForeColor = currentTheme.InputFore;

            materialCombo.BackColor = currentTheme.InputBack;
            materialCombo.ForeColor = currentTheme.InputFore;
            shapeCombo.BackColor = currentTheme.InputBack;
            shapeCombo.ForeColor = currentTheme.InputFore;
            quantityBox.BackColor = currentTheme.InputBack;
            quantityBox.ForeColor = currentTheme.InputFore;

            foreach (var box in parameterBoxes.Concat(finishedBoxes))
            {
                box.BackColor = currentTheme.InputBack;
                box.ForeColor = currentTheme.InputFore;
            }

            calculateButton.BackColor = currentBrandColor;
            calculateButton.ForeColor = Color.White;

            foreach (var btn in mutedButtons)
            {
                btn.BackColor = currentTheme.MutedButtonBack;
                btn.ForeColor = currentTheme.MutedButtonFore;
                btn.FlatAppearance.BorderColor = currentTheme.Border;
            }

            UpdateDensityState();
            if (octgWindow != null && !octgWindow.IsDisposed) octgWindow.ApplyTheme(currentTheme, currentBrandColor);
        }

        private void UpdateDensityState()
        {
            string material = materialCombo.Text;
            if (string.IsNullOrWhiteSpace(material))
            {
                return;
            }

            if (material != "其它")
            {
                densityBox.ReadOnly = true;
                densityBox.BackColor = currentTheme == null ? Color.FromArgb(248, 250, 253) : currentTheme.ReadonlyBack;
                densityBox.ForeColor = currentTheme == null ? Color.FromArgb(36, 42, 58) : currentTheme.InputFore;
                densityBox.Text = materialDensity[material].ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                densityBox.ReadOnly = false;
                densityBox.BackColor = currentTheme == null ? Color.White : currentTheme.InputBack;
                densityBox.ForeColor = currentTheme == null ? Color.FromArgb(36, 42, 58) : currentTheme.InputFore;
                if (string.IsNullOrWhiteSpace(densityBox.Text))
                {
                    densityBox.Text = materialDensity["其它"].ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private void UpdateParameterFields()
        {
            var shape = shapes.FirstOrDefault(x => x.Label == shapeCombo.Text);
            if (shape == null)
            {
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                bool visible = i < shape.Fields.Length;
                parameterLabels[i].Visible = visible;
                parameterBoxes[i].Visible = visible;
                if (visible)
                {
                    parameterLabels[i].Text = shape.Fields[i];
                }
                else
                {
                    parameterBoxes[i].Text = string.Empty;
                }
            }
        }

        private decimal ParseDecimal(string value, string field)
        {
            try
            {
                return ParseMathInput(value);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(field + " 不是有效数字");
            }
        }

        private decimal ParseMathInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new InvalidOperationException("输入不能为空");
            }
            double value = ExpressionParser.Evaluate(input);
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidOperationException("表达式结果无效");
            }
            return Convert.ToDecimal(value);
        }

        private decimal Positive(decimal value, string field)
        {
            if (value <= 0)
            {
                throw new InvalidOperationException(field + " 必须大于 0");
            }
            return value;
        }

        private decimal CalculateSingle(string key, IList<decimal> p, decimal density)
        {
            Positive(density, "密度");
            decimal pi = (decimal)Math.PI;
            if (key == "rect_plate")
            {
                return Positive(p[0], "长度") * Positive(p[1], "宽度") * Positive(p[2], "厚度") * density / 1000000m;
            }
            if (key == "cylinder")
            {
                return pi * Positive(p[0], "直径") * Positive(p[0], "直径") * Positive(p[1], "长度") / 4m * density / 1000000m;
            }
            if (key == "tube_od_id")
            {
                return TubeOdId(p, density, pi);
            }
            if (key == "tube_od_t")
            {
                return TubeOdT(p, density, pi);
            }
            if (key == "square_bar")
            {
                return Positive(p[0], "边长") * Positive(p[0], "边长") * Positive(p[1], "长度") * density / 1000000m;
            }
            if (key == "cone")
            {
                return pi * Positive(p[0], "底半径") * Positive(p[0], "底半径") * Positive(p[1], "高度") * density / 3000000m;
            }
            if (key == "frustum")
            {
                return Frustum(p, density, pi);
            }
            if (key == "wire_by_mpm")
            {
                return Positive(p[0], "每米重") * Positive(p[1], "长度");
            }
            throw new InvalidOperationException("不支持的形状");
        }

        private decimal TubeOdId(IList<decimal> p, decimal density, decimal pi)
        {
            decimal od = Positive(p[0], "外径");
            decimal id = Positive(p[1], "内径");
            decimal length = Positive(p[2], "长度");
            if (id >= od)
            {
                throw new InvalidOperationException("内径必须小于外径");
            }
            return pi * (od * od - id * id) * length / 4m * density / 1000000m;
        }

        private decimal TubeOdT(IList<decimal> p, decimal density, decimal pi)
        {
            decimal od = Positive(p[0], "外径");
            decimal t = Positive(p[1], "壁厚");
            decimal length = Positive(p[2], "长度");
            decimal id = od - 2m * t;
            if (id <= 0)
            {
                throw new InvalidOperationException("壁厚过大，内径小于等于 0");
            }
            return pi * (od * od - id * id) * length / 4m * density / 1000000m;
        }

        private decimal Frustum(IList<decimal> p, decimal density, decimal pi)
        {
            decimal bigR = Positive(p[0], "大半径");
            decimal smallR = Positive(p[1], "小半径");
            decimal h = Positive(p[2], "高度");
            if (smallR >= bigR)
            {
                throw new InvalidOperationException("小半径必须小于大半径");
            }
            return pi * h * (bigR * bigR + bigR * smallR + smallR * smallR) * density / 3000000m;
        }

        private void ClearInput()
        {
            foreach (var box in parameterBoxes)
            {
                box.Text = string.Empty;
            }
            quantityBox.Text = "1";
            singleResult.Text = "0.00";
            totalResult.Text = "0.00";
            resultContext.Text = "填写原料参数后开始计算";
            ClearFinished();
        }

        private void Compute()
        {
            ComputeFinished();
            if (!parameterBoxes.Any(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                singleResult.Text = "0.00"; totalResult.Text = "0.00";
                resultContext.Text = "填写原料参数后开始计算";
                return;
            }
            try
            {
                var shape = shapes.First(x => x.Label == shapeCombo.Text);
                decimal density = ParseDecimal(densityBox.Text.Trim(), "密度");
                decimal quantity = Positive(ParseDecimal(quantityBox.Text.Trim(), "数量"), "数量");
                var parameters = new List<decimal>();
                for (int i = 0; i < shape.Fields.Length; i++)
                {
                    parameters.Add(ParseDecimal(parameterBoxes[i].Text.Trim(), shape.Fields[i]));
                }
                decimal single = CalculateSingle(shape.Key, parameters, density);
                decimal total = single * quantity;
                singleResult.Text = single.ToString("0.00", CultureInfo.InvariantCulture);
                totalResult.Text = total.ToString("0.00", CultureInfo.InvariantCulture);
                resultContext.Text = shape.Label + "  ·  " + quantity.ToString("0.######", CultureInfo.InvariantCulture) + " 件";
            }
            catch (Exception ex)
            {
                singleResult.Text = "—"; totalResult.Text = "—";
                resultContext.Text = ex.Message;
            }
        }

        private sealed class ExpressionParser
        {
            private readonly string text;
            private int pos;

            private ExpressionParser(string text)
            {
                this.text = text;
            }

            public static double Evaluate(string input)
            {
                var parser = new ExpressionParser(input);
                double result = parser.ParseExpression();
                parser.SkipSpaces();
                if (!parser.IsEnd())
                {
                    throw new InvalidOperationException("表达式格式错误");
                }
                return result;
            }

            private double ParseExpression()
            {
                double value = ParseTerm();
                while (true)
                {
                    SkipSpaces();
                    if (Match('+'))
                    {
                        value += ParseTerm();
                    }
                    else if (Match('-'))
                    {
                        value -= ParseTerm();
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private double ParseTerm()
            {
                double value = ParsePower();
                while (true)
                {
                    SkipSpaces();
                    if (Match('*'))
                    {
                        value *= ParsePower();
                    }
                    else if (Match('/'))
                    {
                        double denominator = ParsePower();
                        if (Math.Abs(denominator) < 1e-15)
                        {
                            throw new InvalidOperationException("除数不能为 0");
                        }
                        value /= denominator;
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private double ParsePower()
            {
                double baseValue = ParseUnary();
                SkipSpaces();
                if (Match('^'))
                {
                    double exponent = ParsePower();
                    return Math.Pow(baseValue, exponent);
                }
                return baseValue;
            }

            private double ParseUnary()
            {
                SkipSpaces();
                if (Match('+'))
                {
                    return ParseUnary();
                }
                if (Match('-'))
                {
                    return -ParseUnary();
                }
                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                SkipSpaces();
                if (Match('('))
                {
                    double value = ParseExpression();
                    SkipSpaces();
                    if (!Match(')'))
                    {
                        throw new InvalidOperationException("缺少右括号");
                    }
                    return value;
                }
                return ParseNumber();
            }

            private double ParseNumber()
            {
                SkipSpaces();
                int start = pos;
                bool hasDigit = false;
                while (!IsEnd())
                {
                    char ch = Peek();
                    if (char.IsDigit(ch))
                    {
                        hasDigit = true;
                        pos++;
                        continue;
                    }
                    if (ch == '.' || ch == ',')
                    {
                        pos++;
                        continue;
                    }
                    break;
                }
                if (!hasDigit)
                {
                    throw new InvalidOperationException("缺少数字");
                }
                string raw = text.Substring(start, pos - start).Replace(',', '.');
                double value;
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    throw new InvalidOperationException("数字格式错误");
                }
                return value;
            }

            private bool Match(char expected)
            {
                if (!IsEnd() && text[pos] == expected)
                {
                    pos++;
                    return true;
                }
                return false;
            }

            private char Peek()
            {
                return text[pos];
            }

            private bool IsEnd()
            {
                return pos >= text.Length;
            }

            private void SkipSpaces()
            {
                while (!IsEnd() && char.IsWhiteSpace(text[pos]))
                {
                    pos++;
                }
            }
        }
    }
}
