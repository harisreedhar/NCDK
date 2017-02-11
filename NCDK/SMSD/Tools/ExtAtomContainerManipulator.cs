
using NCDK.Aromaticities;
using NCDK.AtomTypes;
using NCDK.Default;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NCDK.Numerics;
/**
*
* Copyright (C) 2006-2010  Syed Asad Rahman {asad@ebi.ac.uk}
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
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU Lesser General Public License for more details.
*
* You should have received atom copy of the GNU Lesser General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
*/
namespace NCDK.SMSD.Tools
{
    /**
     * Class that handles some customised features for SMSD atom containers.
     * <p>This is an extension of CDK AtomContainer.
     * Some part of this code was taken from CDK source code and modified.</p>
     *
     * @cdk.module smsd
     * @cdk.githash
     * @author Syed Asad Rahman <asad@ebi.ac.uk>
     */
    public class ExtAtomContainerManipulator : AtomContainerManipulator
    {
        private static void PrintAtoms(IAtomContainer mol)
        {
            Console.Out.Write("Atom: ");
            foreach (var a in mol.Atoms)
            {

                Console.Out.Write(a.Symbol);
                Console.Out.Write("[" + a.FormalCharge + "]");
                if (a.Id != null)
                {
                    Console.Out.Write("[" + a.Id + "]");
                }

            }
            Console.Out.WriteLine();
            Console.Out.WriteLine();
        }

        /**
         * Retrurns deep copy of the molecule
         * @param container
         * @return deep copy of the mol
         */
        public static IAtomContainer MakeDeepCopy(IAtomContainer container)
        {
            IAtomContainer newAtomContainer = (IAtomContainer)container.Clone();
            newAtomContainer.NotifyChanged();
            return newAtomContainer;
        }

        /**
         * This function finds rings and uses aromaticity detection code to
         * aromatize the molecule.
         * @param mol input molecule
         */
        public static void AromatizeMolecule(IAtomContainer mol)
        {

            // need to find rings and aromaticity again since added H's

            IRingSet ringSet = null;
            try
            {
                AllRingsFinder arf = new AllRingsFinder();
                ringSet = arf.FindAllRings(mol);

                // SSSRFinder s = new SSSRFinder(atomContainer);
                // srs = s.FindEssentialRings();

            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.StackTrace);
            }

            try
            {
                // figure out which atoms are in aromatic rings:
                //            PrintAtoms(atomContainer);
                ExtAtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(mol);
                //            PrintAtoms(atomContainer);
                Aromaticity.CDKLegacy.Apply(mol);
                //            PrintAtoms(atomContainer);
                // figure out which rings are aromatic:
                RingSetManipulator.MarkAromaticRings(ringSet);
                //            PrintAtoms(atomContainer);
                // figure out which simple (non cycles) rings are aromatic:
                // HueckelAromaticityDetector.DetectAromaticity(atomContainer, srs);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.StackTrace);
            }

            // only atoms in 6 membered rings are aromatic
            // determine largest ring that each atom is atom part of

            for (int i = 0; i <= mol.Atoms.Count - 1; i++)
            {

                mol.Atoms[i].IsAromatic = false;

                foreach (var ring in ringSet)
                {
                    if (!ring.IsAromatic)
                    {
                        continue;
                    }

                    bool haveatom = ring.Contains(mol.Atoms[i]);

                    //Debug.WriteLine("haveatom="+haveatom);

                    if (haveatom && ring.Atoms.Count == 6)
                    {
                        mol.Atoms[i].IsAromatic = true;
                    }
                }
            }
        }

        /**
         * Returns The number of explicit hydrogens for a given IAtom.
         * @param atomContainer
         * @param atom
         * @return The number of explicit hydrogens on the given IAtom.
         */
        public static int GetExplicitHydrogenCount(IAtomContainer atomContainer, IAtom atom)
        {
            int hCount = 0;
            foreach (var iAtom in atomContainer.GetConnectedAtoms(atom))
            {
                IAtom connectedAtom = iAtom;
                if (connectedAtom.Symbol.Equals("H"))
                {
                    hCount++;
                }
            }
            return hCount;
        }

        /**
         * Returns The number of Implicit Hydrogen Count for a given IAtom.
         * @param atom
         * @return Implicit Hydrogen Count
         */
        public static int GetImplicitHydrogenCount(IAtom atom)
        {
            return atom.ImplicitHydrogenCount ?? 0;
        }

        /**
         * The summed implicit + explicit hydrogens of the given IAtom.
         * @param atomContainer
         * @param atom
         * @return The summed implicit + explicit hydrogens of the given IAtom.
         */
        public static int GetHydrogenCount(IAtomContainer atomContainer, IAtom atom)
        {
            return GetExplicitHydrogenCount(atomContainer, atom) + GetImplicitHydrogenCount(atom);
        }

        /**
         * Returns IAtomContainer without Hydrogen. If an AtomContainer has atom single atom which
         * is atom Hydrogen then its not removed.
         * @param atomContainer
         * @return IAtomContainer without Hydrogen. If an AtomContainer has atom single atom which
         * is atom Hydrogen then its not removed.
         */
        public static IAtomContainer RemoveHydrogensExceptSingleAndPreserveAtomID(IAtomContainer atomContainer)
        {
            var map = new CDKObjectMap(); // maps original object to clones.
            if (atomContainer.Bonds.Count > 0)
            {
                var mol = (IAtomContainer)atomContainer.Clone(map);
                List<IAtom> remove = new List<IAtom>(); // lists removed Hs.
                foreach (var atom in atomContainer.Atoms)
                {
                    if (atom.Symbol.Equals("H"))
                        remove.Add(atom);
                }
                foreach (var a in remove)
                    mol.RemoveAtomAndConnectedElectronContainers(map.AtomMap[a]);
                foreach (var atom in mol.Atoms.Where(n => !n.ImplicitHydrogenCount.HasValue))
                    atom.ImplicitHydrogenCount = 0;
                //            Recompute hydrogen counts of neighbours of removed Hydrogens.
                mol = ReComputeHydrogens(mol, atomContainer, remove, map);
                return mol;
            }
            else
            {
                var mol = (IAtomContainer)atomContainer.Clone(map);
                if (string.Equals(atomContainer.Atoms[0].Symbol, "H", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("WARNING: single hydrogen atom removal not supported!");
                }
                return mol;
            }
        }

        /**
         * Returns IAtomContainer without Hydrogen. If an AtomContainer has atom single atom which
         * is atom Hydrogen then its not removed.
         * @param atomContainer
         * @return IAtomContainer without Hydrogen. If an AtomContainer has atom single atom which
         * is atom Hydrogen then its not removed.
         */
        public static IAtomContainer ConvertExplicitToImplicitHydrogens(IAtomContainer atomContainer)
        {
            IAtomContainer mol = (IAtomContainer)atomContainer.Clone();
            ConvertImplicitToExplicitHydrogens(mol);
            if (mol.Atoms.Count > 1)
            {
                mol = RemoveHydrogens(mol);
            }
            else if (string.Equals(mol.Atoms.First().Symbol, "H", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("WARNING: single hydrogen atom removal not supported!");
            }
            return mol;
        }

        /**
         * Convenience method to perceive atom types for all <code>IAtom</code>s in the
         * <code>IAtomContainer</code>, using the <code>CDKAtomTypeMatcher</code>. If the
         * matcher finds atom matching atom type, the <code>IAtom</code> will be configured
         * to have the same properties as the <code>IAtomType</code>. If no matching atom
         * type is found, no configuration is performed.
         * @param container
         * @throws CDKException
         */
        public static new void PercieveAtomTypesAndConfigureAtoms(IAtomContainer container)
        {
            CDKAtomTypeMatcher matcher = CDKAtomTypeMatcher.GetInstance(container.Builder);
            foreach (var atom in container.Atoms)
            {
                if (!(atom is IPseudoAtom))
                {

                    IAtomType matched = matcher.FindMatchingAtomType(container, atom);
                    if (matched != null)
                    {
                        AtomTypeManipulator.Configure(atom, matched);
                    }

                }
            }
        }

        private static IAtomContainer ReComputeHydrogens(IAtomContainer mol, IAtomContainer atomContainer,
                List<IAtom> remove, CDKObjectMap map)
        {
            // Recompute hydrogen counts of neighbours of removed Hydrogens.
            foreach (var aRemove in remove)
            {
                // Process neighbours.
                foreach (var iAtom in atomContainer.GetConnectedAtoms(aRemove))
                {
                    IAtom neighb = map.AtomMap[iAtom];
                    if (neighb == null)
                    {
                        continue; // since for the case of H2, neight H has atom heavy atom neighbor
                    }
                    //Added by Asad
                    if (!(neighb is IPseudoAtom))
                    {
                        neighb.ImplicitHydrogenCount = (neighb.ImplicitHydrogenCount ?? 0) + 1;
                    }
                    else
                    {
                        neighb.ImplicitHydrogenCount = 0;
                    }
                }
            }
            return mol;
        }
    }
}
