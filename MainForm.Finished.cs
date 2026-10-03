using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ModernMaterialWeight
{
    public sealed partial class MainForm
    {
        private void BindFinishedHandlers()
        {
            for (int i = 0; i < finishedBoxes.Length; i++)
            {
                int index = i;
                finishedBoxes[i].TextChanged += delegate
                {
                    if (updatingFinished) return;
                    finishedEdits[index] = ++editSequence;
                    finishedResult.Text = "—"; finishedKgResult.Text = "";
                    finishedContext.Text = "参数已更新，点击开始计算";
                };
                finishedBoxes[i].Leave += delegate { CommitFinishedInputs(); };
                finishedBoxes[i].KeyDown += delegate(object sender, KeyEventArgs e)
                {
                    if (e.KeyCode != Keys.Enter) return;
                    e.SuppressKeyPress = true; e.Handled = true;
                    if (CommitFinishedInputs()) SelectNextControl(finishedBoxes[index], true, true, true, true);
                };
            }
        }

        private bool CommitFinishedInputs()
        {
            if (updatingFinished || finishedEdits.All(x => x == 0)) return true;
            try
            {
                var pending = Enumerable.Range(0, 5).Where(i => finishedEdits[i] != 0).OrderBy(i => finishedEdits[i]).ToArray();
                var parsed = new decimal?[5];
                foreach (int i in pending)
                {
                    string text = finishedBoxes[i].Text.Trim();
                    if (text.Length > 0) parsed[i] = ParseDecimal(text, finishedNames[i]);
                }
                var next = finishedModel.Copy();
                foreach (int i in pending) next.Edit((FinishedField)i, parsed[i]);
                finishedModel = next;
                Array.Clear(finishedEdits, 0, finishedEdits.Length);
                ShowFinishedInputs();
                finishedContext.Text = finishedModel.ReverseMass
                    ? "按吨数反推米数；根数向上取整" : "按米数 × 米重计算成品吨数";
                return true;
            }
            catch (Exception ex)
            {
                finishedResult.Text = "—"; finishedKgResult.Text = "";
                finishedContext.Text = ex is OverflowException ? "成品数值过大，请调整输入" : ex.Message;
                return false;
            }
        }

        private void ShowFinishedInputs()
        {
            updatingFinished = true;
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    decimal? value = finishedModel[(FinishedField)i];
                    string format = i == 1 ? "0" : i == 2 ? "0.0###########################" : "0.############################";
                    finishedBoxes[i].Text = value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "";
                }
            }
            finally { updatingFinished = false; }
        }

        private void ComputeFinished()
        {
            if (!CommitFinishedInputs()) return;
            if (finishedBoxes.All(x => string.IsNullOrWhiteSpace(x.Text))) return;
            try
            {
                var next = finishedModel.Copy();
                next.Recalculate(true);
                if (!next[FinishedField.Meters].HasValue || !next[FinishedField.UnitWeight].HasValue || !next[FinishedField.Tonnes].HasValue)
                    throw new InvalidOperationException("请填写米数与米重，或吨数与米重");
                decimal tonnes = next[FinishedField.Tonnes].Value;
                decimal kg = tonnes * 1000m;
                finishedModel = next;
                ShowFinishedInputs();
                finishedResult.Text = tonnes.ToString("0.############################", CultureInfo.InvariantCulture);
                finishedKgResult.Text = kg.ToString("0.############################", CultureInfo.InvariantCulture) + " kg";
                finishedContext.Text = next[FinishedField.Meters].Value.ToString("0.0#####", CultureInfo.InvariantCulture) + " m"
                    + (next[FinishedField.Count].HasValue ? " · " + next[FinishedField.Count].Value.ToString("0", CultureInfo.InvariantCulture) + " 根" : "")
                    + (next.ReverseMass ? "\n按输入吨数反推米数" : "\n米数 × 米重");
            }
            catch (Exception ex)
            {
                finishedResult.Text = "—"; finishedKgResult.Text = "";
                finishedContext.Text = ex is OverflowException ? "成品数值过大，请调整输入" : ex.Message;
            }
        }

        private void ClearFinished()
        {
            finishedModel = new FinishedPipeModel();
            Array.Clear(finishedEdits, 0, finishedEdits.Length);
            ShowFinishedInputs();
            finishedResult.Text = "—"; finishedKgResult.Text = "";
            finishedContext.Text = "填写米数与米重，或吨数与米重";
        }
    }
}
