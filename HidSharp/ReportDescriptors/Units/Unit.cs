#region License
/* Copyright 2011 James F. Bellinger <http://www.zer7.com>

   Permission to use, copy, modify, and/or distribute this software for any
   purpose with or without fee is hereby granted, provided that the above
   copyright notice and this permission notice appear in all copies.

   THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
   WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
   MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
   ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
   ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
   OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE. */
#endregion

using System;

namespace HidSharp.ReportDescriptors.Units
{
    public class Unit
    {
        uint _value;

        public Unit(uint value)
        {
            _value = value;
        }

        uint Element(int index)
        {
            return (Value >> (index << 2)) & 0xf;
        }

        int Exponent(int index)
        {
            return DecodeExponent(Element(index));
        }

        public static int DecodeExponent(uint value)
        {
            return value >= 8 ? (int)value - 16 : (int)value;
        }

        void Element(int index, uint value)
        {
            Value &= 0xfu << (index << 2); Value |= (value & 0xfu) << (index << 2);
        }

        public static uint EncodeExponent(int value)
        {
            if (value < -8 || value > 7)
                { throw new ArgumentOutOfRangeException("Exponent range is [-8, 7]."); }
            return (uint)(value < 0 ? value + 16 : value);
        }

        void Exponent(int index, int value)
        {
            Element(index, EncodeExponent(value));
        }

        public UnitSystem System
        {
            get { return (UnitSystem)Element(0); }
            set { Element(0, (uint)value); }
        }

        public int LengthExponent
        {
            get { return Exponent(1); }
            set { Exponent(1, value); }
        }

        public LengthUnit LengthUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear: return LengthUnit.Centimeter;
                    case UnitSystem.SIRotation: return LengthUnit.Radians;
                    case UnitSystem.EnglishLinear: return LengthUnit.Inch;
                    case UnitSystem.EnglishRotation: return LengthUnit.Degrees;
                    default: return LengthUnit.None;
                }
            }
        }

        public int MassExponent
        {
            get { return Exponent(2); }
            set { Exponent(2, value); }
        }

        public MassUnit MassUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return MassUnit.Gram;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return MassUnit.Slug;
                    default: return MassUnit.None;
                }
            }
        }

        public int TimeExponent
        {
            get { return Exponent(3); }
            set { Exponent(3, value); }
        }

        public TimeUnit TimeUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? TimeUnit.Seconds : TimeUnit.None;
            }
        }

        public int TemperatureExponent
        {
            get { return Exponent(4); }
            set { Exponent(4, value); }
        }

        public TemperatureUnit TemperatureUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return TemperatureUnit.Kelvin;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return TemperatureUnit.Fahrenheit;
                    default: return TemperatureUnit.None;
                }
            }
        }

        public int CurrentExponent
        {
            get { return Exponent(5); }
            set { Exponent(5, value); }
        }

        public CurrentUnit CurrentUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? CurrentUnit.Ampere : CurrentUnit.None;
            }
        }

        public int LuminousIntensityExponent
        {
            get { return Exponent(6); }
            set { Exponent(6, value); }
        }

        public LuminousIntensityUnit LuminousIntensityUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? LuminousIntensityUnit.Candela : LuminousIntensityUnit.None;
            }
        }

        public uint Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
