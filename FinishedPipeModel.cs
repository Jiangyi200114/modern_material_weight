using System;

namespace ModernMaterialWeight
{
    public enum FinishedField { Length, Count, Meters, UnitWeight, Tonnes }

    // Keep the direction of the last edit: rounded counts must never feed back into meters.
    public sealed class FinishedPipeModel
    {
        private enum Basis { None, LengthCount, LengthMeters, CountMeters }
        private Basis basis;
        private decimal?[] values = new decimal?[5];
        public bool ReverseMass { get; private set; }
        public decimal? this[FinishedField field] { get { return values[(int)field]; } }
        public FinishedPipeModel Copy()
        {
            var copy = (FinishedPipeModel)MemberwiseClone();
            copy.values = (decimal?[])values.Clone();
            return copy;
        }

        public void Edit(FinishedField field, decimal? value)
        {
            if (value.HasValue && value <= 0) throw new InvalidOperationException("成品参数必须大于 0");
            if (field == FinishedField.Count && value.HasValue && decimal.Truncate(value.Value) != value.Value)
                throw new InvalidOperationException("根数必须为正整数");
            values[(int)field] = value;
            if (field == FinishedField.Tonnes) ReverseMass = value.HasValue;
            if (value.HasValue && (field == FinishedField.Meters || (field == FinishedField.Count && Has(FinishedField.Length)))) ReverseMass = false;
            if (field == FinishedField.Count)
                basis = Has(FinishedField.Length) ? Basis.LengthCount : Basis.CountMeters;
            else if (field == FinishedField.Meters)
                basis = Has(FinishedField.Length) ? Basis.LengthMeters : Basis.CountMeters;
            else if (field == FinishedField.Length)
            {
                if (basis != Basis.LengthCount || !Has(FinishedField.Count))
                    basis = Has(FinishedField.Meters) ? Basis.LengthMeters : Basis.LengthCount;
            }
            Recalculate(false);
        }

        public void Recalculate(bool calculateMass)
        {
            if (ReverseMass && Has(FinishedField.Tonnes) && Has(FinishedField.UnitWeight))
            {
                if (Has(FinishedField.Tonnes) && Has(FinishedField.UnitWeight))
                {
                    SetMeters(Value(FinishedField.Tonnes) / Value(FinishedField.UnitWeight));
                    if (Has(FinishedField.Length))
                    {
                        values[1] = decimal.Ceiling(Value(FinishedField.Meters) / Value(FinishedField.Length));
                        basis = Basis.LengthMeters;
                    }
                    else if (Has(FinishedField.Count))
                    {
                        values[0] = Value(FinishedField.Meters) / Value(FinishedField.Count);
                        basis = Basis.CountMeters;
                    }
                }
                return;
            }

            if (!Has(FinishedField.Length) && Has(FinishedField.Count) && Has(FinishedField.Meters)) basis = Basis.CountMeters;
            else if (!Has(FinishedField.Count) && Has(FinishedField.Length) && Has(FinishedField.Meters)) basis = Basis.LengthMeters;
            else if (!Has(FinishedField.Meters) && Has(FinishedField.Length) && Has(FinishedField.Count)) basis = Basis.LengthCount;

            if (basis == Basis.LengthCount && Has(FinishedField.Length) && Has(FinishedField.Count))
                SetMeters(Value(FinishedField.Length) * Value(FinishedField.Count));
            else if (basis == Basis.LengthMeters && Has(FinishedField.Length) && Has(FinishedField.Meters))
                values[1] = decimal.Ceiling(Value(FinishedField.Meters) / Value(FinishedField.Length));
            else if (basis == Basis.CountMeters && Has(FinishedField.Count) && Has(FinishedField.Meters))
                values[0] = Value(FinishedField.Meters) / Value(FinishedField.Count);

            if (!ReverseMass)
                values[4] = calculateMass && Has(FinishedField.Meters) && Has(FinishedField.UnitWeight)
                    ? values[2] * values[3] : null;
        }

        private void SetMeters(decimal meters)
        {
            decimal rounded = decimal.Round(meters, 1, MidpointRounding.AwayFromZero);
            if (rounded <= 0) throw new InvalidOperationException("米数保留一位小数后为 0，请检查输入");
            values[2] = rounded;
        }
        private bool Has(FinishedField field) { return this[field].HasValue; }
        private decimal Value(FinishedField field) { return this[field].Value; }
    }
}
