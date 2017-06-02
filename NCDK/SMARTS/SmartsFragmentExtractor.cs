﻿/*
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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */
using NCDK.Common.Collections;
using System;
using System.Text;

namespace NCDK.SMARTS
{
    /// <summary>
    /// Utility class to create SMARTS that match part (substructure) of a molecule.
    /// SMARTS are generated by providing the atom indexes. An example use cases is
    /// encoding features from a fingerprint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The extractor has two modes. <see cref="MODE_EXACT"/> (default) captures the element,
    /// valence, hydrogen count, connectivity, and charge in the SMARTS atom expressions.
    /// The alternative mode, <see cref="MODE_JCOMPOUNDMAPPER"/>, only captures the element,
    /// non-zero charge, and peripheral bonds. Although the later looks cleaner, the
    /// peripheral bonds intend to capture the connectivity of the terminal atoms but
    /// since the valence is not bounded further substitution is still allowed. This
    /// mirrors functionality from jCompoundMapper <token>cdk-cite-Hinselmann2011</token>.
    /// </para>
    /// <para>
    /// The difference is easily demonstrated for methyl. Consider the compound
    /// of 2-methylpentane <pre>CC(C)CCC</pre>, if we extract one of the methyl atoms
    /// depending on the mode we obtain <pre>[CH3v4X4+0]</pre> or <pre>C*</pre>. The first
    /// of these patterns (obtained by <see cref="MODE_EXACT"/>) matches the compound in
    /// <b>three places</b> (the three methyl groups). The second matches <b>six</b>
    /// times (every atom) because the substituion on the carbon is not locked.
    /// A further complication is introduced by the inclusion of the peripheral atoms,
    /// for 1H-indole <pre>[nH]1ccc2c1cccc2</pre> we can obtain the SMARTS <pre>n(ccc(a)a)a</pre>
    /// that doesn't match at all. This is because one of the aromatic atoms ('a')
    /// needs to match the nitrogen.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// IChemObjectBuilder      bldr      = SilentChemObjectBuilder.getInstance();
    /// SmilesParser            smipar    = new SmilesParser(bldr);
    /// 
    /// IAtomContainer          mol       = smipar.parseSmiles("[nH]1ccc2c1cccc2");
    /// SmartsFragmentExtractor subsmarts = new SmartsFragmentExtractor(mol);
    /// 
    /// // smarts=[nH1v3X3+0][cH1v4X3+0][cH1v4X3+0][cH0v4X3+0]
    /// // hits  =1
    /// String             smarts    = mol.generate(new int[]{0,1,3,4});
    /// 
    /// subsmarts.setMode(MODE_JCOMPOUNDMAPPER);
    /// // smarts=n(ccc(a)a)a
    /// // hits  = 0 - one of the 'a' atoms needs to match the nitrogen
    /// String             smarts    = mol.generate(new int[]{0,1,3,4});
    /// </code>
    /// </example>
    // @author Nikolay Kochev
    // @author Nina Jeliazkova
    // @author John May 
    public sealed class SmartsFragmentExtractor
    {
        /// <summary>
        /// Sets the mode of the extractor to produce SMARTS similar to JCompoundMapper.
        /// </summary>
        public const int MODE_JCOMPOUNDMAPPER = 1;

        /// <summary>
        /// Sets the mode of the extractor to produce exact SMARTS.
        /// </summary>
        public const int MODE_EXACT = 2;

        // molecule being selected over
        private readonly IAtomContainer mol;

        // fast-access mol graph data structures
        private readonly int[][] atomAdj, bondAdj;
        private readonly int[] deg;

        // SMARTS atom and bond expressions
        private readonly string[] aexpr;
        private readonly string[] bexpr;

        // SMARTS traversal/generation
        private readonly int[] avisit;
        private readonly int[] rbnds;
        private readonly int[] rnums;
        private int numVisit;

        // which mode should SMARTS be encoded in
        private int mode = MODE_EXACT;

        /// <summary>
        /// Create a new instance over the provided molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        public SmartsFragmentExtractor(IAtomContainer mol)
        {
            this.mol = mol;

            int numAtoms = mol.Atoms.Count;
            int numBonds = mol.Bonds.Count;

            // build fast access
            this.deg = new int[numAtoms];
            this.atomAdj = Arrays.CreateJagged<int>(numAtoms, 4);
            this.bondAdj = Arrays.CreateJagged<int>(numAtoms, 4);
            this.aexpr = new string[numAtoms];
            this.bexpr = new string[numBonds];
            this.avisit = new int[numAtoms];
            this.rbnds = new int[numBonds];
            this.rnums = new int[100]; // max 99 in SMILES/SMARTS

            // index adjacency information and bond expressions for quick
            // reference and traversal
            for (int bondIdx = 0; bondIdx < numBonds; bondIdx++)
            {
                IBond bond = mol.Bonds[bondIdx];
                IAtom beg = bond.Begin;
                IAtom end = bond.End;
                int begIdx = mol.Atoms.IndexOf(beg);
                int endIdx = mol.Atoms.IndexOf(end);
                this.bexpr[bondIdx] = EncodeBondExpr(bondIdx, begIdx, endIdx);

                // make sufficient space
                if (deg[begIdx] == atomAdj[begIdx].Length)
                {
                    atomAdj[begIdx] = Arrays.CopyOf(atomAdj[begIdx], deg[begIdx] + 2);
                    bondAdj[begIdx] = Arrays.CopyOf(bondAdj[begIdx], deg[begIdx] + 2);
                }
                if (deg[endIdx] == atomAdj[endIdx].Length)
                {
                    atomAdj[endIdx] = Arrays.CopyOf(atomAdj[endIdx], deg[endIdx] + 2);
                    bondAdj[endIdx] = Arrays.CopyOf(bondAdj[endIdx], deg[endIdx] + 2);
                }

                atomAdj[begIdx][deg[begIdx]] = endIdx;
                bondAdj[begIdx][deg[begIdx]] = bondIdx;
                atomAdj[endIdx][deg[endIdx]] = begIdx;
                bondAdj[endIdx][deg[endIdx]] = bondIdx;

                deg[begIdx]++;
                deg[endIdx]++;
            }

            // pre-generate atom expressions
            for (int atomIdx = 0; atomIdx < numAtoms; atomIdx++)
                this.aexpr[atomIdx] = EncodeAtomExpr(atomIdx);
        }

        /// <summary>
        /// Set the mode of SMARTS substructure selection
        /// </summary>
        /// <param name="mode">the mode</param>
        public void SetMode(int mode)
        {
            // check arg
            switch (mode)
            {
                case MODE_EXACT:
                case MODE_JCOMPOUNDMAPPER:
                    break;
                default:
                    throw new ArgumentException("Invalid mode specified!");
            }
            this.mode = mode;

            // re-gen atom expressions
            int numAtoms = mol.Atoms.Count;
            for (int atomIdx = 0; atomIdx < numAtoms; atomIdx++)
                this.aexpr[atomIdx] = EncodeAtomExpr(atomIdx);
        }

        /// <summary>
        /// Generate a SMARTS for the substructure formed of the provided
        /// atoms.
        /// </summary>
        /// <param name="atomIdxs">atom indexes</param>
        /// <returns>SMARTS, null if an empty array is passed</returns>
        public string Generate(int[] atomIdxs)
        {
            if (atomIdxs == null)
                throw new ArgumentNullException(nameof(atomIdxs), "No atom indexes provided");
            if (atomIdxs.Length == 0)
                return null; // makes sense?

            // special case
            if (atomIdxs.Length == 1 && mode == MODE_EXACT)
                return aexpr[atomIdxs[0]];

            // initialize traversal information
            Arrays.Fill(rbnds, 0);
            Arrays.Fill(avisit, 0);
            foreach (int atmIdx in atomIdxs)
                avisit[atmIdx] = -1;

            // first visit marks ring information
            numVisit = 1;
            foreach (int atomIdx in atomIdxs)
            {
                if (avisit[atomIdx] < 0)
                    MarkRings(atomIdx, -1);
            }

            // reset visit flags and generate
            numVisit = 1;
            foreach (int atmIdx in atomIdxs)
                avisit[atmIdx] = -1;

            // second pass builds the expression
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < atomIdxs.Length; i++)
            {
                if (avisit[atomIdxs[i]] < 0)
                {
                    if (i > 0) sb.Append('.');
                    EncodeExpr(atomIdxs[i], -1, sb);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Recursively marks ring closures (back edges) in the <see cref="rbnds"/>
        /// array in a depth first order.
        /// </summary>
        /// <param name="idx">atom index</param>
        /// <param name="bprev">previous bond</param>
        private void MarkRings(int idx, int bprev)
        {
            avisit[idx] = numVisit++;
            int d = deg[idx];
            for (int j = 0; j < d; j++)
            {
                int nbr = atomAdj[idx][j];
                int bidx = bondAdj[idx][j];
                if (avisit[nbr] == 0 || bidx == bprev)
                    continue; // ignored
                else if (avisit[nbr] < 0)
                    MarkRings(nbr, bidx);
                else if (avisit[nbr] < avisit[idx])
                    rbnds[bidx] = -1; // ring closure
            }
        }

        /// <summary>
        /// Recursively encodes a SMARTS expression into the provides
        /// string builder.
        /// </summary>
        /// <param name="idx">atom index</param>
        /// <param name="bprev">previous bond</param>
        /// <param name="sb">destition to write SMARTS to</param>
        private void EncodeExpr(int idx, int bprev, StringBuilder sb)
        {
            avisit[idx] = numVisit++;
            sb.Append(aexpr[idx]);
            int d = deg[idx];

            int remain = d;
            for (int j = 0; j < d; j++)
            {
                int nbr = atomAdj[idx][j];
                int bidx = bondAdj[idx][j];

                // ring open/close
                if (rbnds[bidx] < 0)
                {
                    // open
                    int rnum = ChooseRingNumber();
                    if (rnum > 9) sb.Append('%');
                    sb.Append(rnum);
                    rbnds[bidx] = rnum;
                }
                else if (rbnds[bidx] > 0)
                {
                    // close
                    int rnum = rbnds[bidx];
                    ReleaseRingNumber(rnum);
                    if (rnum > 9) sb.Append('%');
                    sb.Append(rnum);
                }

                if (mode == MODE_EXACT && avisit[nbr] == 0 ||
                    bidx == bprev ||
                    rbnds[bidx] != 0)
                    remain--;
            }

            for (int j = 0; j < d; j++)
            {
                int nbr = atomAdj[idx][j];
                int bidx = bondAdj[idx][j];
                if (mode == MODE_EXACT && avisit[nbr] == 0 ||
                    bidx == bprev ||
                    rbnds[bidx] != 0)
                    continue; // ignored
                remain--;
                if (avisit[nbr] == 0)
                {
                    // peripheral bond
                    if (remain > 0) sb.Append('(');
                    sb.Append(bexpr[bidx]);
                    sb.Append(mol.Atoms[nbr].IsAromatic ? 'a' : '*');
                    if (remain > 0) sb.Append(')');
                }
                else
                {
                    if (remain > 0) sb.Append('(');
                    sb.Append(bexpr[bidx]);
                    EncodeExpr(nbr, bidx, sb);
                    if (remain > 0) sb.Append(')');
                }
            }
        }

        /// <summary>
        /// Select the lowest ring number for use in SMARTS.
        /// </summary>
        /// <returns>ring number</returns>
        /// <exception cref="IllegalStateException">all ring numbers are used</exception>
        private int ChooseRingNumber()
        {
            for (int i = 1; i < rnums.Length; i++)
            {
                if (rnums[i] == 0)
                {
                    rnums[i] = 1;
                    return i;
                }
            }
            throw new InvalidOperationException("No more ring numbers available!");
        }

        /// <summary>
        /// Releases a ring number allowing it to be reused.
        /// </summary>
        /// <param name="rnum">ring number</param>
        private void ReleaseRingNumber(int rnum)
        {
            rnums[rnum] = 0;
        }

        /// <summary>
        /// Encodes the atom at index (atmIdx) to a SMARTS
        /// expression that matches itself.
        /// </summary>
        /// <param name="atmIdx">atom index</param>
        /// <returns>SMARTS atom expression</returns>
        private string EncodeAtomExpr(int atmIdx)
        {
            IAtom atom = mol.Atoms[atmIdx];

            bool complex = mode == MODE_EXACT;

            StringBuilder sb = new StringBuilder();

            switch (atom.AtomicNumber)
            {
                case 0:  // *
                    sb.Append('*');
                    break;
                case 5:  // B
                case 6:  // C
                case 7:  // N
                case 8:  // O
                case 15: // P
                case 16: // S
                case 9:  // F
                case 17: // Cl
                case 35: // Br
                case 53: // I
                    sb.Append(atom.IsAromatic ? atom.Symbol.ToLowerInvariant() : atom.Symbol);
                    break;
                default:
                    complex = true;
                    sb.Append(atom.IsAromatic ? atom.Symbol.ToLowerInvariant() : atom.Symbol);
                    break;
            }

            if (mode == MODE_EXACT)
            {

                int hcount = atom.ImplicitHydrogenCount.Value;
                int valence = hcount;
                int connections = hcount;

                int atmDeg = this.deg[atmIdx];
                for (int i = 0; i < atmDeg; i++)
                {
                    IBond bond = mol.Bonds[bondAdj[atmIdx][i]];
                    IAtom nbr = bond.GetOther(atom);
                    if (nbr.AtomicNumber != null && nbr.AtomicNumber == 1)
                        hcount++;
                    int bord = bond.Order != BondOrder.Unset ? bond.Order.Numeric : 0;
                    if (bord == 0)
                        throw new ArgumentException("Molecule had unsupported zero-order or unset bonds!");
                    valence += bord;
                    connections++;
                }

                sb.Append('H').Append(hcount);
                sb.Append('v').Append(valence);
                sb.Append('X').Append(connections);
            }

            int chg = atom.FormalCharge ?? 0;

            if (chg <= -1 || chg >= +1)
            {
                if (chg >= 0) sb.Append('+');
                else sb.Append('-');
                int abs = Math.Abs(chg);
                if (abs > 1) sb.Append(abs);
                complex = true;
            }
            else if (mode == MODE_EXACT)
            {
                sb.Append("+0");
            }

            return complex ? '[' + sb.ToString() + ']' : sb.ToString();
        }

        /// <summary>
        /// Encodes the bond at index (bondIdx) to a SMARTS
        /// expression that matches itself.
        /// </summary>
        /// <param name="bondIdx">bond index</param>
        /// <param name="beg">atom index of first atom</param>
        /// <param name="end">atom index of second atom</param>
        /// <returns>SMARTS bond expression</returns>
        private string EncodeBondExpr(int bondIdx, int beg, int end)
        {
            IBond bond = mol.Bonds[bondIdx];
            if (bond.Order == BondOrder.Unset)
                return "";

            bool bArom = bond.IsAromatic;
            bool aArom = mol.Atoms[beg].IsAromatic && mol.Atoms[end].IsAromatic;
            switch (bond.Order.Ordinal)
            {
                case BondOrder.O.Single:
                    if (bArom)
                    {
                        return aArom ? "" : ":";
                    }
                    else
                    {
                        return aArom ? "-" : "";
                    }
                case BondOrder.O.Double:
                    return bArom ? "" : "=";
                case BondOrder.O.Triple:
                    return "#";
                default:
                    throw new ArgumentException("Unsupported bond type: " + bond.Order);
            }
        }
    }
}
