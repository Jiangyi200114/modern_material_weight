using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ModernMaterialWeight;

internal static class SmokeTests
{
    private static int checks;
    private static string imagePrefix = "";
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    private static object Field(object owner, string name) { return owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner); }
    private static object Call(object owner, string name, params object[] args)
    {
        return owner.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(owner, args);
    }
    private static void Check(bool ok, string message)
    {
        checks++;
        if (!ok) throw new Exception(message);
    }
    private static List<OctgItem> Lookup(string od, decimal value, bool wall)
    {
        bool approximate;
        var result = OctgCatalog.Lookup(OctgCatalog.Items.Where(x => x.OdInch == od), value, wall, out approximate);
        Check(!approximate, "Expected exact match: " + od + " / " + value);
        return result;
    }
    private static void Mass(string od, decimal ppf, string finish, decimal kg)
    {
        var item = Lookup(od, ppf, false).Single(x => x.EndFinish == finish);
        Check(item.KgPerMeter == kg && item.TonnesPerMeter == kg / 1000m, "Incorrect mass: " + od + " / " + ppf);
    }
    private static void Throws(Action action, string name)
    {
        bool thrown = false;
        try { action(); } catch (TargetInvocationException) { thrown = true; }
        Check(thrown, "Expected input rejection: " + name);
    }
    private static void Save(Form form, string filename)
    {
        form.PerformLayout(); Application.DoEvents();
        using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine("tests", imagePrefix + filename));
        }
    }

    private static void FinishedModelChecks()
    {
        // All six input orders for the three possible pairs.
        foreach (bool reverse in new[] { false, true })
        {
            var pairs = new[] {
                new[] { FinishedField.Length, FinishedField.Count, FinishedField.Meters },
                new[] { FinishedField.Length, FinishedField.Meters, FinishedField.Count },
                new[] { FinishedField.Count, FinishedField.Meters, FinishedField.Length }
            };
            decimal[] data = { 12m, 9m, 100m };
            foreach (var pair in pairs)
            {
                var model = new FinishedPipeModel();
                int a = reverse ? 1 : 0, b = reverse ? 0 : 1;
                model.Edit(pair[a], data[(int)pair[a]]); model.Edit(pair[b], data[(int)pair[b]]);
                decimal expected = pair[2] == FinishedField.Meters ? 108m : pair[2] == FinishedField.Count ? 9m : 100m / 9m;
                Check(model[pair[2]] == expected, "Any two geometry inputs, either order");
            }
        }
        var forward = new FinishedPipeModel();
        forward.Edit(FinishedField.Length, 12m); forward.Edit(FinishedField.Meters, 100m);
        forward.Edit(FinishedField.UnitWeight, 0.01384m); forward.Recalculate(true);
        Check(forward[FinishedField.Count] == 9m && forward[FinishedField.Meters] == 100m && forward[FinishedField.Tonnes] == 1.384m,
            "Ceiling count must not inflate meters or tonnes");
        forward.Recalculate(true);
        Check(forward[FinishedField.Tonnes] == 1.384m, "Repeated calculation must be stable");
        forward.Edit(FinishedField.Count, 10m); forward.Recalculate(true);
        Check(forward[FinishedField.Meters] == 120m && forward[FinishedField.Tonnes] == 1.6608m, "Editing count switches to length times count");
        forward.Edit(FinishedField.Length, 12.345m);
        Check(forward[FinishedField.Meters] == 123.5m, "Meters round to one decimal, away from zero");
        foreach (bool reverse in new[] { false, true })
        {
            var model = new FinishedPipeModel();
            model.Edit(FinishedField.Length, 12m);
            model.Edit(reverse ? FinishedField.Tonnes : FinishedField.UnitWeight, reverse ? 1m : 0.01384m);
            model.Edit(reverse ? FinishedField.UnitWeight : FinishedField.Tonnes, reverse ? 0.01384m : 1m);
            model.Recalculate(true);
            Check(model[FinishedField.Meters] == 72.3m && model[FinishedField.Count] == 7m && model[FinishedField.Tonnes] == 1m,
                "Reverse tonnes, preserve entered mass");
            model.Edit(FinishedField.Length, 10m);
            Check(model[FinishedField.Count] == 8m && model[FinishedField.Meters] == 72.3m, "Change length in reverse mode");
            model.Edit(FinishedField.UnitWeight, .02m);
            Check(model[FinishedField.Meters] == 50m && model[FinishedField.Count] == 5m, "Change unit weight in reverse mode");
        }
        var withCount = new FinishedPipeModel();
        withCount.Edit(FinishedField.Tonnes, 2m); withCount.Edit(FinishedField.UnitWeight, .02m);
        withCount.Edit(FinishedField.Count, 10m);
        Check(withCount[FinishedField.Length] == 10m && withCount[FinishedField.Meters] == 100m, "Reverse mass plus count derives length");
        var direct = new FinishedPipeModel();
        direct.Edit(FinishedField.Meters, 123.4m); direct.Edit(FinishedField.UnitWeight, .01384m); direct.Recalculate(true);
        Check(direct[FinishedField.Tonnes] == 1.707856m, "Direct meters-only weight calculation");
        foreach (decimal invalid in new[] { 0m, -1m, 1.5m })
        {
            bool rejected = false;
            try { new FinishedPipeModel().Edit(FinishedField.Count, invalid); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Reject zero, negative and fractional counts");
        }
    }

    private static void FinishedUiChecks(MainForm main)
    {
        var boxes = (TextBox[])Field(main, "finishedBoxes");
        Action<int, string> edit = delegate(int index, string value)
        {
            boxes[index].Text = value;
            Check((bool)Call(main, "CommitFinishedInputs"), "Commit finished edit");
        };
        edit(0, "12"); edit(1, "10");
        Check(boxes[2].Text == "120.0", "UI length and count derive meters");
        edit(3, "0.01384");
        ((Button)Field(main, "calculateButton")).PerformClick();
        Check(boxes[4].Text == "1.6608" && ((Label)Field(main, "finishedResult")).Text == "1.6608", "Finished field and right result agree");
        Check(((Label)Field(main, "totalResult")).Text == "314.00", "Raw result remains independent");
        edit(2, "100");
        Check(boxes[1].Text == "9", "UI count rounds upward");
        ((Button)Field(main, "calculateButton")).PerformClick();
        Check(boxes[4].Text == "1.384" && boxes[2].Text == "100.0", "No meter feedback after rounding count");
        Save(main, "finished-forward.png");
        edit(4, "1");
        Check(boxes[2].Text == "72.3" && boxes[1].Text == "7", "UI reverse tonnes to meters and count");
        ((Button)Field(main, "calculateButton")).PerformClick();
        Check(boxes[4].Text == "1" && ((Label)Field(main, "finishedResult")).Text == "1", "Keep reverse input tonnes");
        Save(main, "finished-reverse.png");
        boxes[3].Text = "bad";
        Check(!(bool)Call(main, "CommitFinishedInputs") && ((Label)Field(main, "finishedResult")).Text == "—", "Invalid finished input clears stale result");
        Call(main, "ClearFinished");
        Check(boxes.All(x => x.Text == ""), "Clear only finished block");
        edit(2, "100+20"); edit(3, "0.01384");
        foreach (var box in (TextBox[])Field(main, "parameterBoxes")) box.Clear();
        ((Button)Field(main, "calculateButton")).PerformClick();
        Check(boxes[4].Text == "1.6608", "Finished-only calculation with empty raw inputs");
        ((TextBox[])Field(main, "parameterBoxes"))[0].Text = "bad";
        ((Button)Field(main, "calculateButton")).PerformClick();
        Check(boxes[4].Text == "1.6608" && ((Label)Field(main, "totalResult")).Text == "—", "Invalid raw input does not block finished calculation");
        var parameters = (TextBox[])Field(main, "parameterBoxes");
        parameters[0].Text = "2000"; parameters[1].Text = "1000"; parameters[2].Text = "10";
        ((Button)Field(main, "calculateButton")).PerformClick();
    }

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--dpi96")) { SetThreadDpiAwarenessContext(new IntPtr(-1)); imagePrefix = "dpi96-"; }
        FinishedModelChecks();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Mass("2-3/8", 4.6m, OctgCatalog.Plain, 6.85m);
        Mass("2-3/8", 4.7m, OctgCatalog.Eu, 6.99m);
        Mass("2-7/8", 6.4m, OctgCatalog.Plain, 9.52m);
        Mass("2-7/8", 6.5m, OctgCatalog.Eu, 9.67m);
        Mass("3-1/2", 12.95m, OctgCatalog.Eu, 19.27m);
        Mass("4", 11m, OctgCatalog.Eu, 16.37m);
        Mass("4-1/2", 12.6m, OctgCatalog.Plain, 18.75m);
        Mass("4-1/2", 12.75m, OctgCatalog.Eu, 18.97m);
        Mass("4-1/2", 9.5m, OctgCatalog.Casing, 14.38m);
        Mass("4-1/2", 13.5m, OctgCatalog.Casing, 19.87m);
        Mass("5", 21.4m, OctgCatalog.Casing, 32.13m);
        Mass("5-1/2", 43.1m, OctgCatalog.Casing, 64.41m);
        Mass("1.315", 1.72m, OctgCatalog.Integral, 2.56m);
        Check(Lookup("2-3/8", 4.6m, false).Count == 1, "Plain ppf must not match neighboring EU");
        var alternatives = Lookup("4-1/2", 8.56m, true);
        Check(alternatives.Count == 2 && alternatives.Select(x => x.KgPerMeter).Distinct().Count() == 2, "Same wall: retain tubing AND casing");
        alternatives = Lookup("2-3/8", 4.83m, true);
        Check(alternatives.Count == 2 && alternatives.Any(x => x.EndFinish == OctgCatalog.Eu), "Wall lookup must include EU");
        Check(Lookup("3-1/2", 5.49m, true).All(x => x.EndFinish != OctgCatalog.Eu), "Dash must not invent an EU option");
        Check(Lookup("4", 10.7m, false).Single().KgPerMeter == null, "Missing plain mass must not be synthesized");
        bool approximate;
        var nearby = OctgCatalog.Lookup(OctgCatalog.Items.Where(x => x.OdInch == "2-3/8"), 4.65m, false, out approximate);
        Check(approximate && nearby.Count == 2, "Equal nearest matches must be explicit alternatives");
        Check(OctgCatalog.Lookup(OctgCatalog.Items, 999m, false, out approximate).Count == 0 && !approximate, "Unknown ppf");
        Check(OctgCatalog.Items.All(x => x.EndFinish != OctgCatalog.Eu || x.KgPerMeter.HasValue), "All EU records require reference mass");
        Check(OctgCatalog.Items.GroupBy(x => x.OdInch + "/" + x.Type + "/" + x.EndFinish + "/" + x.Ppf).All(x => x.Count() == 1), "Unique catalog keys");
        Check(OctgCatalog.Items.Count(x => x.Type == "套管") == 24, "All 24 reference casing rows");
        Check(OctgCatalog.Items.Count(x => x.Type == "油管" && x.EndFinish == OctgCatalog.Plain) == 45, "All 45 reference tubing rows");
        foreach (var item in OctgCatalog.Items)
        {
            Check(item.WtMm > 0 && item.OdMm > 2 * item.WtMm && item.Ppf > 0, "Catalog geometry");
            Check(!item.KgPerMeter.HasValue || item.KgPerMeter > 0, "Positive reference mass");
        }

        using (var main = new MainForm())
        {
            // Compare every existing geometry and expression with the original executable.
            var oldAssembly = Assembly.LoadFile(Path.GetFullPath("backup_v2.0.1/HopeAgent.exe"));
            using (var original = (Form)Activator.CreateInstance(oldAssembly.GetType("ModernMaterialWeight.MainForm")))
            {
                var cases = new Dictionary<string, decimal[]> {
                    { "rect_plate", new[] { 1000m, 1000m, 10m } },
                    { "cylinder", new[] { 100m, 1000m } },
                    { "tube_od_id", new[] { 100m, 80m, 1000m } },
                    { "tube_od_t", new[] { 100m, 10m, 1000m } },
                    { "square_bar", new[] { 100m, 1000m } },
                    { "cone", new[] { 50m, 1000m } },
                    { "frustum", new[] { 50m, 20m, 1000m } },
                    { "wire_by_mpm", new[] { 12.3m, 100m } }
                };
                foreach (var sample in cases)
                    foreach (decimal density in new[] { 7.85m, 2.7m, 8.9m, 1.234m })
                        Check((decimal)Call(main, "CalculateSingle", sample.Key, sample.Value, density) ==
                              (decimal)Call(original, "CalculateSingle", sample.Key, sample.Value, density), "Calculation regression " + sample.Key);
                foreach (string expression in new[] { "1+2*3", "2^3", "(100+20)/2", "1*0.5", "1*2^2", "-2+4", "2^3^2", "1,5+2" })
                    Check((decimal)Call(main, "ParseMathInput", expression) == (decimal)Call(original, "ParseMathInput", expression), "Expression regression");
                Throws(delegate { Call(main, "ParseMathInput", "1/0"); }, "division by zero");
                Throws(delegate { Call(main, "ParseMathInput", "1+"); }, "incomplete expression");
                Throws(delegate { Call(main, "CalculateSingle", "tube_od_t", new decimal[] { 100, 50, 1000 }, 7.85m); }, "invalid wall");
                Throws(delegate { Call(main, "CalculateSingle", "tube_od_id", new decimal[] { 100, 100, 1000 }, 7.85m); }, "invalid bore");
                Throws(delegate { Call(main, "CalculateSingle", "frustum", new decimal[] { 20, 50, 1000 }, 7.85m); }, "invalid frustum");
            }

            main.StartPosition = FormStartPosition.Manual; main.Location = new Point(-20000, -20000);
            main.Show(); Application.DoEvents();
            Call(main, "OpenOctg"); Application.DoEvents();
            var octg = (Form)Field(main, "octgWindow");
            Check(!octg.Modal && main.Enabled, "OCTG must be modeless");
            Call(main, "OpenOctg");
            Check(ReferenceEquals(octg, Field(main, "octgWindow")), "Reuse existing OCTG window");
            Check(((ComboBox)Field(main, "shapeCombo")).Text == "外径×壁厚管材", "Default tube shape");
            Check(((ComboBox)Field(octg, "_odCombo")).Text == "3-1/2" && ((TextBox)Field(octg, "_odMmBox")).Text == "88.90", "Default OCTG diameter");
            ((ComboBox)Field(main, "shapeCombo")).SelectedIndex = 0;
            var parameters = (TextBox[])Field(main, "parameterBoxes");
            parameters[0].Text = "1000*2"; parameters[1].Text = "1000"; parameters[2].Text = "10";
            ((TextBox)Field(main, "quantityBox")).Text = "2";
            ((Button)Field(main, "calculateButton")).PerformClick();
            Check(((Label)Field(main, "singleResult")).Text == "157.00" && ((Label)Field(main, "totalResult")).Text == "314.00", "Calculate with OCTG still open");
            FinishedUiChecks(main);
            Save(main, "main-light.png");
            var cb = (Button)Field(main, "calculateButton");
            Check(cb.Bottom <= cb.Parent.ClientSize.Height, "Calculate button must fit its row");
            Check(cb.Width > TextRenderer.MeasureText(cb.Text, cb.Font).Width, "Calculate button label fits");

            var od = (ComboBox)Field(octg, "_odCombo");
            var ppf = (TextBox)Field(octg, "_ppfBox");
            var wall = (TextBox)Field(octg, "_wtBox");
            var grid = (DataGridView)Field(octg, "_grid");
            od.SelectedItem = "4-1/2";
            ppf.Text = "12.60"; Call(octg, "Lookup", false);
            Check(((TextBox)Field(octg, "_kgValue")).Text == "18.75" && ((TextBox)Field(octg, "_tonValue")).Text == "0.01875", "UI mass fields");
            Check(wall.Text == "6.88", "UI wall mapping");
            var massBox = (TextBox)Field(octg, "_tonValue");
            massBox.SelectAll();
            Check(massBox.ReadOnly && massBox.ShortcutsEnabled && massBox.SelectedText == "0.01875", "Selectable, copyable, read-only mass");
            wall.Text = "6.88"; Call(octg, "Lookup", true);
            Check(grid.Rows.Count == 2 && grid.SelectedRows.Count == 0, "Multiple end finishes must require selection");
            grid.CurrentCell = grid.Rows[1].Cells[0]; grid.Rows[1].Selected = true;
            Check(((TextBox)Field(octg, "_kgValue")).Text == "18.97", "EU row selection");
            Save(octg, "octg-light.png");
            wall.Text = "8.56"; Call(octg, "Lookup", true);
            Check(grid.Rows.Count == 2 && ((TextBox)Field(octg, "_kgValue")).Text == "—", "Do not silently select casing/tubing");
            Save(octg, "octg-alternatives.png");
            ppf.Text = "999"; Call(octg, "Lookup", false);
            Check(grid.Rows.Count == 0 && ((TextBox)Field(octg, "_kgValue")).Text == "—", "Clear stale results on failed lookup");
            ppf.Text = "bad"; Call(octg, "Lookup", false);
            Check(((Label)Field(octg, "_statusLabel")).Text.Contains("正数"), "Invalid lookup error");
            od.SelectedItem = "2-3/8"; wall.Text = "4.83"; Call(octg, "Lookup", true);
            grid.CurrentCell = grid.Rows[1].Cells[0]; grid.Rows[1].Selected = true;
            ((ComboBox)Field(main, "themeCombo")).SelectedItem = "深色";
            ((ComboBox)Field(main, "brandCombo")).SelectedItem = "科技青";
            Save(main, "main-dark.png"); Save(octg, "octg-dark.png");
            Check(octg.BackColor == main.BackColor, "Child theme follows main");
            main.Size = main.MinimumSize;
            Save(main, "main-minimum.png");
            octg.Size = octg.MinimumSize;
            Save(octg, "octg-minimum.png");
            ((ComboBox)Field(main, "shapeCombo")).SelectedIndex = 7;
            Check(!parameters[2].Visible, "Two-parameter shape visibility");
            ((ComboBox)Field(main, "materialCombo")).SelectedItem = "其它";
            Check(!((TextBox)Field(main, "densityBox")).ReadOnly, "Custom density");
            Call(main, "ClearInput");
            Check(parameters.All(x => x.Text == "") && ((TextBox)Field(main, "quantityBox")).Text == "1", "Clear inputs");
            octg.Close(); Call(main, "OpenOctg");
            var reopened = (Form)Field(main, "octgWindow");
            Check(!ReferenceEquals(octg, reopened) && !reopened.IsDisposed, "Reopen closed window");
            main.Close(); Application.DoEvents();
            Check(reopened.IsDisposed, "Owned child closes with main");
        }
        Console.WriteLine("PASS: " + checks + " checks; catalog records=" + OctgCatalog.Items.Count);
    }
}
