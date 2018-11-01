/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.QSAR.Results;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// IDescriptor based on the number of atoms of a certain element type.
    /// </summary>
    /// <remarks>
    /// It is possible to use the wild card symbol '*' as element type to get the count of all atoms.
    /// <para>This descriptor uses these parameters:
    /// <list type="table">
    ///   <item>
    ///     <term>Name</term>
    ///     <term>Default</term>
    ///     <term>Description</term>
    ///   </item>
    ///   <item>
    ///     <term>elementName</term>
    ///     <term>*</term>
    ///     <term>Symbol of the element we want to count</term>
    ///   </item>
    /// </list>
    /// </para>
    /// Returns a single value with name <i>nX</i> where <i>X</i> is the atomic symbol.  If *
    /// is specified then the name is <i>nAtom</i>
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.githash
    // @cdk.dictref qsar-descriptors:atomCount
    public class AtomCountDescriptor : AbstractMolecularDescriptor, IMolecularDescriptor
    {
        private string elementName = "*";

        public AtomCountDescriptor()
        {
            elementName = "*";
        }

        /// <inheritdoc/>
        public override IImplementationSpecification Specification => specification;
        private static readonly DescriptorSpecification specification =
            new DescriptorSpecification(
                "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomCount",
                typeof(AtomCountDescriptor).FullName,
                "The Chemistry Development Kit");

        public override IReadOnlyList<object> Parameters
        {
            set
            {
                if (value.Count > 1)
                {
                    throw new CDKException("AtomCount only expects one parameter");
                }
                if (!(value[0] is string))
                {
                    throw new CDKException("The parameter must be of type string");
                }
                elementName = (string)value[0];
            }
            get
            {
                // return the parameters as used for the descriptor calculation
                return new object[] { elementName };
            }
        }

        public override IReadOnlyList<string> DescriptorNames
        {
            get
            {
                string name = "n";
                if (string.Equals(elementName, "*", StringComparison.Ordinal))
                    name = "nAtom";
                else
                    name += elementName;
                return new string[] { name };
            }
        }

        /// <summary>
        /// This method calculate the number of atoms of a given type in an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">The atom container for which this descriptor is to be calculated</param>
        /// <returns>Number of atoms of a certain type is returned.</returns>
        // it could be interesting to accept as elementName a SMARTS atom, to get the frequency of this atom
        // this could be useful for other descriptors like polar surface area...
        public DescriptorValue<Result<int>> Calculate(IAtomContainer container)
        {
            int atomCount = 0;

            if (container == null)
            {
                return new DescriptorValue<Result<int>>(specification, ParameterNames, Parameters,
                    new Result<int>(0), DescriptorNames,
                    new CDKException("The supplied AtomContainer was NULL"));
            }

            if (container.Atoms.Count == 0)
            {
                return new DescriptorValue<Result<int>>(specification, ParameterNames, Parameters,
                    new Result<int>(0), DescriptorNames, new CDKException(
                        "The supplied AtomContainer did not have any atoms"));
            }

            if (string.Equals(elementName, "*", StringComparison.Ordinal))
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    // we assume that UNSET is equivalent to 0 implicit H's
                    var hcount = container.Atoms[i].ImplicitHydrogenCount;
                    if (hcount != null)
                        atomCount += hcount.Value;
                }
                atomCount += container.Atoms.Count;
            }
            else if (elementName.Equals("H", StringComparison.Ordinal))
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    if (container.Atoms[i].Symbol.Equals(elementName, StringComparison.Ordinal))
                    {
                        atomCount += 1;
                    }
                    else
                    {
                        // we assume that UNSET is equivalent to 0 implicit H's
                        var hcount = container.Atoms[i].ImplicitHydrogenCount;
                        if (hcount != null) atomCount += hcount.Value;
                    }
                }
            }
            else
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    if (container.Atoms[i].Symbol.Equals(elementName, StringComparison.Ordinal))
                    {
                        atomCount += 1;
                    }
                }
            }

            return new DescriptorValue<Result<int>>(specification, ParameterNames, Parameters, new Result<int>(
                    atomCount), DescriptorNames);
        }

        /// <inheritdoc/>
        public override IDescriptorResult DescriptorResultType { get; } = new Result<int>(1);

        public override IReadOnlyList<string> ParameterNames { get; } = new string[] { "elementName" };

        public override object GetParameterType(string name) => "";

        IDescriptorValue IMolecularDescriptor.Calculate(IAtomContainer container) => Calculate(container);
    }
}
