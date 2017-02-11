/* Copyright (C) 2002-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Util.Xml;
using NCDK.Default;
using System.Diagnostics;
using System.Xml.Linq;

namespace NCDK.IO.InChI
{
    /**
	 * SAX2 implementation for INChI XML fragment parsing.
	 *
	 * <p>The supported elements are: identifier, formula and
	 * connections. All other elements are not parsed (at this moment).
	 * This parser is written based on the INChI files in data/ichi
	 * for version 1.1Beta.
	 *
	 * <p>The returned ChemFile contains a ChemSequence in
	 * which the ChemModel represents the molecule.
	 *
	 * @cdk.module extra
	 * @cdk.githash
	 *
	 * @see org.openscience.cdk.io.INChIReader
	 *
	 * @cdk.require java1.4+
	 */
    public class INChIHandler : XContentHandler
    {
        private INChIContentProcessorTool inchiTool;

        private ChemFile chemFile;
        private ChemSequence chemSequence;
        private ChemModel chemModel;
        private IAtomContainerSet<IAtomContainer> setOfMolecules;
        private IAtomContainer tautomer;

        /**
		 * Constructor for the IChIHandler.
		 **/
        public INChIHandler()
        {
            inchiTool = new INChIContentProcessorTool();
        }

        public override void DoctypeDecl(XDocumentType docType)
        {
            if (docType == null)
                return;
            Trace.TraceInformation("DocType root element: " + docType.Name);
            Trace.TraceInformation("DocType root PUBLIC: " + docType.PublicId);
            Trace.TraceInformation("DocType root SYSTEM: " + docType.SystemId);
        }

        public override void StartDocument()
        {
            chemFile = new ChemFile();
            chemSequence = new ChemSequence();
            chemModel = new ChemModel();
            setOfMolecules = new AtomContainerSet<IAtomContainer>();
        }

        public override void EndDocument()
        {
            chemFile.Add(chemSequence);
        }

        public override void EndElement(XElement element)
        {
            Debug.WriteLine("end element: ", element.ToString());
            if ("identifier".Equals(element.Name.LocalName))
            {
                if (tautomer != null)
                {
                    // ok, add tautomer
                    setOfMolecules.Add(tautomer);
                    chemModel.MoleculeSet = setOfMolecules;
                    chemSequence.Add(chemModel);
                }
            }
            else if ("formula".Equals(element.Name.LocalName))
            {
                if (tautomer != null)
                {
                    Trace.TraceInformation("Parsing <formula> chars: ", element.Value);
                    tautomer = new AtomContainer(inchiTool.ProcessFormula(
                            setOfMolecules.Builder.CreateAtomContainer(), element.Value));
                }
                else
                {
                    Trace.TraceWarning("Cannot set atom info for empty tautomer");
                }
            }
            else if ("connections".Equals(element.Name.LocalName))
            {
                if (tautomer != null)
                {
                    Trace.TraceInformation("Parsing <connections> chars: ", element.Value);
                    inchiTool.ProcessConnections(element.Value, tautomer, -1);
                }
                else
                {
                    Trace.TraceWarning("Cannot set dbond info for empty tautomer");
                }
            }
            else
            {
                // skip all other elements
            }
        }

        /**
		 * Implementation of the StartElement() procedure overwriting the
		 * DefaultHandler interface.
		 *
		 * @param uri       the Universal Resource Identifier
		 * @param local     the local name (without namespace part)
		 * @param raw       the complete element name (with namespace part)
		 * @param atts      the attributes of this element
		 */
        public override void StartElement(XElement element)
        {
            Debug.WriteLine("startElement: ", element.ToString());
            Debug.WriteLine("uri: ", element.Name.NamespaceName);
            Debug.WriteLine("local: ", element.Name.LocalName);
            Debug.WriteLine("raw: ", element.ToString());
            if ("INChI".Equals(element.Name.LocalName))
            {
                // check version
                foreach (var att in element.Attributes())
                {
                    if (att.Name.LocalName.Equals("version")) Trace.TraceInformation("INChI version: ", att.Value);
                }
            }
            else if ("structure".Equals(element.Name.LocalName))
            {
                tautomer = new AtomContainer();
            }
            else
            {
                // skip all other elements
            }
        }

        public ChemFile ChemFile => chemFile;
    }
}
