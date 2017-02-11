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

using NCDK.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Isomorphisms
{
    /**
     * A predicate for filtering atom-mapping results for those which cover unique
     * edges. This class is intended for use with {@link Pattern}.
     *
     * <blockquote><pre>{@code
     *     Pattern     pattern = Ullmann.FindSubstructure(query);
     *     List<int[]> unique  = FluentIterable.Of(patter.MatchAll(target))
     *                                         .Filter(new UniqueBondMatches())
     *                                         .ToList();
     * }</pre></blockquote>
     *
     * @author John May
     * @cdk.module isomorphism
     */
#if TEST
    public
#endif
    sealed class UniqueBondMatches : NCDK.Common.Base.Predicate<int[]>
    {

        /// <summary>Which mappings have we seen already.</summary>
        private readonly HashSet<ICollection<Tuple>> unique;

        /// <summary>The query graph.</summary>
        private readonly int[][] g;

        /**
         * Create filter for the expected number of unique matches. The number of
         * matches can grow if required.
         *
         * @param expectedHits expected number of unique matches
         */
        private UniqueBondMatches(int[][] g, int expectedHits)
        {
            this.unique = new HashSet<ICollection<Tuple>>(new EQ());
            this.g = g;
        }

        class EQ : IEqualityComparer<ICollection<Tuple>>
        {
            public bool Equals(ICollection<Tuple> x, ICollection<Tuple> y) 
            {
                var lb = new List<Tuple>(y);
                foreach (var aa in x)
                {
                    if (!lb.Remove(aa))
                        return false;
                }
                return true;
            }

            public int GetHashCode(ICollection<Tuple> obj)
                => obj.Sum(n => n.GetHashCode());
        }

        /// <summary>Create filter for unique matches.</summary>
        public UniqueBondMatches(int[][] g)
            : this(g, 10)
        { }

        /// <inheritdoc/>
        public bool Apply(int[] input)
        {
            return unique.Add(ToEdgeSet(input));
        }

        /**
         * Convert a mapping to a bitset.
         *
         * @param mapping an atom mapping
         * @return a bit set of the mapped vertices (values in array)
         */
        private ICollection<Tuple> ToEdgeSet(int[] mapping)
        {
            ICollection<Tuple> edges = new HashSet<Tuple>();
            for (int u = 0; u < g.Length; u++)
            {
                foreach (var v in g[u])
                {
                    edges.Add(new Tuple(mapping[u], mapping[v]));
                }
            }
            return edges;
        }

        /// <summary>Immutable helper class holds two vertices id's.</summary>
        private sealed class Tuple
        {
            /// <summary>Endpoints.</summary>
            int u, v;
            
            /// <summary>
            /// Create the tuple
            /// </summary>
            /// <param name="u">an endpoint</param>
            /// <param name="v">another endpoint</param>
            internal Tuple(int u, int v)
            {
                this.u = u;
                this.v = v;
            }

            public override int GetHashCode()
            {
                return u ^ v;
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                Tuple that = o as Tuple;
                if (that == null)
                    return false;

                return this.u == that.u && this.v == that.v || this.u == that.v && this.v == that.u;
            }
        }
    }
}
