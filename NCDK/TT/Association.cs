/* Copyright (C) 2004-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace NCDK.Default
{
    /// <summary>
    /// Base class for storing interactions like hydrogen bonds and ionic interactions.
    /// The ElectronContainer contains zero electrons by default.
    /// </summary>
    // @cdk.module extra
    // @cdk.githash
    // @cdk.keyword orbital
    // @cdk.keyword association
    // @cdk.keyword bond
    [Serializable]
    public class Association : ElectronContainer, ICloneable, IChemObjectListener
    {
        /// <summary>
        /// The atoms which take part in the association.
        /// </summary>
        public IList<IAtom> AssociatedAtoms { get; protected set; }

        /// <summary>
        /// Constructs an association between two Atom's.
        /// </summary>
        /// <param name="atom1">An atom to be associated with another atom</param>
        /// <param name="atom2">An atom to be associated with another atom</param>
        /// <seealso cref="Atom"/>
        public Association(IAtom atom1, IAtom atom2)
            : this(new[] { atom1, atom2 })
        { }

        /// <summary>
        /// Constructs an empty association.
        /// </summary>
        /// <seealso cref="Atom"/>
        public Association()
            : this(Array.Empty<IAtom>())
        { }

        public Association(IEnumerable<IAtom> atoms)
        {
            AssociatedAtoms = CreateObservableChemObjectCollection(atoms);
        }

        private ObservableChemObjectCollection<IAtom> CreateObservableChemObjectCollection(IEnumerable<IAtom> objs)
        {
            var list = new ObservableChemObjectCollection<IAtom>(this, objs);
            return list;
        }

        /// <summary>
        /// The number of electrons in a Association.
        /// </summary>
        public override int? ElectronCount => 0;

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("Association(");
            s.Append(this.GetHashCode());
			foreach (var atom in AssociatedAtoms)
            {
                s.Append(", ").Append(atom.ToString());
            }
            s.Append(')');
            return s.ToString();
        }

        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
			NotifyChanged(evt);
        }
    }
}
