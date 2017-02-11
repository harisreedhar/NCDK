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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;

namespace NCDK.Isomorphisms
{
    /**
	 * A state for the Vento-Foggia (VF) algorithm. The state allows adding and
	 * removing of mappings as well as generating the new candidate mappings {@link
	 * #NextN(int)} and {@link #NextM(int, int)}. The feasibility check is left for
	 * subclasses to implement.
	 *
	 * @author John May
	 * @cdk.module isomorphism
	 */
#if TEST
    public
#endif
    abstract class AbstractVFState : State
    {
        /// <summary>Value indicates a vertex is unmapped.</summary>
#if TEST
        public
#else
        protected
#endif
        const int UNMAPPED = -1;

        /// <summary>Adjacency list representation of the containers.</summary>
#if TEST
        public
#else
        protected
#endif
        readonly int[][] g1, g2;

        /// <summary>Mapping - m1 is the the mapping from g1 to g1, m2 is from g2 to g1.</summary>
#if TEST
        public
#else
        protected
#endif
            readonly int[] m1, m2;

        /// <summary>The (terminal) vertices which are adjacent to each mapped pair.</summary>
#if TEST
        public
#else
        protected
#endif
        readonly int[] t1, t2;

        /// <summary>Size of current solution - the number of vertices matched.</summary>
#if TEST
        public
#else
        protected
#endif
         int size;

        /**
		 * Create a state which will be used to match g1 in g2.
		 *
		 * @param g1 find this graph
		 * @param g2 search this graph
		 */
        public AbstractVFState(int[][] g1, int[][] g2)
        {
            this.g1 = g1;
            this.g2 = g2;
            this.m1 = new int[g1.Length];
            this.m2 = new int[g2.Length];
            this.t1 = new int[g1.Length];
            this.t2 = new int[g2.Length];
            size = 0;
            Arrays.Fill(m1, UNMAPPED);
            Arrays.Fill(m2, UNMAPPED);
        }

        /**
		 * Given the current query candidate (n), find the next candidate. The next
		 * candidate is the next vertex > n (in some ordering) that is unmapped and
		 * is adjacent to a mapped vertex (terminal). If there is no such vertex
		 * (disconnected) the next unmapped vertex is returned. If there are no more
		 * candidates m == |V| of G1.
		 *
		 * @param n previous candidate n
		 * @return the next value of n
		 */

        public sealed override int NextN(int n)
        {
            if (size == 0) return 0;
            for (int i = n + 1; i < g1.Length; i++)
                if (m1[i] == UNMAPPED && t1[i] > 0) return i;
            for (int i = n + 1; i < g1.Length; i++)
                if (m1[i] == UNMAPPED) return i;
            return NMax();
        }

        /**
		 * Given the current target candidate (m), find the next candidate. The next
		 * candidate is the next vertex > m (in some ordering) that is unmapped and
		 * is adjacent to a mapped vertex (terminal). If there is no such vertex
		 * (disconnected) the next unmapped vertex is returned. If there are no more
		 * candidates m == |V| of G2.
		 *
		 * @param m previous candidate m
		 * @return the next value of m
		 */

        public sealed override int NextM(int n, int m)
        {
            if (size == 0) return m + 1;
            // if the query vertex 'n' is in the terminal set (t1) then the
            // target vertex must be in the terminal set (t2)
            for (int i = m + 1; i < g2.Length; i++)
                if (m2[i] == UNMAPPED && (t1[n] == 0 || t2[i] > 0)) return i;
            return MMax();
        }

        /// <inheritdoc/>

        public sealed override int NMax()
        {
            return g1.Length;
        }

        /// <inheritdoc/>

        public sealed override int MMax()
        {
            return g2.Length;
        }

        /// <inheritdoc/>

        public sealed override bool Add(int n, int m)
        {
            if (!Feasible(n, m)) return false;
            m1[n] = m;
            m2[m] = n;
            size = size + 1;
            foreach (var w in g1[n])
                if (t1[w] == 0) t1[w] = size;
            foreach (var w in g2[m])
                if (t2[w] == 0) t2[w] = size;
            return true;
        }

        /// <inheritdoc/>

        public sealed override void Remove(int n, int m)
        {
            m1[n] = m2[m] = UNMAPPED;
            size = size - 1;
            foreach (var w in g1[n])
                if (t1[w] > size) t1[w] = 0;
            foreach (var w in g2[m])
                if (t2[w] > size) t2[w] = 0;
        }

        /**
		 * Is the candidate pair {n, m} feasible. Verifies if the adding candidate
		 * pair {n, m} to the state would lead to an invalid mapping.
		 *
		 * @param n query vertex
		 * @param m target vertex
		 * @return the mapping is feasible
		 */
        public abstract bool Feasible(int n, int m);

        /// <inheritdoc/>

        public sealed override int[] Mapping()
        {
            return Arrays.CopyOf(m1, m1.Length);
        }

        /// <inheritdoc/>

        public sealed override int Count => size;
    }
}
