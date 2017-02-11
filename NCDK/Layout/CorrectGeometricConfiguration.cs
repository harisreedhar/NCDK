/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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
using NCDK.Common.Mathematics;
using NCDK.Graphs;
using NCDK.RingSearches;
using System;
using System.Collections.Generic;
using System.Linq;
using NCDK.Numerics;

namespace NCDK.Layout
{
    /**
     * Correct double-bond configuration depiction in 2D to be correct for it's
     * specified {@link IDoubleBondStereochemistry}. Ideally double-bond adjustment
     * should be done in when generating a structure diagram (and consider
     * overlaps). This method finds double bonds with incorrect depicted
     * configuration and reflects one side to correct the configuration.
     * <b>IMPORTANT: should be invoked before labelling up/down bonds. Cyclic
     * double-bonds with a configuration can not be corrected (error logged).</b>
     *
     * @author John May
     * @cdk.module sdg
     */
#if TEST
    public
#endif
    sealed class CorrectGeometricConfiguration
    {

        /// <summary>The structure we are assigning labels to.</summary>
        private readonly IAtomContainer container;

        /// <summary>Adjacency list graph representation of the structure.</summary>
        private readonly int[][] graph;

        /// <summary>Lookup atom index (avoid IAtomContainer).</summary>
        private readonly IDictionary<IAtom, int> atomToIndex;

        /// <summary>Test if a bond is cyclic.</summary>
        private readonly RingSearch ringSearch;

        /// <summary>Visited flags when atoms are being reflected.</summary>
        private readonly bool[] visited;

        /**
         * Adjust all double bond elements in the provided structure. <b>IMPORTANT:
         * up/down labels should be adjusted before adjust double-bond
         * configurations. coordinates are reflected by this method which can lead
         * to incorrect tetrahedral specification.</b>
         *
         * @param container the structure to adjust
         * @throws ArgumentException an atom had unset coordinates
         */
        public static IAtomContainer Correct(IAtomContainer container)
        {
            if (container.StereoElements.Any())
                new CorrectGeometricConfiguration(container);
            return container;
        }

        /**
         * Adjust all double bond elements in the provided structure.
         *
         * @param container the structure to adjust
         * @throws ArgumentException an atom had unset coordinates
         */
        CorrectGeometricConfiguration(IAtomContainer container)
            : this(container, GraphUtil.ToAdjList(container))
        { }

        /**
         * Adjust all double bond elements in the provided structure.
         *
         * @param container the structure to adjust
         * @param graph     the adjacency list representation of the structure
         * @throws ArgumentException an atom had unset coordinates
         */
        CorrectGeometricConfiguration(IAtomContainer container, int[][] graph)
        {
            this.container = container;
            this.graph = graph;
            this.visited = new bool[graph.Length];
            this.atomToIndex = new Dictionary<IAtom, int>();
            this.ringSearch = new RingSearch(container, graph);

            for (int i = 0; i < container.Atoms.Count; i++)
            {
                IAtom atom = container.Atoms[i];
                atomToIndex[atom] = i;
                if (atom.Point2D == null) throw new ArgumentException("atom " + i + " had unset coordinates");
            }

            foreach (var element in container.StereoElements)
            {
                if (element is IDoubleBondStereochemistry)
                {
                    Adjust((IDoubleBondStereochemistry)element);
                }
            }
        }

        /**
         * Adjust the configuration of the {@code dbs} element (if required).
         *
         * @param dbs double-bond stereochemistry element
         */
        private void Adjust(IDoubleBondStereochemistry dbs)
        {

            IBond db = dbs.StereoBond;
            var bonds = dbs.Bonds;

            IAtom left = db.Atoms[0];
            IAtom right = db.Atoms[1];

            int p = Parity(dbs);
            int q = Parity(GetAtoms(left, bonds[0].GetConnectedAtom(left), right))
                    * Parity(GetAtoms(right, bonds[1].GetConnectedAtom(right), left));

            // configuration is unspecified? then we add an unspecified bond.
            // note: IDoubleBondStereochemistry doesn't indicate this yet
            if (p == 0)
            {
                foreach (var bond in container.GetConnectedBonds(left))
                    bond.Stereo = BondStereo.None;
                foreach (var bond in container.GetConnectedBonds(right))
                    bond.Stereo = BondStereo.None;
                bonds[0].Stereo = BondStereo.UpOrDown;
                return;
            }

            // configuration is already correct
            if (p == q) return;

            Arrays.Fill(visited, false);
            visited[atomToIndex[left]] = true;

            // XXX: bad but correct layout
            if (ringSearch.Cyclic(atomToIndex[left], atomToIndex[right]))
            {
                Arrays.Fill(visited, true);
                foreach (var w in graph[atomToIndex[right]])
                {
                    Reflect(w, db);
                }
                return;
            }

            foreach (var w in graph[atomToIndex[right]])
            {
                if (!visited[w]) Reflect(w, db);
            }
        }

        /**
         * Create an array of three atoms for a side of the double bond. This is
         * used to determine the 'winding' of one side of the double bond.
         *
         * @param focus       a double bonded atom
         * @param substituent the substituent we know the configuration of
         * @param otherFocus  the other focus (i.e. the atom focus is double bonded
         *                    to)
         * @return 3 atoms arranged as, substituent, other substituent and other
         *         focus. if the focus atom has an implicit hydrogen the other
         *         substituent is the focus.
         */
        private IAtom[] GetAtoms(IAtom focus, IAtom substituent, IAtom otherFocus)
        {
            IAtom otherSubstituent = focus;
            foreach (var w in graph[atomToIndex[focus]])
            {
                IAtom atom = container.Atoms[w];
                if (atom != substituent && atom != otherFocus) otherSubstituent = atom;
            }
            return new IAtom[] { substituent, otherSubstituent, otherFocus };
        }

        /**
         * Access the parity (odd/even) parity of the double bond configuration (
         * together/opposite).
         *
         * @param element double bond element
         * @return together = -1, opposite = +1
         */
        private static int Parity(IDoubleBondStereochemistry element)
        {
            switch (element.Stereo.Ordinal)
            {
                case DoubleBondConformation.O.Together:
                    return -1;
                case DoubleBondConformation.O.Opposite:
                    return +1;
                default:
                    return 0;
            }
        }

        /**
         * Determine the parity (odd/even) of the triangle formed by the 3 atoms.
         *
         * @param atoms array of 3 atoms
         * @return the parity of the triangle formed by 3 points, odd = -1, even =
         *         +1
         */
        private static int Parity(IAtom[] atoms)
        {
            return Parity(atoms[0].Point2D.Value, atoms[1].Point2D.Value, atoms[2].Point2D.Value);
        }

        /**
         * Determine the parity of the triangle formed by the 3 coordinates a, b and
         * c.
         *
         * @param a point 1
         * @param b point 2
         * @param c point 3
         * @return the parity of the triangle formed by 3 points
         */
        private static int Parity(Vector2 a, Vector2 b, Vector2 c)
        {
            double det = (a.X - c.X) * (b.Y - c.Y) - (a.Y - c.Y) * (b.X - c.X);
            return (int)Math.Sign(det);
        }

        /**
         * Reflect the atom at index {@code v} and any then reflect any unvisited
         * neighbors.
         *
         * @param v    index of the atom to reflect
         * @param bond bond
         */
        private void Reflect(int v, IBond bond)
        {
            visited[v] = true;
            IAtom atom = container.Atoms[v];
            atom.Point2D = Reflect(atom.Point2D.Value, bond);
            foreach (var w in graph[v])
            {
                if (!visited[w]) Reflect(w, bond);
            }
        }

        /**
         * Reflect the point {@code p} over the {@code bond}.
         *
         * @param p    the point to reflect
         * @param bond bond
         * @return the reflected point
         */
        private Vector2 Reflect(Vector2 p, IBond bond)
        {
            IAtom a = bond.Atoms[0];
            IAtom b = bond.Atoms[1];
            return Reflect(p, a.Point2D.Value.X, a.Point2D.Value.Y, b.Point2D.Value.X, b.Point2D.Value.Y);
        }

        /**
         * Reflect the point {@code p} in the line (x0,y0 - x1,y1).
         *
         * @param p  the point to reflect
         * @param x0 plane x start
         * @param y0 plane y end
         * @param x1 plane x start
         * @param y1 plane y end
         * @return the reflected point
         */
        private Vector2 Reflect(Vector2 p, double x0, double y0, double x1, double y1)
        {
            double dx, dy, a, b;

            dx = (x1 - x0);
            dy = (y1 - y0);

            a = (dx * dx - dy * dy) / (dx * dx + dy * dy);
            b = 2 * dx * dy / (dx * dx + dy * dy);

            return new Vector2(a * (p.X - x0) + b * (p.Y - y0) + x0, b * (p.X - x0) - a * (p.Y - y0) + y0);
        }
    }
}
