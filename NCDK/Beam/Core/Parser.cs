/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static NCDK.Beam.Configuration;
using static NCDK.Beam.Element;

namespace NCDK.Beam
{
    /// <summary>
	/// Parse a SMILES string and create a {@link Graph}. A new parser should be
	/// created for each invocation, for convenience {@link #Parse(string)} is
	/// provided.
    /// </summary>
	/// <blockquote><pre>
	/// Graph g = Parser.Parse("CCO");
	/// </pre></blockquote>
	/// <author>John May</author>
#if TEST
    public
#endif
    sealed class Parser
    {
        /// <summary>Keep track of branching.</summary>
        private readonly IntStack stack = new IntStack(10);

        /// <summary>Molecule being loaded.</summary>
        private readonly Graph g;

        /// <summary>Keep track of ring information.</summary>
        private RingBond[] rings = new RingBond[10];

        /// <summary>Local arrangement for ring openings.</summary>
        private IDictionary<int, LocalArrangement> arrangement
                = new Dictionary<int, LocalArrangement>(5);

        private IDictionary<int, Configuration> configurations
                = new Dictionary<int, Configuration>(5);

        /// <summary>Current bond.</summary>
        private Bond bond = Bond.Implicit;

        /// <summary>Current configuration.</summary>
        private Configuration configuration = Configuration.Unknown;


        /// <summary>
        /// Which vertices start a new run of tokens. This includes the first vertex
        /// and all vertices which immediately follow a 'dot' bond. These are
        /// required to correctly store atom topologies.
        /// </summary>
        private ICollection<int> start = new SortedSet<int>();

        /// <summary>Number of open rings - all rings should be closed.</summary>
        private int openRings = 0;

        /// <summary>Strict parsing.</summary>
        private readonly bool strict;

        private BitArray checkDirectionalBonds = new BitArray(0);    // realloc on demand

        /// <summary>
        /// Create a new parser for the specified buffer.
        /// </summary>
        /// <param name="buffer">character buffer holding a SMILES string</param>
        /// <exception cref="InvalidSmilesException">if the SMILES could not be parsed</exception>"
        public Parser(CharBuffer buffer, bool strict)
        {
            this.strict = strict;
            g = new Graph(1 + (2 * (buffer.Length / 3)));
            ReadSmiles(buffer);
            if (openRings > 0)
                throw new InvalidSmilesException("Unclosed ring detected:", buffer);
            if (stack.Count > 1)
                throw new InvalidSmilesException("Unclosed branch detected:", buffer);
            start.Add(0); // always include first vertex as start
            if (g.GetFlags(Graph.HAS_STRO) != 0)
            {
                CreateTopologies(buffer);
            }
        }

        /// <summary>
        /// Create a new (loose) parser for the specified string.
        /// <param name="str">SMILES string</param>
        // @ thrown if the SMILES could not be parsed
        /// </summary>
        public Parser(string str)
            : this(CharBuffer.FromString(str), false)
        {
        }

        /// <summary>
        /// Strict parsing of the provided SMILES string. The strict parser will
        /// throw more exceptions for unusual input.
        /// <param name="str">the SMILES string to process</param>
        /// <returns>a graph created with the strict parser</returns>
        /// </summary>
        public static Graph GetStrict(string str)
        {
            return new Parser(CharBuffer.FromString(str), true).Molecule();
        }

        /// <summary>
        /// Loose parsing of the provided SMILES string. The loose parser is more
        /// relaxed and will allow abnormal aromatic elements (e.g. 'te') as well as
        /// bare 'H', 'D' and 'T' for hydrogen and it's isotopes. Note the hydrogen
        /// and isotopes are replaced with their correct bracket equivalent.
        /// <param name="str">the SMILES string to process</param>
        /// <returns>a graph created with the loose parser</returns>
        /// </summary>
        public static Graph Losse(string str)
        {
            return new Parser(CharBuffer.FromString(str), false).Molecule();
        }

        /// <summary>
        /// Access the molecule created by the parser.
        /// <returns>the chemical graph for the parsed smiles string</returns>
        /// </summary>
        public Graph Molecule()
        {
            return g;
        }

        /// <summary>
        /// Create the topologies (stereo configurations) for the chemical graph. The
        /// topologies define spacial arrangement around atoms.
        /// </summary>CharBuffer buffer>
        private void CreateTopologies(CharBuffer buffer)
        {
            // create topologies (stereo configurations)
            foreach (var e in configurations)
            {
                AddTopology(e.Key,
                            Topology.ToExplicit(g, e.Key, e.Value));
            }

            for (int v = BitArrays.NextSetBit(checkDirectionalBonds, 0); v >= 0; v = BitArrays.NextSetBit(checkDirectionalBonds, v + 1))
            {
                int nUpV = 0;
                int nDownV = 0;
                int nUpW = 0;
                int nDownW = 0;
                int w = -1;

                {
                    int d = g.Degree(v);
                    for (int j = 0; j < d; ++j)
                    {
                        Edge e = g.EdgeAt(v, j);
                        Bond bond = e.GetBond(v);
                        if (bond == Bond.Up)
                            nUpV++;
                        else if (bond == Bond.Down)
                            nDownV++;
                        else if (bond == Bond.Double)
                            w = e.Other(v);
                    }
                }

                if (w < 0)
                    continue;

                BitArrays.EnsureCapacity(checkDirectionalBonds, w + 1);
                checkDirectionalBonds.Set(w, false);

                {
                    int d = g.Degree(w);
                    for (int j = 0; j < d; ++j)
                    {
                        Edge e = g.EdgeAt(w, j);
                        Bond bond = e.GetBond(w);
                        if (bond == Bond.Up)
                            nUpW++;
                        else if (bond == Bond.Down)
                            nDownW++;
                    }
                }

                if (nUpV + nDownV == 0 || nUpW + nDownW == 0)
                {
                    continue;
                }

                if (nUpV > 1)
                    throw new InvalidSmilesException("Multiple directional bonds on atom " + v, buffer);
                if (nDownV > 1)
                    throw new InvalidSmilesException("Multiple directional bonds on atom " + v, buffer);
                if (nUpW > 1)
                    throw new InvalidSmilesException("Multiple directional bonds on atom " + w, buffer);
                if (nDownW > 1)
                    throw new InvalidSmilesException("Multiple directional bonds on atom " + w, buffer);
            }
        }

        /// <summary>
        /// Add a topology for vertex 'u' with configuration 'c'. If the atom 'u' was
        /// involved in a ring closure the local arrangement is used instead of the
        /// Order in the graph. The configuration should be explicit '@TH1' or '@TH2'
        /// instead of '@' or '@@'.
        /// <param name="u">a vertex</param>
        /// <param name="c">explicit configuration of that vertex</param>
        // @see Topology#ToExplicit(Graph, int, Configuration)
        /// </summary>
        private void AddTopology(int u, Configuration c)
        {

            // stereo on ring closure - use local arrangement
            if (arrangement.ContainsKey(u))
            {
                int[] vs = arrangement[u].ToArray();
                List<Edge> es = new List<Edge>(vs.Length);
                foreach (var v in vs)
                    es.Add(g.CreateEdge(u, v));

                if (c.Type == Types.Tetrahedral)
                    vs = InsertThImplicitRef(u, vs); // XXX: temp fix
                else if (c.Type == Types.DoubleBond)
                    vs = InsertDbImplicitRef(u, vs); // XXX: temp fix

                g.AddTopology(Topology.Create(u, vs, es, c));
            }
            else
            {
                int[] us = new int[g.Degree(u)];
                IList<Edge> es = g.GetEdges(u);
                for (int i = 0; i < us.Length; i++)
                    us[i] = es[i].Other(u);

                if (c.Type == Configuration.Types.Tetrahedral)
                {
                    us = InsertThImplicitRef(u, us); // XXX: temp fix
                }
                else if (c.Type == Configuration.Types.DoubleBond)
                {
                    us = InsertDbImplicitRef(u, us); // XXX: temp fix
                }
                else if (c.Type == Configuration.Types.ExtendedTetrahedral)
                {
                    // Extended tetrahedral is a little more complicated, note
                    // following presumes the end atoms are not in ring closures
                    int v = es[0].Other(u);
                    int w = es[1].Other(u);
                    IList<Edge> vs = g.GetEdges(v);
                    IList<Edge> ws = g.GetEdges(w);
                    us = new int[] { -1, v, -1, w };
                    int i = 0;
                    foreach (var e in vs)
                    {
                        int x = e.Other(v);
                        if (e.Bond.Order == 1) us[i++] = x;
                    }

                    i = 2;
                    foreach (var e in ws)
                    {
                        int x = e.Other(w);
                        if (e.Bond.Order == 1) us[i++] = x;
                    }

                    if (us[0] < 0 || us[2] < 0)
                        return;

                    Array.Sort(us);
                }

                g.AddTopology(Topology.Create(u, us, es, c));
            }
        }

        // XXX: temporary fix for correcting configurations
        private int[] InsertThImplicitRef(int u, int[] vs)
        {
            if (vs.Length == 4)
                return vs;
            if (vs.Length != 3)
                throw new InvalidSmilesException("Invaid number of verticies for TH1/TH2 stereo chemistry");
            if (start.Contains(u))
                return new int[] { u, vs[0], vs[1], vs[2] };
            else
                return new int[] { vs[0], u, vs[1], vs[2] };
        }

        // XXX: temporary fix for correcting configurations
        private int[] InsertDbImplicitRef(int u, int[] vs)
        {
            if (vs.Length == 3)
                return vs;
            if (vs.Length != 2)
                throw new InvalidSmilesException("Invaid number of verticies for DB1/DB2 stereo chemistry");
            if (start.Contains(u))
                return new int[] { u, vs[0], vs[1] };
            else
                return new int[] { vs[0], u, vs[1] };
        }

        /// <summary>
        /// Add an atom and bond with the atom on the stack (if available and non-dot
        /// bond).
        /// <param name="a">an atom to add</param>
        /// </summary>
        private void AddAtom(Atom_ a, CharBuffer buffer)
        {
            int v = g.AddAtom(a);
            if (!stack.IsEmpty)
            {
                int u = stack.Pop();
                if (bond != Bond.Dot)
                {
                    if (bond.IsDirectional)
                    {
                        BitArrays.EnsureCapacity(checkDirectionalBonds, Math.Max(u, v) + 1);
                        checkDirectionalBonds.Set(u, true);
                        checkDirectionalBonds.Set(v, true);
                    }
                    g.AddEdge(new Edge(u, v, bond));
                }
                else
                {
                    start.Add(v); // start of a new run
                }
                if (arrangement.ContainsKey(u))
                    arrangement[u].Add(v);

            }
            stack.Push(v);
            bond = Bond.Implicit;

            // configurations used to create topologies after parsing
            if (configuration != Configuration.Unknown)
            {
                g.AddFlags(Graph.HAS_ATM_STRO);
                configurations.Add(v, configuration);
                configuration = Configuration.Unknown;
            }
        }

        /// <summary>
        /// Read a molecule from the character buffer.
        /// <param name="buffer">a character buffer</param>
        // @ invalid grammar
        private void ReadSmiles(CharBuffer buffer)
        {
            // primary dispatch
            while (buffer.HasRemaining())
            {
                char c = buffer.Get();
                switch (c)
                {

                    // aliphatic subset
                    case '*':
                        AddAtom(AtomImpl.AliphaticSubset.Unknown, buffer);
                        break;
                    case 'B':
                        if (buffer.GetIf('r'))
                            AddAtom(AtomImpl.AliphaticSubset.Bromine, buffer);
                        else
                            AddAtom(AtomImpl.AliphaticSubset.Boron, buffer);
                        break;
                    case 'C':
                        if (buffer.GetIf('l'))
                            AddAtom(AtomImpl.AliphaticSubset.Chlorine, buffer);
                        else
                            AddAtom(AtomImpl.AliphaticSubset.Carbon, buffer);
                        break;
                    case 'N':
                        AddAtom(AtomImpl.AliphaticSubset.Nitrogen, buffer);
                        break;
                    case 'O':
                        AddAtom(AtomImpl.AliphaticSubset.Oxygen, buffer);
                        break;
                    case 'P':
                        AddAtom(AtomImpl.AliphaticSubset.Phosphorus, buffer);
                        break;
                    case 'S':
                        AddAtom(AtomImpl.AliphaticSubset.Sulfur, buffer);
                        break;
                    case 'F':
                        AddAtom(AtomImpl.AliphaticSubset.Fluorine, buffer);
                        break;
                    case 'I':
                        AddAtom(AtomImpl.AliphaticSubset.Iodine, buffer);
                        break;

                    // aromatic subset
                    case 'b':
                        AddAtom(AtomImpl.AromaticSubset.Boron, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'c':
                        AddAtom(AtomImpl.AromaticSubset.Carbon, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'n':
                        AddAtom(AtomImpl.AromaticSubset.Nitrogen, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'o':
                        AddAtom(AtomImpl.AromaticSubset.Oxygen, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'p':
                        AddAtom(AtomImpl.AromaticSubset.Phosphorus, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 's':
                        AddAtom(AtomImpl.AromaticSubset.Sulfur, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;


                    // D/T for hydrogen isotopes - non-standard but OpenSMILES spec
                    // says it's possible. The D and T here are automatic converted
                    // to [2H] and [3H].
                    case 'H':
                        if (strict)
                            throw new InvalidSmilesException("hydrogens should be specified in square brackets - '[H]'",
                                                             buffer);
                        AddAtom(AtomImpl.EXPLICIT_HYDROGEN, buffer);
                        break;
                    case 'D':
                        if (strict)
                            throw new InvalidSmilesException("deuterium should be specified as a hydrogen isotope - '[2H]'",
                                                             buffer);
                        AddAtom(AtomImpl.DEUTERIUM, buffer);
                        break;
                    case 'T':
                        if (strict)
                            throw new InvalidSmilesException("tritium should be specified as a hydrogen isotope - '[3H]'",
                                                             buffer);
                        AddAtom(AtomImpl.TRITIUM, buffer);
                        break;

                    // bracket atom
                    case '[':
                        AddAtom(ReadBracketAtom(buffer), buffer);
                        break;

                    // ring bonds
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        Ring(c - '0', buffer);
                        break;
                    case '%':
                        int num = buffer.GetNumber(2);
                        if (num < 0)
                            throw new InvalidSmilesException("a number (<digit>+) must follow '%':", buffer);
                        Ring(num, buffer);
                        break;

                    // bond/dot
                    case '-':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Single;
                        break;
                    case '=':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Double;
                        break;
                    case '#':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Triple;
                        break;
                    case '$':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Quadruple;
                        break;
                    case ':':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        bond = Bond.Aromatic;
                        break;
                    case '/':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Up;
                        g.AddFlags(Graph.HAS_BND_STRO);
                        break;
                    case '\\':
                        // we allow C\\C=C/C since it could be an escaping error
                        if (bond != Bond.Implicit && bond != Bond.Down)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Down;
                        g.AddFlags(Graph.HAS_BND_STRO);
                        break;
                    case '.':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Bond specified before disconnection:", buffer);
                        bond = Bond.Dot;
                        break;

                    // branching
                    case '(':
                        if (stack.IsEmpty)
                            throw new InvalidSmilesException("cannot open branch - there were no previous atoms:",
                                                             buffer);
                        stack.Push(stack.Peek());
                        break;
                    case ')':
                        if (stack.Count < 2)
                            throw new InvalidSmilesException("closing of an unopened branch:",
                                                             buffer);
                        stack.Pop();
                        break;

                    // termination
                    case '\t':
                    case ' ':
                        // string suffix is title 
                        StringBuilder sb = new StringBuilder();
                        while (buffer.HasRemaining())
                        {
                            c = buffer.Get();
                            if (c == '\n' || c == '\r')
                                break;
                            sb.Append(c);
                        }
                        g.Title = sb.ToString();
                        return;
                    case '\n':
                    case '\r':
                        return;

                    default:
                        throw new InvalidSmilesException("unexpected character:", buffer);
                }
            }
        }

        /// <summary>
        /// Read a bracket atom from the buffer. A bracket atom optionally defines
        /// isotope, chirality, hydrogen count, formal charge and the atom class.
        /// <para>
        /// bracket_atom ::= '[' isotope? symbol chiral? hcount? charge? class? ']'
        /// </para>
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <returns>a bracket atom</returns>
        // @thrown if the bracket atom did not match the grammar, invalid symbol, missing closing bracket or invalid chiral specification.
        public Atom_ ReadBracketAtom(CharBuffer buffer)
        {
            int start = buffer.Position;

            bool arbitraryLabel = false;

            if (!buffer.HasRemaining())
                throw new InvalidSmilesException("Unclosed bracket atom", buffer);

            int isotope = buffer.GetNumber();
            bool aromatic = buffer.NextChar >= 'a' && buffer.NextChar <= 'z';
            Element element = Element.Read(buffer);

            if (strict && element == null)
                throw new InvalidSmilesException("unrecognised element symbol: ", buffer);

            if (element != null && aromatic)
                g.AddFlags(Graph.HAS_AROM);

            // element isn't aromatic as per the OpenSMILES specification
            if (strict && !element.IsAromatic(AromaticSpecification.OpenSmiles))
                throw new InvalidSmilesException("abnormal aromatic element", buffer);

            if (element == null)
            {
                arbitraryLabel = true;
            }

            configuration = Configuration.Read(buffer);

            int hCount = ReadHydrogens(buffer);
            int charge = ReadCharge(buffer);
            int atomClass = ReadClass(buffer);

            if (!arbitraryLabel && !buffer.GetIf(']'))
            {
                if (strict)
                {
                    throw InvalidSmilesException.InvalidBracketAtom(buffer);
                }
                else
                {
                    arbitraryLabel = true;
                }
            }

            if (arbitraryLabel)
            {
                int end = buffer.Position;
                int depth = 1;
                while (buffer.HasRemaining())
                {
                    char c = buffer.Get();
                    if (c == '[')
                        depth++;
                    else if (c == ']')
                    {
                        depth--;
                        if (depth == 0)
                            break;
                    }
                    end++;
                }
                if (depth != 0)
                    throw new InvalidSmilesException("unparsable label in bracket atom",
                                                     buffer,
                                                     buffer.Position - 1);
                string label = buffer.Substr(start, end);
                return new AtomImpl.BracketAtom(label);
            }

            return new AtomImpl.BracketAtom(isotope,
                                            element,
                                            hCount,
                                            charge,
                                            atomClass,
                                            aromatic);
        }

        /// <summary>
        /// Read the hydrogen count and progress the provided buffer. The hydrogen
        /// count is specified by a 'H' an 0 or more digits. A 'H' without digits is
        /// intercepted as 'H1'. When there is no 'H' or 'H0' is specified then the
        /// the hydrogen count is 0.
        /// <param name="buffer">a character buffer</param>
        /// <returns>the hydrogen count</returns>, 0 if none
        /// </summary>
        public static int ReadHydrogens(CharBuffer buffer)
        {
            if (buffer.GetIf('H'))
            {
                // when no number is specified 'H' then there is 1 hydrogen
                int count = buffer.GetNumber();
                return count < 0 ? 1 : count;
            }
            return 0;
        }

        /// <summary>
        /// Read a charge value and progress the provide buffer. The charge value is
        /// present in bracket atoms either directly after the symbol, the chiral
        /// specification or the hydrogen count. The specification of charge by
        /// concatenated signs (e.g. ++, --) and other bad form (e.g. '++-1') is
        /// intercepted.
        /// <param name="buffer">a character buffer</param>
        /// <returns>the formal charge value</returns>, 0 if none present
        // @see <a href="http://www.opensmiles.org/opensmiles.html#charge">Charge -
        ///      OpenSMILES Specification</a>
        /// </summary>
        public static int ReadCharge(CharBuffer buffer)
        {
            return ReadCharge(0, buffer);
        }

        /// <summary>
        /// Internal method for parsing charge, to allow concatenated signs (--, ++)
        /// the method recursively invokes increment or decrementing an accumulator.
        /// <param name="acc">   accumulator</param>
        /// <param name="buffer">a character buffer</param>
        /// <returns>the charge value</returns>
        /// </summary>
        private static int ReadCharge(int acc, CharBuffer buffer)
        {
            if (buffer.GetIf('+'))
                return buffer.NextIsDigit() ? acc + buffer.GetNumber()
                                            : ReadCharge(acc + 1, buffer);
            if (buffer.GetIf('-'))
                return buffer.NextIsDigit() ? acc - buffer.GetNumber()
                                            : ReadCharge(acc - 1, buffer);
            return acc;
        }

        /// <summary>
        /// Read the atom class of a bracket atom and progress the buffer (if read).
        /// The atom class is the last attribute of the bracket atom and is
        /// identified by a ':' followed by one or more digits. The atom class may be
        /// padded such that ':005' and ':5' are equivalent.
        /// <param name="buffer">a character buffer</param>
        /// <returns>the atom class</returns>, or 0
        // @see <a href="http://www.opensmiles.org/opensmiles.html#atomclass">Atom
        ///      Class - OpenSMILES Specification</a>
        /// </summary>
        public static int ReadClass(CharBuffer buffer)
        {
            if (buffer.GetIf(':'))
            {
                if (buffer.NextIsDigit())
                    return buffer.GetNumber();
                throw new InvalidSmilesException("invalid atom class, <digit>+ must follow ':'", buffer);
            }
            return 0;
        }

        /// <summary>
        /// Handle the ring open/closure of the specified ring number 'rnum'.
        /// <param name="rnum">ring number</param>
        // @ bond types did not match on ring closure
        /// </summary>
        private void Ring(int rnum, CharBuffer buffer)
        {
            if (bond == Bond.Dot)
                throw new InvalidSmilesException("a ring bond can not be a 'dot':",
                                                 buffer,
                                                 -1);
            if (rings.Length <= rnum || rings[rnum] == null)
            {
                OpenRing(rnum);
            }
            else
            {
                CloseRing(rnum, buffer);
            }
        }

        /// <summary>
        /// Open the ring bond with the specified 'rnum'.
        /// <param name="rnum">ring number</param>
        /// </summary>
        private void OpenRing(int rnum)
        {
            if (rnum >= rings.Length)
                rings = Arrays.CopyOf(rings,
                                      Math.Min(100, rnum * 2)); // max rnum: 99
            int u = stack.Peek();

            // create a ring bond
            rings[rnum] = new RingBond(u, bond);

            // keep track of arrangement (important for stereo configurations)
            CreateArrangement(u).Add(-rnum);
            openRings++;

            bond = Bond.Implicit;
        }

        /// <summary>
        /// Create the current local arrangement for vertex 'u' - if the arrangment
        /// already exists then that arrangement is used.
        /// <param name="u">vertex to get the arrangement around</param>
        /// <returns>current local arrangement</returns>
        /// </summary>
        private LocalArrangement CreateArrangement(int u)
        {
            LocalArrangement la;
            if (!arrangement.TryGetValue(u, out la))
            {
                la = new LocalArrangement();
                 int d = g.Degree(u);
                for (int j = 0; j < d; ++j)
                {
                     Edge e = g.EdgeAt(u, j);
                    la.Add(e.Other(u));
                }
                arrangement[u] = la;
            }
            return la;
        }

        /// <summary>
        /// Close the ring bond with the specified 'rnum'.
        /// </summary>
        /// <param name="rnum">ring number</param>
        // @ bond types did not match
        private void CloseRing(int rnum, CharBuffer buffer)
        {
            RingBond rbond = rings[rnum];
            rings[rnum] = null;
            int u = rbond.u;
            int v = stack.Peek();

            if (u == v)
                throw new InvalidSmilesException("Endpoints of ringbond are the same - loops are not allowed",
                                                 buffer);

            if (g.Adjacent(u, v))
                throw new InvalidSmilesException("Endpoints of ringbond are already connected - multi-edges are not allowed",
                                                 buffer);

            bond = DecideBond(rbond.bond, bond.Inverse(), buffer);

            if (bond.IsDirectional)
            {
                BitArrays.EnsureCapacity(checkDirectionalBonds, Math.Max(u, v) + 1);
                checkDirectionalBonds.Set(u, true);
                checkDirectionalBonds.Set(v, true);
            }

            g.AddEdge(new Edge(u, v, bond));
            bond = Bond.Implicit;
            // adjust the arrangement replacing where this ring number was openned
            arrangement[rbond.u].Replace(-rnum, stack.Peek());
            openRings--;
        }

        /// <summary>
        /// Decide the bond to use for a ring bond. The bond symbol can be present on
        /// either or both bonded atoms. This method takes those bonds, chooses the
        /// correct one or reports an error if there is a conflict.
        /// Equivalent SMILES:
        /// <blockquote><pre>
        ///     C=1CCCCC=1
        ///     C=1CCCCC1    (preferred)
        ///     C1CCCCC=1
        /// </pre></blockquote>
        /// <param name="a">a bond</param>
        /// <param name="b">other bond</param>
        /// <returns>the bond to use for this edge</returns>
        // @ ring bonds did not match
        /// </summary>
        public static Bond DecideBond(Bond a, Bond b, CharBuffer buffer)
        {
            if (a == b)
                return a;
            else if (a == Bond.Implicit)
                return b;
            else if (b == Bond.Implicit)
                return a;
            throw new InvalidSmilesException("Ring closure bonds did not match. Ring was opened with '" + a + "' and closed with '" + b + "'." +
                                                     " Note - directional bonds ('/','\\') are relative.",
                                             buffer,
                                             -1);
        }

        /// <summary>
        /// Convenience method for parsing a SMILES string.
        /// <param name="str">SMILES string</param>
        /// <returns>the chemical graph for the provided SMILES notation</returns>
        // @ thrown if the SMILES could not be interpreted
        /// </summary>
        public static Graph Parse(string str)
        {
            return new Parser(str).Molecule();
        }

        /// <summary>
        /// Hold information about ring open/closures. The ring bond can optionally
        /// specify the bond type.
        /// </summary>
        private sealed class RingBond
        {
            internal int u;
            internal Bond bond;

            public RingBond(int u, Bond bond)
            {
                this.u = u;
                this.bond = bond;
            }
        }

        /// <summary>
        /// Hold information on the local arrangement around an atom. The arrangement
        /// is normally identical to the Order loaded unless the atom is involved in
        /// a ring closure. This is particularly important for stereo specification
        /// where the ring bonds should be in the Order listed. This class stores the
        /// local arrangement by setting a negated 'rnum' as a placeholder and then
        /// replacing it once the connected atom has been read. Although this could
        /// be stored directly on the graph (negated edge) it allows us to keep all
        /// edges in sorted Order.
        /// </summary>
        private sealed class LocalArrangement
        {

            int[] vs;
            int n;

            /// <summary>New local arrangement.</summary>
            public LocalArrangement()
            {
                this.vs = new int[4];
            }

            /// <summary>
            /// Append a vertex to the arrangement.
            /// </summary>
            /// <param name="v">vertex to append</param>
            public void Add(int v)
            {
                if (n == vs.Length)
                    vs = Arrays.CopyOf(vs, n * 2);
                vs[n++] = v;
            }

            /// <summary>
            /// Replace the vertex 'u' with 'v'. Allows us to use negated values as
            /// placeholders.
            /// </summary>
            /// <blockquote><pre>
            /// LocalArrangement la = new LocalArrangement();
            /// la.Add(1);
            /// la.Add(-2);
            /// la.Add(-1);
            /// la.Add(5);
            /// la.Replace(-1, 4);
            /// la.Replace(-2, 6);
            /// la.ToArray() = {1, 6, 4, 5}
            /// </pre></blockquote>
            /// <param name="u">negated vertex</param>
            /// <param name="v">new vertex</param>
            public void Replace(int u, int v)
            {
                for (int i = 0; i < n; i++)
                {
                    if (vs[i] == u)
                    {
                        vs[i] = v;
                        return;
                    }
                }
            }

            /// <summary>
            /// Access the local arrange of vertices.
            /// </summary>
            /// <returns>array of vertices and there Order around an atom</returns>.
            public int[] ToArray()
            {
                return Arrays.CopyOf(vs, n);
            }
        }
    }
}
