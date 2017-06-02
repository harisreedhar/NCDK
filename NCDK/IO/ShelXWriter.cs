/* Copyright (C) 2001-2007  Egon Willighagen <egonw@users.sf.net>
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
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.IO.Formats;
using System.IO;
using System;
using NCDK.Common.Mathematics;
using NCDK.Numerics;
using NCDK.Tools.Manipulator;
using NCDK.Geometries;
using System.Linq;

namespace NCDK.IO
{
    /// <summary>
    /// Serializes a MoleculeSet or a Molecule object to ShelX code.
    /// The output can be read with Platon.
    /// </summary>
    // @cdk.module  extra
    // @cdk.githash
    // @cdk.iooptions
    // @author Egon Willighagen
    // @cdk.keyword file format, ShelX
    public class ShelXWriter : DefaultChemObjectWriter
    {
        private TextWriter writer;

        /// <summary>
        /// Constructs a new ShelXWriter class. Output will be stored in the Writer class given as parameter.
        /// </summary>
        /// <param name="output">Writer to redirect the output to.</param>
        public ShelXWriter(TextWriter output)
        {
            writer = output;
        }

        public ShelXWriter(Stream output)
           : this(new StreamWriter(output))
        { }

        public ShelXWriter()
                : this(new StringWriter())
        { }

        public override IResourceFormat Format => ShelXFormat.Instance;

        public override void SetWriter(TextWriter output)
        {
            writer = output;
        }

        public override void SetWriter(Stream output)
        {
            SetWriter(new StreamWriter(output));
        }

        /// <summary>
        /// Flushes the output and closes this object.
        /// </summary>
        public override void Close()
        {
            writer.Close();
        }

        public override void Dispose()
        {
            Close();
        }

        public override bool Accepts(Type type)
        {
            if (typeof(ICrystal).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Serializes the IChemObject to ShelX and redirects it to the output Writer.
        /// </summary>
        /// <param name="obj">A Molecule of MoleculeSet object</param>
        public override void Write(IChemObject obj)
        {
            if (obj is ICrystal)
            {
                WriteCrystal((ICrystal)obj);
            }
            else
            {
                throw new CDKException("Only Crystal objects can be read.");
            }
        }

        // Private procedures

        private void WriteCrystal(ICrystal crystal)
        {
            var title = crystal.GetProperty<string>(CDKPropertyName.Title);
            if (title != null && title.Trim().Length > 0)
            {
                Writeln("TITL " + title.ToString().Trim());
            }
            else
            {
                Writeln("TITL Produced with CDK (http://cdk.sf.net/)");
            }
            Vector3 a = crystal.A;
            Vector3 b = crystal.B;
            Vector3 c = crystal.C;
            double alength = a.Length();
            double blength = b.Length();
            double clength = c.Length();
            double alpha = Vectors.RadianToDegree(Vectors.Angle(b, c));
            double beta = Vectors.RadianToDegree(Vectors.Angle(a, c));
            double gamma = Vectors.RadianToDegree(Vectors.Angle(a, b));
            Write("CELL " + 1.54184.ToString("F5") + "   ");
            Write(alength.ToString("F5") + "  ");
            Write(blength.ToString("F5") + "  ");
            Write(clength.ToString("F5") + "  ");
            Write(alpha.ToString("F4") + "  ");
            Write(beta.ToString("F4") + "  ");
            Write(gamma.ToString("F4") + "  ");
            Writeln("ZERR " + ((double)crystal.Z).ToString("F5")
                    + "    0.01000  0.01000   0.01000   0.0100   0.0100   0.0100");
            string spaceGroup = crystal.SpaceGroup;
            if ("P1".Equals(spaceGroup))
            {
                Writeln("LATT  -1");
            }
            else if ("P 2_1 2_1 2_1".Equals(spaceGroup))
            {
                Writeln("LATT  -1");
                Writeln("SYMM  1/2+X   , 1/2-Y   ,    -Z");
                Writeln("SYMM     -X   , 1/2+Y   , 1/2-Z");
                Writeln("SYMM  1/2-X   ,    -Y   , 1/2+Z");
            }
            //        MFAnalyser mfa = new MFAnalyser(crystal);
            string elemNames = "";
            string elemCounts = "";
            IMolecularFormula formula = MolecularFormulaManipulator.GetMolecularFormula(crystal);
            var asortedElements = MolecularFormulaManipulator.Elements(formula).ToList();
            foreach (var element in asortedElements)
            {
                string symbol = element.Symbol;
                elemNames += symbol + "    ".Substring(symbol.Length);
                string countS = MolecularFormulaManipulator.GetElementCount(formula, element).ToString();
                elemCounts += countS + "    ".Substring(countS.Length);
            }
            Writeln("SFAC  " + elemNames);
            Writeln("UNIT  " + elemCounts);
            /* write atoms */
            for (int i = 0; i < crystal.Atoms.Count; i++)
            {
                IAtom atom = crystal.Atoms[i];
                Vector3 cartCoord = atom.Point3D.Value;
                Vector3 fracCoord = CrystalGeometryTools.CartesianToFractional(a, b, c, cartCoord);
                string symbol = atom.Symbol;
                string output = symbol + (i + 1);
                Write(output);
                for (int j = 1; j < 5 - output.Length; j++)
                {
                    Write(" ");
                }
                Write("     ");
                string elemID = null;
                for (int elemidx = 0; elemidx < asortedElements.Count; elemidx++)
                {
                    IElement elem = asortedElements[elemidx];
                    if (elem.Symbol.Equals(symbol))
                    {
                        elemID = (elemidx + 1).ToString();
                        break;
                    }
                }
                Write(elemID);
                Write("    ".Substring(elemID.Length));
                Write(fracCoord.X.ToString("F5") + "   ");
                Write(fracCoord.Y.ToString("F5") + "   ");
                Writeln(fracCoord.Y.ToString("F5") + "    11.00000    0.05000");
            }
            Writeln("END");
        }

        private void Write(string s)
        {
            try
            {
                writer.Write(s);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("CMLWriter IOException while printing \"" + s + "\":" + e.ToString());
            }
        }

        private void Writeln(string s)
        {
            try
            {
                writer.Write(s);
                writer.WriteLine();
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("CMLWriter IOException while printing \"" + s + "\":" + e.ToString());
            }
        }
    }
}
