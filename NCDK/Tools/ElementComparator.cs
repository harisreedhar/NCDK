/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *  */
using System;
using System.Collections.Generic;

namespace NCDK.Tools
{
    /**
      * Compares elements based on the order commonly used in
      * molecular formula. Order:
      * C, H, other elements in alphabetic order.
      *
      * @cdk.module standard
     * @cdk.githash
      *
      * @cdk.keyword element, sorting
      */
    public class ElementComparator : IComparer<string>
    {

        private const string H_ELEMENT_SYMBOL = "H";
        private const string C_ELEMENT_SYMBOL = "C";

        /**
         * Returns a negative if o1 comes before o2 in a molecular formula,
         * returns zero if they are identical, and positive if o1 comes
         * after o2 in the formula.
         */

        public int Compare(string o1, string o2)
        {
            if (C_ELEMENT_SYMBOL.Equals(o1))
            {
                if (C_ELEMENT_SYMBOL.Equals(o2))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (H_ELEMENT_SYMBOL.Equals(o1))
            {
                if (C_ELEMENT_SYMBOL.Equals(o2))
                {
                    return 1;
                }
                else if (H_ELEMENT_SYMBOL.Equals(o2))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (C_ELEMENT_SYMBOL.Equals(o2) || H_ELEMENT_SYMBOL.Equals(o2))
                {
                    return 1;
                }
                else
                {
                    return string.Compare((string)o1, (string)o2, StringComparison.Ordinal);
                }
            }
        }
    }
}
