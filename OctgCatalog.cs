using System;
using System.Collections.Generic;
using System.Linq;

namespace ModernMaterialWeight
{
    // Each end finish is a separate record. Null means the supplied table contains a dash.
    public sealed class OctgItem
    {
        public string OdInch { get; private set; }
        public decimal OdMm { get; private set; }
        public decimal Ppf { get; private set; }
        public decimal WtMm { get; private set; }
        public string Type { get; private set; }
        public string EndFinish { get; private set; }
        public decimal? KgPerMeter { get; private set; }
        public decimal? TonnesPerMeter { get { return KgPerMeter / 1000m; } }

        internal OctgItem(string od, decimal mm, decimal ppf, decimal wall, string type, string finish, decimal? kg)
        {
            OdInch = od; OdMm = mm; Ppf = ppf; WtMm = wall;
            Type = type; EndFinish = finish; KgPerMeter = kg;
        }
    }

    public static class OctgCatalog
    {
        public const string Plain = "不加厚";
        public const string Eu = "EU 外加厚";
        public const string Integral = "整体接头";
        public const string Casing = "T&C 套管";
        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<OctgItem> Items = Build().AsReadOnly();

        // Exact matches always win, including across end finishes and tubing/casing.
        // Retain the old 0.1 tolerance only as an explicitly labelled nearest-match fallback.
        public static List<OctgItem> Lookup(IEnumerable<OctgItem> source, decimal value, bool byWall, out bool approximate)
        {
            var rows = source.ToList();
            Func<OctgItem, decimal> distance = x => Math.Abs((byWall ? x.WtMm : x.Ppf) - value);
            var exact = rows.Where(x => distance(x) == 0m).ToList();
            approximate = false;
            if (exact.Count > 0 || rows.Count == 0) return exact;
            decimal nearest = rows.Min(distance);
            if (nearest > 0.1m) return new List<OctgItem>();
            approximate = true;
            return rows.Where(x => distance(x) == nearest).ToList();
        }

        private static List<OctgItem> Build()
        {
            var rows = new List<OctgItem>();
            // User reference image 4: OD, mm, wall, plain ppf/kg, EU ppf/kg, integral ppf/kg.
            Tube(rows, "1.050", 26.67m, 2.87m, 1.14m, 1.70m, 1.20m, 1.79m);
            Tube(rows, "1.050", 26.67m, 3.91m, 1.48m, 2.20m, 1.54m, 2.29m);
            Tube(rows, "1.315", 33.40m, 3.38m, 1.70m, 2.53m, 1.80m, 2.68m, 1.72m, 2.56m);
            Tube(rows, "1.315", 33.40m, 4.55m, 2.19m, 3.26m, 2.24m, 3.33m);
            Tube(rows, "1.660", 42.16m, 3.18m, 2.09m, null, null, null, 2.10m, 3.13m);
            Tube(rows, "1.660", 42.16m, 3.56m, 2.30m, 3.42m, 2.40m, 3.57m, 2.33m, 3.47m);
            Tube(rows, "1.660", 42.16m, 4.85m, 3.03m, 4.51m, 3.07m, 4.57m);
            Tube(rows, "1.900", 48.26m, 3.18m, 2.40m, null, null, null, 2.40m, 3.57m);
            Tube(rows, "1.900", 48.26m, 3.68m, 2.75m, 4.09m, 2.90m, 4.32m, 2.76m, 4.11m);
            Tube(rows, "1.900", 48.26m, 5.08m, 3.65m, 5.43m, 3.73m, 5.55m);
            Tube(rows, "1.900", 48.26m, 6.35m, 4.42m, 6.58m);
            Tube(rows, "1.900", 48.26m, 7.62m, 5.15m, 7.66m);
            Tube(rows, "2.063", 52.40m, 3.96m, 3.24m, null, null, null, 3.25m, 4.84m);
            Tube(rows, "2.063", 52.40m, 5.72m, 4.50m, null);
            Tube(rows, "2-3/8", 60.32m, 4.24m, 4.00m, 5.95m);
            Tube(rows, "2-3/8", 60.32m, 4.83m, 4.60m, 6.85m, 4.70m, 6.99m);
            Tube(rows, "2-3/8", 60.32m, 6.45m, 5.80m, 8.63m, 5.95m, 8.85m);
            Tube(rows, "2-3/8", 60.32m, 7.49m, 6.60m, 9.82m);
            Tube(rows, "2-3/8", 60.32m, 8.53m, 7.35m, 10.94m, 7.45m, 11.09m);
            Tube(rows, "2-7/8", 73.02m, 5.51m, 6.40m, 9.52m, 6.50m, 9.67m);
            Tube(rows, "2-7/8", 73.02m, 7.01m, 7.80m, 11.61m, 7.90m, 11.76m);
            Tube(rows, "2-7/8", 73.02m, 7.82m, 8.60m, 12.80m, 8.70m, 12.95m);
            Tube(rows, "2-7/8", 73.02m, 8.64m, 9.35m, 13.91m, 9.45m, 14.06m);
            Tube(rows, "2-7/8", 73.02m, 9.96m, 10.50m, 15.63m);
            Tube(rows, "2-7/8", 73.02m, 11.18m, 11.50m, 17.11m);
            // User reference image 5.
            Tube(rows, "3-1/2", 88.90m, 5.49m, 7.70m, 11.46m);
            Tube(rows, "3-1/2", 88.90m, 6.45m, 9.20m, 13.69m, 9.30m, 13.84m);
            Tube(rows, "3-1/2", 88.90m, 7.34m, 10.20m, 15.18m);
            Tube(rows, "3-1/2", 88.90m, 9.52m, 12.70m, 18.90m, 12.95m, 19.27m);
            Tube(rows, "3-1/2", 88.90m, 10.92m, 14.30m, 21.28m);
            Tube(rows, "3-1/2", 88.90m, 12.09m, 15.50m, 23.07m);
            Tube(rows, "3-1/2", 88.90m, 13.46m, 17.00m, 25.30m);
            Tube(rows, "4", 101.60m, 5.74m, 9.50m, 14.14m);
            Tube(rows, "4", 101.60m, 6.65m, 10.70m, null, 11.00m, 16.37m);
            Tube(rows, "4", 101.60m, 8.38m, 13.20m, 19.64m);
            Tube(rows, "4", 101.60m, 10.54m, 16.10m, 23.96m);
            Tube(rows, "4", 101.60m, 12.70m, 18.90m, 28.13m);
            Tube(rows, "4", 101.60m, 15.49m, 22.20m, 33.04m);
            Tube(rows, "4-1/2", 114.30m, 6.88m, 12.60m, 18.75m, 12.75m, 18.97m);
            Tube(rows, "4-1/2", 114.30m, 8.56m, 15.20m, 22.62m);
            Tube(rows, "4-1/2", 114.30m, 9.65m, 17.00m, 25.30m);
            Tube(rows, "4-1/2", 114.30m, 10.92m, 18.90m, 28.13m);
            Tube(rows, "4-1/2", 114.30m, 12.70m, 21.50m, 32.00m);
            Tube(rows, "4-1/2", 114.30m, 14.22m, 23.70m, 35.27m);
            Tube(rows, "4-1/2", 114.30m, 16.00m, 26.10m, 38.84m);
            // User reference image 6. Casing masses are transcribed, NOT ppf conversions.
            Case(rows, "4-1/2", 114.30m, 9.50m, 14.38m, 5.21m);
            Case(rows, "4-1/2", 114.30m, 10.50m, 15.73m, 5.69m);
            Case(rows, "4-1/2", 114.30m, 11.60m, 17.38m, 6.35m);
            Case(rows, "4-1/2", 114.30m, 13.50m, 19.87m, 7.37m);
            Case(rows, "4-1/2", 114.30m, 15.10m, 22.69m, 8.56m);
            Case(rows, "5", 127.00m, 11.50m, 17.19m, 5.59m);
            Case(rows, "5", 127.00m, 13.00m, 19.69m, 6.43m);
            Case(rows, "5", 127.00m, 15.00m, 22.69m, 7.52m);
            Case(rows, "5", 127.00m, 18.00m, 27.19m, 9.19m);
            Case(rows, "5", 127.00m, 21.40m, 32.13m, 11.10m);
            Case(rows, "5", 127.00m, 23.20m, 34.76m, 12.14m);
            Case(rows, "5", 127.00m, 24.10m, 36.15m, 12.70m);
            Case(rows, "5-1/2", 139.70m, 14.00m, 20.91m, 6.20m);
            Case(rows, "5-1/2", 139.70m, 15.50m, 23.48m, 6.98m);
            Case(rows, "5-1/2", 139.70m, 17.00m, 25.72m, 7.72m);
            Case(rows, "5-1/2", 139.70m, 20.00m, 30.05m, 9.17m);
            Case(rows, "5-1/2", 139.70m, 23.00m, 34.05m, 10.54m);
            Case(rows, "5-1/2", 139.70m, 26.80m, 40.15m, 12.70m);
            Case(rows, "5-1/2", 139.70m, 29.70m, 44.47m, 14.27m);
            Case(rows, "5-1/2", 139.70m, 32.60m, 48.74m, 15.88m);
            Case(rows, "5-1/2", 139.70m, 35.30m, 52.80m, 17.45m);
            Case(rows, "5-1/2", 139.70m, 38.00m, 56.82m, 19.05m);
            Case(rows, "5-1/2", 139.70m, 40.50m, 60.64m, 20.62m);
            Case(rows, "5-1/2", 139.70m, 43.10m, 64.41m, 22.22m);
            return rows;
        }

        private static void Tube(List<OctgItem> rows, string od, decimal mm, decimal wall, decimal ppf, decimal? kg,
            decimal? euPpf = null, decimal? euKg = null, decimal? integralPpf = null, decimal? integralKg = null)
        {
            rows.Add(new OctgItem(od, mm, ppf, wall, "油管", Plain, kg));
            if (euPpf.HasValue) rows.Add(new OctgItem(od, mm, euPpf.Value, wall, "油管", Eu, euKg));
            if (integralPpf.HasValue) rows.Add(new OctgItem(od, mm, integralPpf.Value, wall, "油管", Integral, integralKg));
        }

        private static void Case(List<OctgItem> rows, string od, decimal mm, decimal ppf, decimal kg, decimal wall)
        {
            rows.Add(new OctgItem(od, mm, ppf, wall, "套管", Casing, kg));
        }
    }
}
