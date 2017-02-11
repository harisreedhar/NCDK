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

using System;
using System.Collections.Generic;
using System.Linq;
using static NCDK.SGroups.CxSmilesState;

namespace NCDK.SGroups
{
    /**
     * Parse CXSMILES (ChemAxon Extended SMILES) layers. The layers are suffixed after the SMILES but before the title
     * and encode a large number of the features. CXSMILES was not intended for outside consumption so has some quirks
     * but does provide some useful features. This parser handles a subset of the grammar:
     * <br>
     * <pre>
     * - Atom Labels
     * - Atom Values
     * - Atom Coordinates
     * - Positional Variations
     * - Polymer Sgroups
     * - Atom Radicals
     * - Fragment grouping
     * </pre>
     * The following properties are ignored
     * <pre>
     * - cis/trans specification
     * - relative stereochemistry
     * </pre>
     */
#if TEST
    public
#endif
    sealed class CxSmilesParser
    {
        private const char COMMA_SEPARATOR = ',';
        private const char DOT_SEPARATOR = '.';

        private CxSmilesParser()
        {
        }

        /**
         * Process atom labels from extended SMILES in a char iter.
         *
         * @param iter char iteration
         * @param dest destination of labels (atomidx->label)
         * @return parse success/failure
         */
        private static bool ProcessAtomLabels(CharIter iter, IDictionary<int, string> dest)
        {
            int atomIdx = 0;
            while (iter.MoveNext())
            {

                // fast forward through empty labels
                while (iter.NextIf(';'))
                    atomIdx++;

                char c = iter.Next();
                if (c == '$')
                {
                    // end of atom label
                    return true;
                }
                else
                {
                    int beg = iter.pos - 1;
                    while (iter.MoveNext())
                    {
                        if (iter.Curr() == ';' || iter.Curr() == '$')
                            break;
                        iter.Next();
                    }
                    dest.Add(atomIdx, Unescape(iter.Substr(beg, iter.pos)));
                    atomIdx++;
                    if (iter.NextIf('$'))
                    {
                        iter.NextIf(','); // optional
                        return true;
                    }
                    if (!iter.NextIf(';'))
                        return false;
                }
            }
            return false;
        }

        private static double ReadDouble(CharIter iter)
        {
            int sign = +1;
            if (iter.NextIf('-'))
                sign = -1;
            else if (iter.NextIf('+'))
                sign = +1;
            double intPart;
            double fracPart = 0;
            int divisor = 1;

            intPart = (double)ProcessUnsignedInt(iter);
            if (intPart < 0) intPart = 0;
            iter.NextIf('.');

            char c;
            while (iter.MoveNext() && IsDigit(c = iter.Curr()))
            {
                fracPart *= 10;
                fracPart += c - '0';
                divisor *= 10;
                iter.Next();
            }

            return sign * (intPart + (fracPart / divisor));
        }

        /**
         * Coordinates are written between parenthesis. The z-coord may be omitted '(0,1,),(2,3,)'.
         * @param iter input characters, iterator is progressed by this method
         * @param state output CXSMILES state
         * @return parse was a success (or not)
         */
        private static bool ProcessCoords(CharIter iter, CxSmilesState state)
        {
            if (state.AtomCoords == null)
                state.AtomCoords = new List<double[]>();
            while (iter.MoveNext())
            {

                // end of coordinate list
                if (iter.Curr() == ')')
                {
                    iter.Next();
                    iter.NextIf(','); // optional
                    return true;
                }

                double x = ReadDouble(iter);
                if (!iter.NextIf(','))
                    return false;
                double y = ReadDouble(iter);
                if (!iter.NextIf(','))
                    return false;
                double z = ReadDouble(iter);
                iter.NextIf(';');

                state.zCoords = state.zCoords || z != 0;
                state.AtomCoords.Add(new double[] { x, y, z });
            }
            return false;
        }

        /**
         * Fragment grouping defines disconnected components that should be considered part of a single molecule (i.e.
         * Salts). Examples include NaH, AlCl3, Cs2CO3, HATU, etc.
         *
         * @param iter input characters, iterator is progressed by this method
         * @param state output CXSMILES state
         * @return parse was a success (or not)
         */
        private static bool ProcessFragmentGrouping(CharIter iter, CxSmilesState state)
        {
            if (state.fragGroups == null)
                state.fragGroups = new List<IList<int>>();
            IList<int> dest = new List<int>();
            while (iter.MoveNext())
            {
                dest.Clear();
                if (!ProcessIntList(iter, DOT_SEPARATOR, dest))
                    return false;
                iter.NextIf(COMMA_SEPARATOR);
                if (dest.Count == 0)
                    return true;
                state.fragGroups.Add(new List<int>(dest));
            }
            return false;
        }

        /**
         * Sgroup polymers in CXSMILES can be variable length so may be terminated either with the next group
         * or the end of the CXSMILES.
         *
         * @param c character
         * @return character an delimit an Sgroup
         */
        private static bool IsSgroupDelim(char c)
        {
            return c == ':' || c == ',' || c == '|';
        }

        private static bool ProcessDataSgroups(CharIter iter, CxSmilesState state)
        {

            if (state.dataSgroups == null)
                state.dataSgroups = new List<DataSgroup>(4);

            IList<int> atomset = new List<int>();
            if (!ProcessIntList(iter, COMMA_SEPARATOR, atomset))
                return false;

            if (!iter.NextIf(':'))
                return false;
            int beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string field = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
                return false;
            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string value = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, "", "", ""));
                return true;
            }

            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string operator_ = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, "", ""));
                return true;
            }

            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string unit = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, unit, ""));
                return true;
            }

            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string tag = Unescape(iter.Substr(beg, iter.pos));

            state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, unit, tag));

            return true;
        }

        /**
         * Polymer Sgroups describe variations of repeating units. Only the atoms and not crossing bonds are written.
         *
         * @param iter input characters, iterator is progressed by this method
         * @param state output CXSMILES state
         * @return parse was a success (or not)
         */
        private static bool ProcessPolymerSgroups(CharIter iter, CxSmilesState state)
        {
            if (state.sgroups == null)
                state.sgroups = new List<PolymerSgroup>();
            int beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string keyword = iter.Substr(beg, iter.pos);
            if (!iter.NextIf(':'))
                return false;
            IList<int> atomset = new List<int>();
            if (!ProcessIntList(iter, COMMA_SEPARATOR, atomset))
                return false;


            string subscript;
            string supscript;

            if (!iter.NextIf(':'))
                return false;

            // "If the subscript equals the keyword of the Sgroup this field can be empty", ergo
            // if omitted it equals the keyword
            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            subscript = Unescape(iter.Substr(beg, iter.pos));
            if (string.IsNullOrEmpty(subscript))
                subscript = keyword;

            // "In the superscript only connectivity and flip information is allowed.", default
            // appears to be "eu" either/unspecified
            if (!iter.NextIf(':'))
                return false;
            beg = iter.pos;
            while (iter.MoveNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            supscript = Unescape(iter.Substr(beg, iter.pos));
            if (string.IsNullOrEmpty(supscript))
                supscript = "eu";

            if (iter.NextIf(',') || iter.Curr() == '|')
            {
                state.sgroups.Add(new CxSmilesState.PolymerSgroup(keyword, atomset, subscript, supscript));
                return true;
            }
            // not supported: crossing bond info (difficult to work out from doc) and bracket orientation

            return false;
        }

        /**
         * Positional variation/multi centre bonding. Describe as a begin atom and one or more end points.
         *
         * @param iter input characters, iterator is progressed by this method
         * @param state output CXSMILES state
         * @return parse was a success (or not)
         */
        private static bool ProcessPositionalVariation(CharIter iter, CxSmilesState state)
        {
            if (state.positionVar == null)
                state.positionVar = new SortedDictionary<int, IList<int>>();
            while (iter.MoveNext())
            {
                if (IsDigit(iter.Curr()))
                {
                    int beg = ProcessUnsignedInt(iter);
                    if (!iter.NextIf(':'))
                        return false;
                    IList<int> endpoints = new List<int>(6);
                    if (!ProcessIntList(iter, DOT_SEPARATOR, endpoints))
                        return false;
                    iter.NextIf(',');
                    state.positionVar.Add(beg, endpoints);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * CXSMILES radicals.
         *
         * @param iter input characters, iterator is progressed by this method
         * @param state output CXSMILES state
         * @return parse was a success (or not)
         */
        private static bool ProcessRadicals(CharIter iter, CxSmilesState state)
        {
            if (state.atomRads == null)
                state.atomRads = new SortedDictionary<int, Radical>();
            CxSmilesState.Radical rad;
            switch (iter.Next())
            {
                case '1':
                    rad = CxSmilesState.Radical.Monovalent;
                    break;
                case '2':
                    rad = CxSmilesState.Radical.Divalent;
                    break;
                case '3':
                    rad = CxSmilesState.Radical.DivalentSinglet;
                    break;
                case '4':
                    rad = CxSmilesState.Radical.DivalentTriplet;
                    break;
                case '5':
                    rad = CxSmilesState.Radical.Trivalent;
                    break;
                case '6':
                    rad = CxSmilesState.Radical.TrivalentDoublet;
                    break;
                case '7':
                    rad = CxSmilesState.Radical.TrivalentQuartet;
                    break;
                default:
                    return false;
            }
            if (!iter.NextIf(':'))
                return false;
            IList<int> dest = new List<int>(4);
            if (!ProcessIntList(iter, COMMA_SEPARATOR, dest))
                return false;
            foreach (var atomidx in dest)
                state.atomRads.Add(atomidx, rad);
            return true;
        }

        /**
         * Parse an string possibly containing CXSMILES into an intermediate state
         * ({@link CxSmilesState}) representation.
         *
         * @param str input character string (SMILES title field)
         * @param state output CXSMILES state
         * @return position where CXSMILES ends (below 0 means no CXSMILES)
         */
        public static int ProcessCx(string str, CxSmilesState state)
        {

            CharIter iter = new CharIter(str);

            if (!iter.NextIf('|'))
                return -1;

            while (iter.MoveNext())
            {
                switch (iter.Next())
                {
                    case '$': // atom labels and values
                              // dest is atom labels by default
                        IDictionary<int, string> dest = state.atomLabels = new SortedDictionary<int, string>();
                        // check for atom values
                        if (iter.NextIf("_AV:"))
                            dest = state.atomValues = new SortedDictionary<int, string>();
                        if (!ProcessAtomLabels(iter, dest))
                            return -1;
                        break;
                    case '(': // coordinates
                        if (!ProcessCoords(iter, state))
                            return -1;
                        break;
                    case 'c': // Skip cis/trans/unspec
                    case 't':
                        // c/t:
                        if (iter.NextIf(':'))
                        {
                            if (!SkipIntList(iter, COMMA_SEPARATOR))
                                return -1;
                        }
                        // ctu:
                        else if (iter.NextIf("tu:"))
                        {
                            if (!SkipIntList(iter, COMMA_SEPARATOR))
                                return -1;
                        }
                        break;
                    case 'r': // Skip relative stereochemistry
                        if (!iter.NextIf(':'))
                            return -1;
                        if (!SkipIntList(iter, COMMA_SEPARATOR))
                            return -1;
                        break;
                    case 'f': // fragment grouping
                        if (!iter.NextIf(':'))
                            return -1;
                        if (!ProcessFragmentGrouping(iter, state))
                            return -1;
                        break;
                    case 'S': // Sgroup polymers
                        if (iter.NextIf("g:"))
                        {
                            if (!ProcessPolymerSgroups(iter, state))
                                return -1;
                        }
                        else if (iter.NextIf("gD:"))
                        {
                            if (!ProcessDataSgroups(iter, state))
                                return -1;
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    case 'm': // positional variation
                        if (!iter.NextIf(':'))
                            return -1;
                        if (!ProcessPositionalVariation(iter, state))
                            return -1;
                        break;
                    case '^': // Radicals
                        if (!ProcessRadicals(iter, state))
                            return -1;
                        break;
                    case 'C':
                    case 'H': // skip coordination and hydrogen bonding
                        if (!iter.NextIf(':'))
                            return -1;
                        while (iter.MoveNext() && IsDigit(iter.Curr()))
                        {
                            if (!SkipIntList(iter, DOT_SEPARATOR))
                                return -1;
                            iter.NextIf(',');
                        }
                        break;
                    case '|': // end of CX
                              // consume optional separators
                        if (!iter.NextIf(' ')) iter.NextIf('\t');
                        return iter.pos;
                    default:
                        return -1;
                }
            }

            return -1;
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool SkipIntList(CharIter iter, char sep)
        {
            while (iter.MoveNext())
            {
                char c = iter.Curr();
                if (IsDigit(c) || c == sep)
                    iter.Next();
                else
                    return true;
            }
            // ran of end
            return false;
        }

        private static int ProcessUnsignedInt(CharIter iter)
        {
            if (!iter.MoveNext())
                return -1;
            char c = iter.Curr();
            if (!IsDigit(c))
                return -1;
            int res = c - '0';
            iter.Next();
            while (iter.MoveNext() && IsDigit(c = iter.Curr()))
            {
                res = res * 10 + c - '0';
                iter.Next();
            }
            return res;
        }

        /**
         * Process a list of unsigned integers.
         *
         * @param iter char iter
         * @param sep the separator
         * @param dest output
         * @return int-list was successfully processed
         */
        private static bool ProcessIntList(CharIter iter, char sep, IList<int> dest)
        {
            while (iter.MoveNext())
            {
                char c = iter.Curr();
                if (IsDigit(c))
                {
                    int r = ProcessUnsignedInt(iter);
                    if (r < 0) return false;
                    iter.NextIf(sep);
                    dest.Add(r);
                }
                else
                {
                    return true;
                }
            }
            // ran of end
            return false;
        }

        public static string Unescape(string str)
        {
            int dst = 0;
            int src = 0;
            char[] chars = str.ToCharArray();
            int len = chars.Length;
            while (src < chars.Length)
            {
                // match the pattern &#[0-9][0-9]*;
                if (src + 3 < len && chars[src] == '&' && chars[src + 1] == '#' && IsDigit(chars[src + 2]))
                {
                    int tmp = src + 2;
                    int code = 0;
                    while (tmp < len && IsDigit(chars[tmp]))
                    {
                        code *= 10;
                        code += chars[tmp] - '0';
                        tmp++;
                    }
                    if (tmp < len && chars[tmp] == ';')
                    {
                        src = tmp + 1;
                        chars[dst++] = (char)code;
                        continue;
                    }
                }
                chars[dst++] = chars[src++];
            }
            return new string(chars, 0, dst);
        }

        /**
         * Utility for parsing a sequence of characters. The char iter allows us to pull
         * of one or more characters at a time and track where we are in the string.
         */
        sealed class CharIter
        {

            private readonly string str;
            private readonly int len;
            public int pos = 0;

            public CharIter(string str)
            {
                this.str = str;
                this.len = str.Length;
            }

            /**
             * If the next character matches the provided query the iterator is progressed.
             *
             * @param c query character
             * @return iterator was moved forwards
             */
            public bool NextIf(char c)
            {
                if (!MoveNext() || str[pos] != c)
                    return false;
                pos++;
                return true;
            }

            /**
             * If the next sequence of characters matches the prefix the iterator
             * is progressed to character following the prefix.
             *
             * @param prefix prefix string
             * @return iterator was moved forwards
             */
            public bool NextIf(string prefix)
            {
                bool res;
                if (res = this.str.Substring(pos).StartsWith(prefix))
                    pos += prefix.Length;
                return res;
            }

            /**
             * Is there more chracters to read?
             *
             * @return whether more characters are available
             */
            public bool MoveNext()
            {
                return pos < len;
            }

            /**
             * Access the current character of the iterator.
             *
             * @return charactor
             */
            public char Curr()
            {
                return str[pos];
            }

            /**
             * Access the current character of the iterator and move
             * to the next position.
             *
             * @return charactor
             */
            public char Next()
            {
                return str[pos++];
            }

            /**
             * Access a substring from the iterator.
             *
             * @param beg begin position (inclusive)
             * @param end end position (exclusive)
             * @return substring
             */
            public string Substr(int beg, int end)
            {
                return str.Substring(beg, end - beg);
            }
        }
    }
}
