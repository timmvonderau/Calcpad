﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Calcpad.Core
{
    internal class Unit : IEquatable<Unit>
    {
        private string _text = string.Empty;
        private int _hashCode;
        private readonly float[] _powers;
        private readonly double[] _factors;

        private static bool _isUs;
        private static readonly string[] Names = { "g",  "m",  "s",  "A",  "°C",  "mol", "cd"};
        private static readonly Dictionary<string, Unit> Units;
        private static readonly Unit[] ForceUnits = new Unit[9];
        internal static bool IsUs
        {
            get => _isUs;
            set
            {
                _isUs = value;
                if (value)
                {
                    Units["therm"] = Units["therm_US"];
                    Units["cwt"] = Units["cwt_US"];
                    Units["ton"] = Units["ton_US"];
                    Units["fl_oz"] = Units["fl_oz_US"];
                    Units["gi"] = Units["gi_US"];
                    Units["pt"] = Units["pt_US"];
                    Units["qt"] = Units["qt_US"];
                    Units["gal"] = Units["gal_US"];
                    Units["bbl"] = Units["bbl_US"];
                    Units["bu"] = Units["bu_US"];
                    Units["tonf"] = Units["tonf_US"];
                }
                else
                {
                    Units["therm"] = Units["therm_UK"];
                    Units["cwt"] = Units["cwt_UK"];
                    Units["ton"] = Units["ton_UK"];
                    Units["fl_oz"] = Units["fl_oz_UK"];
                    Units["gi"] = Units["gi_UK"];
                    Units["pt"] = Units["pt_UK"];
                    Units["qt"] = Units["qt_UK"];
                    Units["gal"] = Units["gal_UK"];
                    Units["bbl"] = Units["bbl_UK"];
                    Units["bu"] = Units["bu_UK"];
                    Units["tonf"] = Units["tonf_UK"];
                }
            }
        }

        internal bool IsForce => _powers.Length > 2 && _powers[0] == 1f && _powers[2] == -2f && Text.Contains("s");

        internal bool IsTemp => _powers.Length == 5 && 
                                  _powers[4] == 1f &&
                                  _powers[0] == 0f &&
                                  _powers[1] == 0f &&
                                  _powers[2] == 0f &&
                                  _powers[3] == 0f;

        internal string Text
        {
            get
            {
                if (string.IsNullOrEmpty(_text))
                    _text = GetText(OutputWriter.OutputFormat.Text);

                return _text;
            }
            set => _text = value;
        }

        internal string Html
        {
            get
            {
                if (string.IsNullOrEmpty(_text))
                    return GetText(OutputWriter.OutputFormat.Html);

                OutputWriter writer = new HtmlWriter();
                return writer.FormatUnitsText(_text);
            }
        }

        internal string Xml
        {
            get
            {
                if (string.IsNullOrEmpty(_text))
                    return GetText(OutputWriter.OutputFormat.Xml);

                OutputWriter writer = new XmlWriter();
                return writer.FormatUnitsText(_text);
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                var hash = new HashCode();
                for (var i = 0; i < _powers.Length; i++)
                {
                    hash.Add(_powers[i]);
                    hash.Add(_factors[i]);
                }
                _hashCode = hash.ToHashCode();
            }
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is Unit u)
                return Equals(u);

            return false;
        }


        public bool Equals(Unit other)
        {
            if (other is null)
                return false;

            if (_powers.Length != other._powers.Length)
                return false;

            for (var i = 0; i < _powers.Length; ++i)
            {
                if (_powers[i] != other._powers[i] || _factors[i] != other._factors[i])
                    return false;
            }
            return true;
        }

        internal Unit(int n)
        {
            _powers = new float[n];
            _factors = new double[n];
            for (int i = 0; i < n; i++)
                _factors[i] = i == 0 ? 1000.0 : 1.0;
        }

        internal Unit(string text) : this(Units[text]) { }
        internal Unit(
            string text, 
            float mass,
            float length = 0f,
            float time = 0f,
            float current = 0f,
            float temp = 0f,
            float substance = 0f,
            float luminosity = 0f
        )
        {
            _text = text;
            int n;
            if (luminosity != 0f)
                n = 7;
            else if (substance != 0f)
                n = 6;
            else if (temp != 0f)
                n = 5;
            else if (current != 0f)
                n = 4;
            else if (time != 0f)
                n = 3;
            else if (length != 0f)
                n = 2;
            else
                n = 1;

            _factors = new double[n];
            for (int i = 0; i < n; i++)
                _factors[i] = i == 0 ? 1000.0 : 1.0;

            _powers = new float[n];
            if (n > 0)
            {
                _powers[0] = mass;
                if (n > 1)
                {
                    _powers[1] = length;
                    if (n > 2)
                    { 
                        _powers[2] = time;
                        if (n > 3)
                        {
                            _powers[3] = current;
                            if (n > 4)
                            {
                                _powers[4] = temp;
                                if (n > 5)
                                {
                                    _powers[5] = substance;
                                    if (n > 6)
                                        _powers[6] = luminosity;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal Unit(Unit u)
        {
            _text = u._text;
            _hashCode = u._hashCode;
            int n = u._powers.Length;
            _powers = new float[n];
            _factors = new double[n];
            Array.Copy(u._powers, _powers, n);
            Array.Copy(u._factors, _factors, n);
        }

        private static string TemperatureToDelta(string s)
        {
            return s[0] switch
            {
                '°' => "Δ°" + s[1],
                'K' => "Δ°C",
                'R' => "Δ°F",
                _ => s
            };
        }

        internal static bool Exists(string unit) => Units.ContainsKey(unit);
        public override string ToString() => _text;
        internal static string GetText(Unit u) => IsNullOrEmpty(u) ? "unitless" : u.Text;

        static Unit()
        {
            var kg = new Unit("kg", 1).Scale("g", 0.001);
            var m = new Unit("m", 0, 1);
            var mi = m.Scale("mi", 1609.344);
            var a = m.Pow(2).Scale("a", 100);
            var L = m.Shift(-1).Pow(3);
            L.Text = "L";
            var s = new Unit("s", 0f, 0f, 1f);
            var h = s.Scale("h", 3600.0);
            var A = new Unit("A", 0f, 0f, 0f, 1f);
            var N = new Unit("N", 1f, 1f, -2f);
            var Nm = new Unit("Nm", 1f, 2f, -2f);
            var Hz = new Unit("Hz", 0f, 0f, -1f);
            var Pa = new Unit("Pa", 1f, -1f, -2f);
            var J = new Unit("J", 1f, 2f, -2f);
            var W = new Unit("W", 1f, 2f, -3f);
            var C = new Unit("C", 0f, 0f, 1f, 1f);
            var V = new Unit("V", 1f, 2f, -3f, -1f);
            var F = new Unit("F", -1f, -2f, 4f, 2f);
            var Ohm = new Unit("Ω", 1f, 2f, -3f, -2f);
            var S = new Unit("S", -1f, -2f, 3f, 2f);
            var Wb = new Unit(new Unit("Wb", 1f, 2f, -2f, -1f));
            var T = new Unit(new Unit("T", 1f, 0f, -2f, -1f));
            var H = new Unit(new Unit("H", 1f, 2f, -2f, -2f));
            var Bq = new Unit("Bq", 0f, 0f, -1f);
            var Gy = new Unit("Gy", 0f, 2f, -2f);
            var Sv = new Unit("Sv", 0f, 2f, -2f);

            ForceUnits[4] = N.Shift(3);
            ForceUnits[0] = ForceUnits[4] * m.Pow(-4);
            ForceUnits[0].Text = "kN/m^4";
            ForceUnits[1] = ForceUnits[4] * m.Pow(-3);
            ForceUnits[1].Text = "kN/m^3";
            ForceUnits[2] = ForceUnits[4] * m.Pow(-2);
            ForceUnits[2].Text = "kPa";
            ForceUnits[3] = ForceUnits[4] * m.Pow(-1);
            ForceUnits[3].Text = "kN/m";
            ForceUnits[5] = ForceUnits[4] * m;
            ForceUnits[5].Text = "kN·m";
            ForceUnits[6] = ForceUnits[4] * m.Pow(2);
            ForceUnits[6].Text = "kN·m^2";
            ForceUnits[7] = ForceUnits[4] * m.Pow(3);
            ForceUnits[7].Text = "kN·m^3";
            ForceUnits[8] = ForceUnits[4] * m.Pow(4);
            ForceUnits[8].Text = "kN·m^4";

            Units = new Dictionary<string, Unit>()
            {
                {string.Empty, null},
                {"g",  kg},
                {"hg", kg.Shift(2)},
                {"kg", kg.Shift(3)},
                {"t",  kg.Scale("t", 1000000.0)},
                {"kt", kg.Scale("kt", 1000000000.0)},
                {"Mt", kg.Scale("Mt", 1000000000000.0)},
                {"Gt", kg.Scale("Gt", 1E+15)},
                {"dg", kg.Shift(-1)},
                {"cg", kg.Shift(-2)},
                {"mg", kg.Shift(-3)},
                {"μg", kg.Shift(-6)},
                {"ng", kg.Shift(-9)},
                {"pg", kg.Shift(-12)},
                {"Da", kg.Scale("Da", 1.6605390666050505e-27)},
                {"u", kg.Scale("u", 1.6605390666050505e-27)},

                {"gr", kg.Scale("gr", 0.06479891)},
                {"dr", kg.Scale("dr", 1.7718451953125)},
                {"oz", kg.Scale("oz", 28.349523125)},
                {"lb", kg.Scale("lb", 453.59237)},
                {"kip",kg.Scale("kip", 453592.37)},
                {"st", kg.Scale("st", 6350.29318)},
                {"qr", kg.Scale("qr", 12700.58636)},
                {"cwt_US",kg.Scale("cwt_US", 45359.237 )},
                {"cwt_UK",kg.Scale("cwt_UK", 50802.34544)},
                {"ton_US",kg.Scale("ton_US", 907184.74)},
                {"ton_UK",kg.Scale("ton_UK", 1016046.9088)},
                {"slug", kg.Scale("slug", 14593.90294)},

                {"m",  m},
                {"km", m.Shift(3)},
                {"dm", m.Shift(-1)},
                {"cm", m.Shift(-2)},
                {"mm", m.Shift(-3)},
                {"μm", m.Shift(-6)},
                {"nm", m.Shift(-9)},
                {"pm", m.Shift(-12)},
                {"AU", m.Scale("AU", 149597870700.0)},
                {"ly", m.Scale("ly", 9460730472580800.0)},

                {"th", m.Scale("th", 2.54E-05)},
                {"in", m.Scale("in", 0.0254)},
                {"ft", m.Scale("ft", 0.3048)},
                {"yd", m.Scale("yd", 0.9144)},
                {"ch", m.Scale("ch", 20.1168)},
                {"fur", m.Scale("fur", 201.168)},
                {"mi", mi},
                {"ftm", m.Scale("ftm", 1.852)},
                {"cable", m.Scale("cable", 185.2)},
                {"nmi", m.Scale("nmi", 1852)},
                {"li", m.Scale("li", 0.201168)},
                {"rod", m.Scale("rod", 5.0292)},
                {"pole", m.Scale("pole", 5.0292)},
                {"perch", m.Scale("perch", 5.0292)},
                {"lea", m.Scale("lea", 4828.032)},

                {"a",  a},
                {"daa",a.Scale("daa", 10.0)},
                {"ha", a.Scale("ha", 100.0)},
                {"L",  L},
                {"dL", L.Scale("dL", 0.1)},
                {"cL", L.Scale("cL", 0.01)},
                {"mL", L.Scale("mL", 0.001)},
                {"hL", L.Scale("hL", 100.0)},

                {"rood",m.Pow(2).Scale("rood", 1011.7141056)},
                {"ac",  m.Pow(2).Scale("ac", 4046.8564224)},
                {"fl_oz_US", L.Scale("fl_oz_US",  0.0295735295625 )},
                {"fl_oz_UK", L.Scale("fl_oz_UK", 0.0284130625)},
                {"gi_US",  L.Scale("gi_US", 0.11829411825)},
                {"gi_UK",  L.Scale("gi_UK",  0.1420653125)},
                {"pt_US",  L.Scale("pt_US", 0.473176473)},
                {"pt_UK",  L.Scale("pt_UK", 0.56826125)},
                {"qt_US",  L.Scale("qt_US", 0.946352946)},
                {"qt_UK",  L.Scale("qt_UK", 1.1365225)},
                {"gal_US", L.Scale("gal_US", 3.785411784)},
                {"gal_UK", L.Scale("gal_UK", 4.54609)},
                {"bbl_US", L.Scale("bbl_US", 119.240471196)},
                {"bbl_UK", L.Scale("bbl_UK", 163.65924)},
                {"bu_US", L.Scale("bu_US", 35.2390704) },
                {"bu_UK", L.Scale("bu_UK", 36.36872) },

                {"s",  s},
                {"ms", s.Shift(-3)},
                {"μs", s.Shift(-6)},
                {"ns", s.Shift(-9)},
                {"ps", s.Shift(-12)},
                {"min",s.Scale("min", 60.0)},
                {"h",  h},
                {"d",  h.Scale("d", 24)},
                {"kmh", (m.Shift(3) / h).Scale("kmh", 1)},
                {"mph", (mi / h).Scale("mph", 1)},
                {"Hz", Hz},
                {"kHz", Hz.Shift(3)},
                {"MHz", Hz.Shift(6)},
                {"GHz", Hz.Shift(9)},
                {"THz", Hz.Shift(12)},
                {"mHz", Hz.Shift(-3)},
                {"μHz", Hz.Shift(-6)},
                {"nHz", Hz.Shift(-9)},
                {"pHz", Hz.Shift(-12)},
                {"rpm", Hz.Scale("rpm", 1.0 / 60.0)},

                {"A", A},
                {"kA", A.Shift(3)},
                {"MA", A.Shift(6)},
                {"GA", A.Shift(9)},
                {"TA", A.Shift(12)},
                {"mA", A.Shift(-3)},
                {"μA", A.Shift(-6)},
                {"nA", A.Shift(-9)},
                {"pA", A.Shift(-12)},
                {"Ah", A * h},
                {"mAh", A.Shift(-3) * h},

                {"°C", new Unit("°C",   0f, 0f, 0f, 0f, 1f)},
                {"Δ°C", new Unit("Δ°C", 0f, 0f, 0f, 0f, 1f)},
                {"K", new Unit("K",     0f, 0f, 0f, 0f, 1f)},
                {"°F", new Unit("°F",   0f, 0f, 0f, 0f, 1f).Scale("°F", 5.0 / 9.0)},
                {"Δ°F", new Unit("Δ°F", 0f, 0f, 0f, 0f, 1f).Scale("Δ°F", 5.0 / 9.0)},
                {"°R", new Unit("°R",   0f, 0f, 0f, 0f, 1f).Scale("°R", 5.0 / 9.0)},

                {"mol", new Unit("mol", 0f, 0f, 0f, 0f, 0f, 1f)},
                {"cd", new Unit("cd",   0f, 0f, 0f, 0f, 0f, 0f, 1f)},

                {"N",  N},
                {"daN", N.Shift(1)},
                {"hN", N.Shift(2)},
                {"kN", N.Shift(3)},
                {"MN", N.Shift(6)},
                {"GN", N.Shift(9)},
                {"TN", N.Shift(12)},
                {"Nm", Nm},
                {"kNm", Nm.Shift(3)},

                {"kgf",  N.Scale("kgf", 9.80665)},
                {"tf",   N.Scale("tf", 9806.65)},
                {"dyn", N.Scale("dyn", 1e-5)},

                {"ozf",  N.Scale("ozf", 0.278013851)},
                {"lbf",  N.Scale("lbf", 4.4482216153)},
                {"kipf", N.Scale("kipf", 4448.2216153)},
                {"tonf_US", N.Scale("tonf_US", 8896.443230521)},
                {"tonf_UK", N.Scale("tonf_UK", 9964.01641818352)},
                {"pdl",  N.Scale("pdl", 0.138254954376)},

                {"Pa",   Pa},
                {"daPa", Pa.Shift(1)},
                {"hPa",  Pa.Shift(2)},
                {"kPa",  Pa.Shift(3)},
                {"MPa",  Pa.Shift(6)},
                {"GPa",  Pa.Shift(9)},
                {"TPa",  Pa.Shift(12)},
                {"dPa", Pa.Shift(-1)},
                {"cPa", Pa.Shift(-2)},
                {"mPa", Pa.Shift(-3)},
                {"μPa", Pa.Shift(-6)},
                {"nPa", Pa.Shift(-9)},
                {"pPa", Pa.Shift(-12)},
                {"bar",  Pa.Scale("bar", 100000.0)},
                {"mbar", Pa.Scale("mbar", 100.0)},
                {"μbar", Pa.Scale("μbar", 0.1)},
                {"atm",  Pa.Scale("atm", 101325.0)},
                {"mmHg",  Pa.Scale("mmHg", 133.322387415)},

                {"at",   Pa.Scale("at", 98066.5)},
                {"Torr", Pa.Scale("Torr", 133.32236842)},
                {"osi",  Pa.Scale("osi", 430.922330894662)},
                {"osf",  Pa.Scale("osf", 2.99251618676848)},
                {"psi",  Pa.Scale("psi", 6894.75729322959)},
                {"ksi",  Pa.Scale("ksi", 6894757.29322959)},
                {"tsi",  Pa.Scale("tsi", 15444256.3366971)},
                {"psf",  Pa.Scale("psf", 47.880258980761)},
                {"ksf",  Pa.Scale("ksf", 47880.258980761)},
                {"tsf",  Pa.Scale("tsf", 107251.780115952)},
                {"inHg", Pa.Scale("inHg", 3386.389)},

                {"J", J},
                {"kJ", J.Shift(3)},
                {"MJ", J.Shift(6)},
                {"GJ", J.Shift(9)},
                {"TJ", J.Shift(12)},
                {"mJ", J.Shift(-3)},
                {"μJ", J.Shift(-6)},
                {"nJ", J.Shift(-9)},
                {"pJ", J.Shift(-12)},
                {"Wh", J.Scale("Wh", 3600.0)},
                {"kWh", J.Scale("kWh", 3600000.0)},
                {"MWh", J.Scale("MWh", 3600000000.0)},
                {"GWh", J.Scale("GWh", 3600000000000.0)},
                {"TWh", J.Scale("TWh", 3.6E+15)},

                {"erg", J.Scale("erg", 1e-7)},
                {"eV",  J.Scale("eV",  1.6021773300241367e-19)},
                {"keV", J.Scale("keV", 1.6021773300241367e-16)},
                {"MeV", J.Scale("MeV", 1.6021773300241367e-13)},
                {"GeV", J.Scale("GeV", 1.6021773300241367e-10)},
                {"TeV", J.Scale("TeV", 1.6021773300241367e-7)},
                {"PeV", J.Scale("PeV", 1.6021773300241367e-4)},
                {"EeV", J.Scale("EeV", 1.6021773300241367e-1)},
                {"BTU", J.Scale("BTU", 1055.05585262)},
                {"therm_US", J.Scale("therm_US", 1054.804e+5)},
                {"therm_UK", J.Scale("therm_UK", 1055.05585262e+5)},
                {"quad", J.Scale("quad", 1055.05585262e+15)},
                {"cal", J.Scale("cal", 4.1868)},
                {"kcal", J.Scale("kcal", 4186.8)},

                {"W", W},
                {"kW", W.Shift(3)},
                {"MW", W.Shift(6)},
                {"GW", W.Shift(9)},
                {"TW", W.Shift(12)},
                {"mW", W.Shift(-3)},
                {"μW", W.Shift(-6)},
                {"nW", W.Shift(-9)},
                {"pW", W.Shift(-12)},

                {"hp", W.Scale("hp", 745.69987158227022)},
                {"hp_M", W.Scale("hp_M", 735.49875)},
                {"ks", W.Scale("ks", 735.49875)},
                {"hp_E", W.Scale("hp_E", 746)},
                {"hp_S", W.Scale("hp_S", 9812.5)},

                {"C", C},
                {"kC", C.Shift(3)},
                {"MC", C.Shift(6)},
                {"GC", C.Shift(9)},
                {"TC", C.Shift(12)},
                {"mC", C.Shift(-3)},
                {"μC", C.Shift(-6)},
                {"nC", C.Shift(-9)},
                {"pC", C.Shift(-12)},

                {"V", V},
                {"kV", V.Shift(3)},
                {"MV", V.Shift(6)},
                {"GV", V.Shift(9)},
                {"TV", V.Shift(12)},
                {"mV", V.Shift(-3)},
                {"μV", V.Shift(-6)},
                {"nV", V.Shift(-9)},
                {"pV", V.Shift(-12)},

                {"F", F},
                {"kF", F.Shift(3)},
                {"MF", F.Shift(6)},
                {"GF", F.Shift(9)},
                {"TF", F.Shift(12)},
                {"mF", F.Shift(-3)},
                {"μF", F.Shift(-6)},
                {"nF", F.Shift(-9)},
                {"pF", F.Shift(-12)},

                {"Ω", Ohm},
                {"kΩ", Ohm.Shift(3)},
                {"MΩ", Ohm.Shift(6)},
                {"GΩ", Ohm.Shift(9)},
                {"TΩ", Ohm.Shift(12)},
                {"mΩ", Ohm.Shift(-3)},
                {"μΩ", Ohm.Shift(-6)},
                {"nΩ", Ohm.Shift(-9)},
                {"pΩ", Ohm.Shift(-12)},

                {"S", S},
                {"kS", S.Shift(3)},
                {"MS", S.Shift(6)},
                {"GS", S.Shift(9)},
                {"TS", S.Shift(12)},
                {"mS", S.Shift(-3)},
                {"μS", S.Shift(-6)},
                {"nS", S.Shift(-9)},
                {"pS", S.Shift(-12)},

                {"Wb", Wb},
                {"kWb", Wb.Shift(3)},
                {"MWb", Wb.Shift(6)},
                {"GWb", Wb.Shift(9)},
                {"TWb", Wb.Shift(12)},
                {"mWb", Wb.Shift(-3)},
                {"μWb", Wb.Shift(-6)},
                {"nWb", Wb.Shift(-9)},
                {"pWb", Wb.Shift(-12)},

                {"T", T},
                {"kT", T.Shift(3)},
                {"MT", T.Shift(6)},
                {"GT", T.Shift(9)},
                {"TT", T.Shift(12)},
                {"mT", T.Shift(-3)},
                {"μT", T.Shift(-6)},
                {"nT", T.Shift(-9)},
                {"pT", T.Shift(-12)},

                {"H", H},
                {"kH", H.Shift(3)},
                {"MH", H.Shift(6)},
                {"GH", H.Shift(9)},
                {"TH", H.Shift(12)},
                {"mH", H.Shift(-3)},
                {"μH", H.Shift(-6)},
                {"nH", H.Shift(-9)},
                {"pH", H.Shift(-12)},

                {"Bq", Bq},
                {"kBq", Bq.Shift(3)},
                {"MBq", Bq.Shift(6)},
                {"GBq", Bq.Shift(9)},
                {"TBq", Bq.Shift(12)},
                {"mBq", Bq.Shift(-3)},
                {"μBq", Bq.Shift(-6)},
                {"nBq", Bq.Shift(-9)},
                {"pBq", Bq.Shift(-12)},
                {"Ci",  Bq.Scale("Ci", 3.7e+10)},
                {"Rd",  Bq.Scale("Rd", 1e+6)},

                {"Gy", Gy},
                {"kGy", Gy.Shift(3)},
                {"MGy", Gy.Shift(6)},
                {"GGy", Gy.Shift(9)},
                {"TGy", Gy.Shift(12)},
                {"mGy", Gy.Shift(-3)},
                {"μGy", Gy.Shift(-6)},
                {"nGy", Gy.Shift(-9)},
                {"pGy", Gy.Shift(-12)},

                {"Sv", Sv},
                {"kSv", Sv.Shift(3)},
                {"MSv", Sv.Shift(6)},
                {"GSv", Sv.Shift(9)},
                {"TSv", Sv.Shift(12)},
                {"mSv", Sv.Shift(-3)},
                {"μSv", Sv.Shift(-6)},
                {"nSv", Sv.Shift(-9)},
                {"pSv", Sv.Shift(-12)},

                {"lm", new Unit("lm", 0, 0, 0, 0, 0, 0, 1)},
                {"lx", new Unit("lx", 0, -2, 0, 0, 0, 0, 1)},
                {"kat", new Unit("kat", 0, 0, -1, 0, 0, 1)}
            };

            Units.Add("therm", Units["therm_UK"]);
            Units.Add("cwt", Units["cwt_UK"]);
            Units.Add("ton", Units["ton_UK"]);
            Units.Add("fl_oz", Units["fl_oz_UK"]);
            Units.Add("gi", Units["gi_UK"]);
            Units.Add("pt", Units["pt_UK"]);
            Units.Add("qt", Units["qt_UK"]);
            Units.Add("gal", Units["gal_UK"]);
            Units.Add("bbl", Units["bbl_UK"]);
            Units.Add("bu", Units["bu_UK"]);
            Units.Add("tonf", Units["tonf_UK"]);
        }

        internal void Scale(double factor)
        {
            for (var i = 0; i < _powers.Length; ++i)
            {
                if (_powers[i] != 0f)
                {
                    _factors[i] *= Math.Pow(factor, 1.0 / _powers[i]);
                    break;
                }
            }
        }

        internal Unit Shift(int n)
        {
            var s = GetPrefix(n) + _text;
            var unit = new Unit(this)
            {
                _text = s
            };
            unit.Scale(GetScale(n));
            return unit;
        }

        internal Unit Scale(string s, double factor)
        {
            var unit = new Unit(this)
            {
                _text = s
            };
            unit.Scale(factor);
            return unit;
        }

        private string GetText(OutputWriter.OutputFormat format)
        {
            OutputWriter writer = format switch
            {
                OutputWriter.OutputFormat.Html => new HtmlWriter(),
                OutputWriter.OutputFormat.Xml => new XmlWriter(),
                _ => new TextWriter()
            };
            var stringBuilder = new StringBuilder();
            var isFirst = true;
            for (var i = 0; i < _powers.Length; i++)
            {
                if (_powers[i] != 0f)
                {
                    var p = isFirst ? _powers[i] : Math.Abs(_powers[i]);
                    var s = GetDimText(writer, Names[i], _factors[i] , p);
                    if (i == 4 && stringBuilder.Length > 0)
                        s = TemperatureToDelta(s);

                    if (isFirst)
                        isFirst = false;
                    else
                    {
                        var oper = _powers[i] > 0f ? '·' : '/';
                        if (format == OutputWriter.OutputFormat.Xml)
                            stringBuilder.Append($"<m:r><m:t>{oper}</m:t></m:r>");
                        else
                            stringBuilder.Append(oper);
                    }
                    stringBuilder.Append(s);
                }
            }
            return stringBuilder.ToString();
        }

        public static Unit operator *(Unit u, double d)
        {
            var unit = new Unit(u);
            for (var i = 0; i < unit._powers.Length; i++)
            {
                ref float p = ref unit._powers[i];
                if (p != 0f)
                {
                    unit._factors[i] *= Math.Pow(d, 1.0 / p);
                    break;
                }
            }
            return unit;
        }

        public static Unit operator *(double d, Unit u) => u * d;

        public static Unit operator *(Unit u1, Unit u2) => MultiplyOrDivide(u1, u2);

        public static Unit operator /(Unit u1, Unit u2) => MultiplyOrDivide(u1, u2, -1f);

        private static Unit MultiplyOrDivide(Unit u1, Unit u2, float k = 1f)
        {
            var n1 = u1._powers.Length;
            var n2 = u2._powers.Length;
            var n = n1 > n2 ? n1 : n2;
            var size = n;
            while (size > 0)
            {
                var i = size - 1;
                var p1 = i < n1 ? u1._powers[i] : 0f;
                var p2 = i < n2 ? -k * u2._powers[i] : 0f;
                if (p1 != p2)
                    break;

                size = i;
            }
            Unit unit = new(size);
            for (var i = 0; i < n; i++)
            {
                var p1 = i < n1 ? u1._powers[i] : 0f;
                var p2 = i < n2 ? k * u2._powers[i] : 0f;
                if (i < size)
                {
                    unit._factors[i] = p1 == 0f ? u2._factors[i] : u1._factors[i];
                    unit._powers[i] = p1 + p2;
                }
            }
            return unit;
        }

        public static double GetProductOrDivideFactor(Unit u1, Unit u2, bool divide = false)
        {
            var n1 = u1._powers.Length;
            var n2 = u2._powers.Length;
            var n = n1 > n2 ? n1 : n2;
            var k = divide ? -1d : 1d;
            var factor = 1.0;
            for (var i = 0; i < n; i++)
            {
                var p1 = i < n1 ? u1._powers[i] : 0d;
                var p2 = i < n2 ? k * u2._powers[i] : 0d;
                if (p1 != 0 && p2 != 0)
                {
                    if (k == 1)
                        factor *= Math.Pow(u2._factors[i] / u1._factors[i], p2);
                    else
                        factor /= Math.Pow(u2._factors[i] / u1._factors[i], -p2);
                }
            }
            return factor;
        }


        public static Unit operator /(Unit u, double d) => u * (1.0 / d);

        public static Unit operator /(double d, Unit u) => d * u.Pow(-1.0);

        internal Unit Pow(double x)
        {
            float xf = (float)x;
            Unit unit = new(_powers.Length);
            for (var i = 0; i < _powers.Length; i++)
            {
                unit._factors[i] = _factors[i];
                unit._powers[i] = _powers[i] * xf;
            }
            return unit;
        }

        internal static bool IsConsistent(Unit u1, Unit u2)
        {
            if (IsNullOrEmpty(u1))
                return IsNullOrEmpty(u2);

            if (IsNullOrEmpty(u2))
                return false;

            int n = u1._powers.Length;
            if (u2._powers.Length != n)
                return false;

            for (var i = 0; i < n; ++i)
            {
                if (u1._powers[i] != u2._powers[i])
                    return false;
            }
            return true;
        }

        internal static bool IsMultiple(Unit u1, Unit u2)
        {
            if (IsNullOrEmpty(u1))
                return IsNullOrEmpty(u2);

            if (IsNullOrEmpty(u2))
                return false;

            int n = u1._powers.Length;
            if (u2._powers.Length != n)
                return false;

            double? d1 = null;
            for (var i = 0; i < n; ++i)
            {
                ref float p1 = ref u1._powers[i];
                ref float p2 = ref u2._powers[i];
                if (p1 != p2)
                {
                    if (p1 == 0f || p2 == 0f)
                        return false;

                    if (!d1.HasValue)
                        d1 = p2 - p1;
                    else
                    {
                        var d2 = p2 - p1;
                        if (d1.Value != d2)
                            return false;
                    }
                }
            }
            return true;
        }

        private bool IsEmpty() => _powers.Length == 0;

        internal static bool IsNullOrEmpty(Unit u) => u is null || u.IsEmpty();

        internal double ConvertTo(Unit u)
        {
            var d = 1.0;
            for (var i = 0; i < _powers.Length; i++)
            {
                ref float p = ref _powers[i];
                if (p != 0f)
                    d *= Math.Pow(_factors[i] / u._factors[i], p);
            }
            return d;
        }

        internal static Unit GetForceUnit(Unit u)
        {
            var i = (int)u._powers[1] + 3;
            if (i < 0 || i > 5)
                return null;

            return ForceUnits[i];
        }

        internal static string GetPrefix(int n)
        {
            return n switch
            {
                1 => "da",
                2 => "h",
                3 => "k",
                6 => "M",
                9 => "G",
                12 => "T",
                15 => "P",
                18 => "E",
                21 => "Z",
                24 => "Y",
                -1 => "d",
                -2 => "c",
                -3 => "m",
                -6 => "μ",
                -9 => "n",
                -12 => "p",
                -15 => "f",
                -18 => "a",
                -21 => "z",
                -24 => "y",
                _ => string.Empty,
            };
        }

        internal static double GetScale(int n)
        {
            return n switch
            {
                0 => 1.0,
                1 => 10.0,
                2 => 100.0,
                3 => 1E+3,
                6 => 1E+6,
                9 => 1E+9,
                12 => 1E+12,
                15 => 1E+15,
                18 => 1E+18,
                21 => 1E+21,
                24 => 1E+24,
                -1 => 0.1,
                -2 => 0.01,
                -3 => 0.001,
                -6 => 1E-06,
                -9 => 1E-09,
                -12 => 1E-12,
                -15 => 1E-15,
                -18 => 1E-18,
                -21 => 1E-21,
                -24 => 1E-24,
                _ => Math.Pow(10, n),
            };
        }

        private static int GetPower(double factor)
        {
            var d = factor switch
            {
                1.0  => 0.0,
                10.0 => 1.0,
                1E+2 => 2.0,
                1E+3 => 3.0,
                1E+4 => 4.0,
                1E+5 => 5.0,
                1E+6 => 6.0,
                0.1  => -1.0,
                1E-2 => -2.0,
                1E-3 => -3.0,
                1E-4 => -4.0,
                1E-5 => -5.0,
                1E-6 => -6.0,
                _ => Math.Log10(factor),
            };
            var n = (int)d;
            return Math.Abs(n - d) < 1E-12 ? n : 0;
        }

        private static string GetDimText(OutputWriter writer, string name, double factor, float power)
        {
            if (factor != 1.0)
            {
                switch (name)
                {
                    case "s":
                        {
                            if (factor == 60.0 || factor == 3600.0)
                            {
                                factor = 1.0;
                                name = factor == 60.0 ? "min" : "h";
                            }

                            break;
                        }   
                    case "g":
                        {
                            var a1 = factor / 14.59390294;
                            if (Math.Abs(a1 - Math.Round(a1)) < 1E-12)
                            {
                                name = "slug";
                                factor = a1;
                            }
                            else
                            {
                                var a2 = factor / 453.59237;
                                if (a2 < 1.0)
                                {
                                    var a3 = 1.0 / a2;
                                    if (Math.Abs(a3 - Math.Round(a3)) < 1E-12)
                                    {
                                        a3 = Math.Round(a3);
                                        factor = 1.0;
                                        switch (a3)
                                        {
                                            case 7000.0:
                                                name = "gr";
                                                break;
                                            case 256.0:
                                                name = "dr";
                                                break;
                                            case 16.0:
                                                name = "oz";
                                                break;
                                            default:
                                                name = "lb";
                                                factor = a3;
                                                break;
                                        }
                                    }
                                }
                                else if (Math.Abs(a2 - Math.Round(a2)) < 1E-12)
                                {
                                    a2 = Math.Round(a2);
                                    factor = 1.0;
                                    switch (a2)
                                    {
                                        case 14.0:
                                            name = "st";
                                            break;
                                        case 28.0:
                                            name = "qr";
                                            break;
                                        case 100.0:
                                            name = "cwt_US";
                                            break;
                                        case 112.0:
                                            name = "cwt_UK";
                                            break;
                                        case 1000.0:
                                            name = "kip";
                                            break;
                                        case 2000.0:
                                            name = "ton_US";
                                            break;
                                        case 2240.0:
                                            name = "ton_UK";
                                            break;
                                        default:
                                            name = "lb";
                                            factor = a2;
                                            break;
                                    }
                                }
                                else
                                    switch (factor)
                                    {
                                        case >= 1000000.0:
                                            factor /= 1000000.0;
                                            name = "t";
                                            break;
                                        case >= 1000.0:
                                            factor /= 1000.0;
                                            name = "kg";
                                            break;
                                    }
                            }
                            break;
                        }
                    case "m":
                        {
                            var a = factor / 2.54E-05;
                            if (Math.Abs(a - Math.Round(a)) < 1E-12)
                            {
                                a = Math.Round(a);
                                factor = 1.0;
                                switch (a)
                                {
                                    case 1.0:
                                        name = "th";
                                        break;
                                    case 1000.0:
                                        name = "in";
                                        break;
                                    case 36000.0:
                                        name = "yd";
                                        break;
                                    case 792000.0:
                                        name = "ch";
                                        break;
                                    case 7920000.0:
                                        name = "fur";
                                        break;
                                    case 63360000.0:
                                        name = "mi";
                                        break;
                                    default:
                                        name = "ft";
                                        factor = a / 12000.0;
                                        break;
                                }
                            }
                            break;
                        }
                }
                var n = GetPower(factor);
                name = writer.FormatUnits(GetPrefix(n) + name);
                factor /= GetScale(n);
                if (Math.Abs(factor - 1) > 1e-12)
                    name = writer.AddBrackets(writer.FormatReal(factor, 6) + '·' + name);
            }
            else
                name = writer.FormatUnits(name);

            var sp = writer.FormatReal(power, 1);
            if (power < 0 && writer is TextWriter)
                sp = writer.AddBrackets(sp);

            return power != 1f ? writer.FormatPower(name, sp, 0, -1) : name;
        }
    }
}