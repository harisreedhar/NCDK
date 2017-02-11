/*
 * Copyright (c) 2016 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NCDK.SGroups
{
    /**
     * Light-weight intermediate data-structure for transferring information CDK to/from
     * CXSMILES.
     */
#if TEST
    public
#endif
    sealed class CxSmilesState
    {
        public IDictionary<int, string> atomLabels = null;
        public IDictionary<int, string> atomValues = null;
        public IList<double[]> AtomCoords { get; set; } = null;
        public IList<IList<int>> fragGroups = null;
        public IDictionary<int, Radical> atomRads = null;
        public IDictionary<int, IList<int>> positionVar = null;
        public IList<PolymerSgroup> sgroups = null;
        public IList<DataSgroup> dataSgroups = null;
        public bool zCoords = false;

        public enum Radical
        {
            Monovalent,
            Divalent,
            DivalentSinglet,
            DivalentTriplet,
            Trivalent,
            TrivalentDoublet,
            TrivalentQuartet
        }

        public sealed class DataSgroup
        {
            readonly IList<int> atoms;
            readonly string field;
            readonly string value;
            readonly string operator_;

            readonly string unit;

            readonly string tag;

            public DataSgroup(IList<int> atoms, string field, string value, string operator_, string unit, string tag)
            {
                this.atoms = atoms;
                this.field = field;
                this.value = value;
                this.operator_ = operator_;
                this.unit = unit;
                this.tag = tag;
            }

            public override bool Equals(Object o)
            {
                DataSgroup that = o as DataSgroup;
                if (that == null)
                    return false;

                if (atoms != null ? !Compares.AreEqual(atoms, that.atoms) : that.atoms != null) return false;
                if (field != null ? !field.Equals(that.field) : that.field != null) return false;
                if (value != null ? !value.Equals(that.value) : that.value != null) return false;
                if (operator_ != null ? !operator_.Equals(that.operator_) : that.operator_ != null) return false;
                if (unit != null ? !unit.Equals(that.unit) : that.unit != null) return false;
                return tag != null ? tag.Equals(that.tag) : that.tag == null;

            }


            public override int GetHashCode()
            {
                int result = atoms != null ? atoms.GetHashCode() : 0;
                result = 31 * result + (field != null ? field.GetHashCode() : 0);
                result = 31 * result + (value != null ? value.GetHashCode() : 0);
                result = 31 * result + (operator_ != null ? operator_.GetHashCode() : 0);
                result = 31 * result + (unit != null ? unit.GetHashCode() : 0);
                result = 31 * result + (tag != null ? tag.GetHashCode() : 0);
                return result;
            }

            public override string ToString()
            {
                return "DataSgroup{" +
                       "atoms=" + atoms +
                       ", field='" + field + '\'' +
                       ", value='" + value + '\'' +
                       ", operator='" + operator_ + '\'' +
                       ", unit='" + unit + '\'' +
                       ", tag='" + tag + '\'' +
                       '}';
            }

            internal string Field => field;
            internal string Value => value;
        }

        public sealed class PolymerSgroup
        {
            readonly string type;
            readonly IList<int> atomset;
            readonly string subscript;
            readonly string supscript;

            public PolymerSgroup(string type, IList<int> atomset, string subscript, string supscript)
            {
                Trace.Assert(type != null && atomset != null && subscript != null && supscript != null);
                this.type = type;
                this.atomset = new List<int>(atomset);
                this.subscript = subscript;
                this.supscript = supscript;
            }


            public override bool Equals(Object o)
            {
                PolymerSgroup that = o as PolymerSgroup;
                if (that == null)
                    return false;

                return type.Equals(that.type) &&
                       Compares.AreEqual(atomset, that.atomset) &&
                       subscript.Equals(that.subscript) &&
                       supscript.Equals(that.supscript);
            }


            public override int GetHashCode()
            {
                int result = type.GetHashCode();
                foreach (var a in atomset)
                    result = 31 * result + a.GetHashCode();
                result = 31 * result + subscript.GetHashCode();
                result = 31 * result + supscript.GetHashCode();
                return result;
            }


            public override string ToString()
            {
                return "PolymerSgroup{" +
                       "type='" + type + '\'' +
                       ", atomset=" + atomset +
                       ", subscript='" + subscript + '\'' +
                       ", supscript='" + supscript + '\'' +
                       '}';
            }

            internal string Type => type;
            internal IList<int> AtomSet => atomset;
            internal string Subscript => subscript;
            internal string Supscript => supscript;
        }


        static string Escape(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (IsEscapeChar(c))
                    sb.Append("&#").Append((int)c).Append(';');
                else
                    sb.Append(c);
            }
            return sb.ToString();

        }

        private static bool IsEscapeChar(char c)
        {
            return c < 32 || c > 126 || c == '|' || c == '{' || c == '}' || c == ',' || c == ';' || c == ':' || c == '$';
        }
    }
}