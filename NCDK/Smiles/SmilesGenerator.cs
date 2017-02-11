/* Copyright (C) 2002-2007  Oliver Horlacher
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Beam;
using NCDK.Graphs;
using NCDK.Graphs.Canon;
using NCDK.Graphs.Invariant;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NCDK.Smiles
{
    /**
     * Generate a SMILES {@cdk.cite WEI88, WEI89} string for a provided structure.
     * The generator can produce several <i>flavour</i> of SMILES.
     * <p/>
     * <ul>
     *     <li>generic - non-canonical SMILES string, different atom ordering
     *         produces different SMILES. No isotope or stereochemistry encoded.
     *         </li>
     *     <li>unique - canonical SMILES string, different atom ordering
     *         produces the same* SMILES. No isotope or stereochemistry encoded.
     *         </li>
     *     <li>isomeric - non-canonical SMILES string, different atom ordering
     *         produces different SMILES. Isotope and stereochemistry is encoded.
     *         </li>
     *     <li>absolute - canonical SMILES string, different atom ordering
     *         produces the same SMILES. Isotope and stereochemistry is encoded.</li>
     * </ul>
     *
     * <p/>
     * A generator instance is created using one of the static methods, the SMILES
     * are then created by invoking {@link #Create(IAtomContainer)}.
     * <blockquote><pre>
     * IAtomContainer  ethanol = ...;
     * SmilesGenerator sg      = SmilesGenerator.Generic();
     * string          smi     = sg.Create(ethanol); // CCO or OCC
     *
     * SmilesGenerator sg      = SmilesGenerator.Unique();
     * string          smi     = sg.Create(ethanol); // only CCO
     * </pre></blockquote>
     *
     * <p/>
     *
     * The isomeric and absolute generator encode tetrahedral and double bond
     * stereochemistry using {@link IStereoElement}s
     * provided on the <see cref="IAtomContainer"/>. If stereochemistry is not being
     * written it may need to be determined from 2D/3D coordinates using
     * {@link org.openscience.cdk.stereo.StereoElementFactory}.
     *
     * <p/>
     *
     * By default the generator will not write aromatic SMILES. Kekulé SMILES are
     * generally preferred for compatibility and aromaticity can easily be
     * reperceived. Modifying a generator to produce {@link #Aromatic()} SMILES
     * will use the {@link org.openscience.cdk.CDKConstants#ISAROMATIC} flags.
     * These flags can be set manually or with the
     * {@link org.openscience.cdk.aromaticity.Aromaticity} utility.
     * <blockquote><pre>
     * IAtomContainer  benzene = ...;
     *
     * // with no flags set the output is always kekule
     * SmilesGenerator sg      = SmilesGenerator.Generic();
     * string          smi     = sg.Create(benzene); // C1=CC=CC=C1
     *
     * SmilesGenerator sg      = SmilesGenerator.Generic()
     *                                          .Aromatic();
     * string          smi     = sg.Create(ethanol); // C1=CC=CC=C1
     *
     * foreach (var a in benzene.Atoms)
     *     a.IsAromatic = true;
     * foreach (var b in benzene.bond())
     *     b.IsAromatic = true;
     *
     * // with flags set, the aromatic generator encodes this information
     * SmilesGenerator sg      = SmilesGenerator.Generic();
     * string          smi     = sg.Create(benzene); // C1=CC=CC=C1
     *
     * SmilesGenerator sg      = SmilesGenerator.Generic()
     *                                          .Aromatic();
     * string          smi     = sg.Create(ethanol); // c1ccccc1
     * </pre></blockquote>
     * <p/>
     * By default atom classes are not written. Atom classes can be written but
     * creating a generator {@link #WithAtomClasses()}.
     *
     * <blockquote><pre>
     * IAtomContainer  benzene = ...;
     *
     * // see CDKConstants for property key
     * benzene.Atoms[3]
     *        .SetProperty(ATOM_ATOM_MAPPING, 42);
     *
     * SmilesGenerator sg      = SmilesGenerator.Generic();
     * string          smi     = sg.Create(benzene); // C1=CC=CC=C1
     *
     * SmilesGenerator sg      = SmilesGenerator.Generic()
     *                                          .WithAtomClasses();
     * string          smi     = sg.Create(ethanol); // C1=CC=[CH:42]C=C1
     * </pre></blockquote>
     * <p/>
     *
     * Auxiliary data can be stored with SMILES by knowing the output order of
     * atoms. The following example demonstrates the storage of 2D coordinates.
     *
     * <blockquote><pre>
     * IAtomContainer  mol = ...;
     * SmilesGenerator sg  = SmilesGenerator.Generic();
     *
     * int   n     = mol.Atoms.Count;
     * int[] order = new int[n];
     *
     * // the order array is filled up as the SMILES is generated
     * string smi = sg.Create(mol, order);
     *
     * // load the coordinates array such that they are in the order the atoms
     * // are read when parsing the SMILES
     * Vector2[] coords = new Vector2[mol.Atoms.Count];
     * for (int i = 0; i < coords.Length; i++)
     *     coords[order[i]] = container.Atoms[i].Point2D;
     *
     * // SMILES string suffixed by the coordinates
     * string smi2d = smi + " " + Arrays.ToString(coords);
     *
     * </pre></blockquote>
     *
     * * the unique SMILES generation uses a fast equitable labelling procedure
     *   and as such there are some structures which may not be unique. The number
     *   of such structures is generally minimal.
     *
     * @author         Oliver Horlacher
     * @author         Stefan Kuhn (chiral smiles)
     * @author         John May
     * @cdk.keyword    SMILES, generator
     * @cdk.module     smiles
     * @cdk.githash
     *
     * @see org.openscience.cdk.aromaticity.Aromaticity
     * @see org.openscience.cdk.stereo.Stereocenters
     * @see org.openscience.cdk.stereo.StereoElementFactory
     * @see ITetrahedralChirality
     * @see IDoubleBondStereochemistry
     * @see org.openscience.cdk.CDKConstants
     * @see SmilesParser
     */
    public sealed class SmilesGenerator
    {

        private readonly bool isomeric, canonical, aromatic, classes;
        private readonly CDKToBeam converter;

        /**
		 * Create the generic SMILES generator.
		 * @see #Generic()
		 */
        public SmilesGenerator()
            : this(false, false, false, false)
        {
        }

        /**
		 * Create the SMILES generator.
		 *
		 * @param isomeric include isotope and stereo configurations in produced
		 *                 SMILES
		 */
        private SmilesGenerator(bool isomeric, bool canonical, bool aromatic, bool classes)
        {
            this.isomeric = isomeric;
            this.canonical = canonical;
            this.aromatic = aromatic;
            this.classes = classes;
            this.converter = new CDKToBeam(isomeric, aromatic, classes);
        }

        /**
		 * The generator should write aromatic (lower-case) SMILES. This option is
		 * not recommended as different parsers can interpret where bonds should be
		 * placed.
		 *
		 * <blockquote><pre>
		 * IAtomContainer  container = ...;
		 * SmilesGenerator smilesGen = SmilesGenerator.Unique()
		 *                                            .Aromatic();
		 * smilesGen.CreateSMILES(container);
		 * </pre></blockquote>
		 *
		 * @return a generator for aromatic SMILES
		 */
        public SmilesGenerator Aromatic()
        {
            return new SmilesGenerator(isomeric, canonical, true, classes);
        }

        /**
		 * Specifies that the generator should write atom classes in SMILES. Atom
		 * classes are provided by the {@link org.openscience.cdk.CDKConstants#ATOM_ATOM_MAPPING}
		 * property. This method returns a new SmilesGenerator to use.
		 *
		 * <blockquote><pre>
		 * IAtomContainer  container = ...;
		 * SmilesGenerator smilesGen = SmilesGenerator.Unique()
		 *                                            .AtomClasses();
		 * smilesGen.CreateSMILES(container); // C[CH2:4]O second atom has class = 4
		 * </pre></blockquote>
		 *
		 * @return a generator for SMILES with atom classes
		 */
        public SmilesGenerator WithAtomClasses()
        {
            return new SmilesGenerator(isomeric, canonical, aromatic, true);
        }

        /**
		 * Create a generator for generic SMILES. Generic SMILES are
		 * non-canonical and useful for storing information when it is not used
		 * as an index (i.e. unique keys). The generated SMILES is dependant on
		 * the input order of the atoms.
		 *
		 * @return a new arbitrary SMILES generator
		 */
        public static SmilesGenerator Generic()
        {
            return new SmilesGenerator(false, false, false, false);
        }

        /**
		 * Convenience method for creating an isomeric generator. Isomeric SMILES
		 * are non-unique but contain isotope numbers (e.g. {@code [13C]}) and
		 * stereo-chemistry.
		 *
		 * @return a new isomeric SMILES generator
		 */
        public static SmilesGenerator Isomeric()
        {
            return new SmilesGenerator(true, false, false, false);
        }

        /**
		 * Create a unique SMILES generator. Unique SMILES use a fast canonisation
		 * algorithm but does not encode isotope or stereo-chemistry.
		 *
		 * @return a new unique SMILES generator
		 */
        public static SmilesGenerator Unique()
        {
            return new SmilesGenerator(false, true, false, false);
        }

        /**
		 * Create a absolute SMILES generator. Unique SMILES uses the InChI to
		 * canonise SMILES and encodes isotope or stereo-chemistry. The InChI
		 * module is not a dependency of the SMILES module but should be present
		 * on the classpath when generation absolute SMILES.
		 *
		 * @return a new absolute SMILES generator
		 */
        public static SmilesGenerator CreateAbsolute()
        {
            return new SmilesGenerator(true, true, false, false);
        }

        /**
		 * Create a SMILES string for the provided molecule.
		 *
		 * @param molecule the molecule to create the SMILES of
		 * @return a SMILES string
		 * @ SMILES could not be generated
		 * @deprecated use #create
		 */
        [Obsolete]
        public string CreateSMILES(IAtomContainer molecule)
        {
            try
            {
                return Create(molecule);
            }
            catch (CDKException e)
            {
                throw new ArgumentException(
                        "SMILES could not be generated, please use the new API method 'Create()'"
                                + "to catch the checked exception", e);
            }
        }

        /**
		 * Create a SMILES string for the provided reaction.
		 *
		 * @param reaction the reaction to create the SMILES of
		 * @return a reaction SMILES string
		 * @ SMILES could not be generated
		 * @deprecated use #CreateReactionSMILES
		 */
        [Obsolete]
        public string CreateSMILES(IReaction reaction)
        {
            try
            {
                return CreateReactionSMILES(reaction);
            }
            catch (CDKException e)
            {
                throw new ArgumentException(
                        "SMILES could not be generated, please use the new API method 'Create()'"
                                + "to catch the checked exception", e);
            }
        }

        /**
		 * Generate SMILES for the provided {@code molecule}.
		 *
		 * @param molecule The molecule to evaluate
		 * @return the SMILES string
		 * @ SMILES could not be created
		 */
        public string Create(IAtomContainer molecule)
        {
            return Create(molecule, new int[molecule.Atoms.Count]);
        }

        /**
		 * Create a SMILES string and obtain the order which the atoms were
		 * written. The output order allows one to arrange auxiliary atom data in the
		 * order that a SMILES string will be read. A simple example is seen below
		 * where 2D coordinates are stored with a SMILES string. In reality a more
		 * compact binary encoding would be used instead of printing the coordinates
		 * as a string.
		 *
		 * <blockquote><pre>
		 * IAtomContainer  mol = ...;
		 * SmilesGenerator sg  = SmilesGenerator.Generic();
		 *
		 * int   n     = mol.Atoms.Count;
		 * int[] order = new int[n];
		 *
		 * // the order array is filled up as the SMILES is generated
		 * string smi = sg.Create(mol, order);
		 *
		 * // load the coordinates array such that they are in the order the atoms
		 * // are read when parsing the SMILES
		 * Vector2[] coords = new Vector2[mol.Atoms.Count];
		 * for (int i = 0; i < coords.Length; i++)
		 *     coords[order[i]] = container.Atoms[i].Point2D;
		 *
		 * // SMILES string suffixed by the coordinates
		 * string smi2d = smi + " " + Arrays.ToString(coords);
		 *
		 * </pre></blockquote>
		 *
		 * @param molecule the molecule to write
		 * @param order    array to store the output order of atoms
		 * @return the SMILES string
		 * @ SMILES could not be created
		 */
        public string Create(IAtomContainer molecule, int[] order)
        {

            try
            {
                if (order.Length != molecule.Atoms.Count)
                    throw new ArgumentException("the array for storing output order should be"
                            + "the same length as the number of atoms");

                Graph g = converter.ToBeamGraph(molecule);

                // apply the canonical labelling
                if (canonical)
                {

                    // determine the output order
                    int[] labels = Labels(molecule);

                    g = g.Permute(labels).Resonate();

                    if (isomeric)
                    {

                        // FIXME: required to ensure canonical double bond labelling
                        g.Sort(new Graph.VisitHighOrderFirst());

                        // canonical double-bond stereo, output be C/C=C/C or C\C=C\C
                        // and we need to normalise to one
                        g = Functions.NormaliseDirectionalLabels(g);

                        // visit double bonds first, prefer C1=CC=C1 to C=1C=CC1
                        // visit hydrogens first
                        g.Sort(new Graph.VisitHighOrderFirst()).Sort(new Graph.VisitHydrogenFirst());
                    }

                    string smiles = g.ToSmiles(order);

                    // the SMILES has been generated on a reordered molecule, transform
                    // the ordering
                    int[] canorder = new int[order.Length];
                    for (int i = 0; i < labels.Length; i++)
                        canorder[i] = order[labels[i]];
                    Array.Copy(canorder, 0, order, 0, order.Length);

                    return smiles;
                }
                else
                {
                    return g.ToSmiles(order);
                }
            }
            catch (IOException e)
            {
                throw new CDKException(e.Message);
            }
        }

        /**
		 * Generate a SMILES for the given <code>Reaction</code>.
		 *
		 * @param reaction the reaction in question
		 * @return the SMILES representation of the reaction
		 * @.openscience.cdk.exception.CDKException if there is an error during SMILES generation
		 */
        public string CreateReactionSMILES(IReaction reaction)
        {
            StringBuilder reactionSMILES = new StringBuilder();
            IAtomContainerSet<IAtomContainer> reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                reactionSMILES.Append(Create(reactants[i]));
                if (i + 1 < reactants.Count)
                {
                    reactionSMILES.Append('.');
                }
            }
            reactionSMILES.Append('>');
            IAtomContainerSet<IAtomContainer> agents = reaction.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                reactionSMILES.Append(Create(agents[i]));
                if (i + 1 < agents.Count)
                {
                    reactionSMILES.Append('.');
                }
            }
            reactionSMILES.Append('>');
            IAtomContainerSet<IAtomContainer> products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                reactionSMILES.Append(Create(products[i]));
                if (i + 1 < products.Count)
                {
                    reactionSMILES.Append('.');
                }
            }
            return reactionSMILES.ToString();
        }

        /**
		 * Indicates whether output should be an aromatic SMILES.
		 *
		 * @param useAromaticityFlag if false only SP2-hybridized atoms will be lower case (default),
		 * true=SP2 or aromaticity trigger lower case
		 * @deprecated since 1.5.6, use {@link #aromatic} - invoking this method
		 *             does nothing
		 */
        [Obsolete]
        public void SetUseAromaticityFlag(bool useAromaticityFlag)
        {

        }

        /**
		 * Given a molecule (possibly disconnected) compute the labels which
		 * would order the atoms by increasing canonical labelling. If the SMILES
		 * are isomeric (i.e. stereo and isotope specific) the InChI numbers are
		 * used. These numbers are loaded via reflection and the 'cdk-inchi' module
		 * should be present on the classpath.
		 *
		 * @param molecule the molecule to
		 * @return the permutation
		 * @see Canon
		 */
        private int[] Labels(IAtomContainer molecule)
        {
            long[] labels = isomeric ? InChiNumbers(molecule) : Canon.Label(molecule, GraphUtil.ToAdjList(molecule));
            int[] cpy = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
                cpy[i] = (int)labels[i] - 1;
            return cpy;
        }

        /**
		 * Obtain the InChI numbering for canonising SMILES. The cdk-smiles module
		 * does not and should not depend on cdk-inchi and so the numbers are loaded
		 * via reflection. If the class cannot be found on the classpath an
		 * exception is thrown.
		 *
		 * @param container a structure
		 * @return the inchi numbers
		 * @ the inchi numbers could not be obtained
		 */
        private long[] InChiNumbers(IAtomContainer container)
        {
            // TODO: create an interface so we don't have to dynamically load the
            // class each time

            return InChINumbersTools.GetUSmilesNumbers(container);
        }
    }
}
