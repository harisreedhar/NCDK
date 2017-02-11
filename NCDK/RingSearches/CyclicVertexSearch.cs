/*
 * Copyright (C) 2012 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version. All we ask is that proper credit is given for our
 * work, which includes - but is not limited to - adding the above copyright
 * notice to the beginning of your source code files, and to any copyright
 * notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using NCDK.Graphs;

namespace NCDK.RingSearches
{
    /**
     * Describes a search to identify vertices which belong to elementary cycles and
     * if those cycles are isolated or are part of a fused system. We define a cycle
     * as isolated if it edge disjoint with all other cycles. This corresponds to
     * the isolated and spiro rings of a chemical structures.
     *
     * @author John May
     * @cdk.module core
     * @cdk.githash
     */
    public interface CyclicVertexSearch
    {
        /**
		 * Returns true if the vertex <i>v</i> is in a cycle.
		 *
		 * @param v a vertex identifier by index
		 * @return whether the vertex is in a cycle
		 */
        bool Cyclic(int v);

        /**
		 * Is the edge between the two vertices <i>u</i> and <i>v</i> in a cycle?
		 *
		 * @param u a vertex
		 * @param v another vertex
		 * @return whether the edge is cycle
		 */
        bool Cyclic(int u, int v);

        /**
		 * The set of cyclic vertices.
		 *
		 * @return the cyclic vertices of the molecule.
		 */
        int[] Cyclic();

        /**
		 * Construct the sets of vertices which belong to isolated cycles. Each row
		 * in the array describes a set of cyclic vertices which is edge disjoint
		 * with all other elementary cycles.
		 *
		 * @return vertices belonging to the isolated rings
		 */
        int[][] Isolated();

        /**
		 * Construct the sets of vertices which belong to fused cycle systems (share
		 * at least one edge). Each row in the array describes a set of vertices in
		 * a separate fused system. Each fused system is edge disjoint with every
		 * other fused system.
		 *
		 * @return vertices belonging to the fused cycles
		 */
        int[][] FUsed();

        /**
		 * Build an indexed lookup of vertex color. The vertex color indicates which
		 * cycle a given vertex belongs. If a vertex belongs to more then one cycle
		 * it is colored '0'. If a vertex belongs to no cycle it is colored '-1'.
		 *
		 * @return vertex colors
		 */
        int[] VertexColor();
    }
}

