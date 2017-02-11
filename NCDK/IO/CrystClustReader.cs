/* Copyright (C) 2004-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.IO.Formats;
using System.IO;
using System;
using NCDK.Numerics;
using System.Diagnostics;
using NCDK.Maths;
using NCDK.Geometries;

namespace NCDK.IO
{
    // @cdk.module extra
    // @cdk.githash
    // @cdk.iooptions
    public class CrystClustReader : DefaultChemObjectReader
    {
        private TextReader input;

        public CrystClustReader()
            : this(new StringReader(""))    // fixed CDK's bug
        { }

        public CrystClustReader(TextReader input)
        {
            this.input = input;
        }

        public CrystClustReader(Stream input)
            : this(new StreamReader(input))
        { }

        public override IResourceFormat Format => CrystClustFormat.Instance;

        public override void SetReader(TextReader reader)
        {
            this.input = reader;
        }

        public override void SetReader(Stream input)
        {
            SetReader(new StreamReader(input));
        }

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                IChemFile cf = ReadChemFile((IChemFile)obj);
                return (T)cf;
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile.");
            }
        }

        private IChemFile ReadChemFile(IChemFile file)
        {
            IChemSequence seq = file.Builder.CreateChemSequence();
            IChemModel model = file.Builder.CreateChemModel();
            ICrystal crystal = null;

            int lineNumber = 0;
            Vector3 a, b, c;

            try
            {
                string line = input.ReadLine();
                while (line != null)
                {
                    Debug.WriteLine($"{lineNumber++}: {line}");
                    if (line.StartsWith("frame:"))
                    {
                        Debug.WriteLine("found new frame");
                        model = file.Builder.CreateChemModel();
                        crystal = file.Builder.CreateCrystal();

                        // assume the file format is correct

                        Debug.WriteLine("reading spacegroup");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        crystal.SpaceGroup = line;

                        Debug.WriteLine("reading unit cell axes");
                        Vector3 axis = new Vector3();
                        Debug.WriteLine("parsing A: ");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.X = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Y = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Z = FortranFormat.Atof(line);
                        crystal.A = axis;
                        axis = new Vector3();
                        Debug.WriteLine("parsing B: ");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.X = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Y = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Z = FortranFormat.Atof(line);
                        crystal.B = axis;
                        axis = new Vector3();
                        Debug.WriteLine("parsing C: ");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.X = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Y = FortranFormat.Atof(line);
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        axis.Z = FortranFormat.Atof(line);
                        crystal.C = axis;
                        Debug.WriteLine("Crystal: ", crystal);
                        a = crystal.A;
                        b = crystal.B;
                        c = crystal.C;

                        Debug.WriteLine("Reading number of atoms");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        int atomsToRead = int.Parse(line);

                        Debug.WriteLine("Reading no molecules in assym unit cell");
                        line = input.ReadLine();
                        Debug.WriteLine($"{lineNumber++}: {line}");
                        int Z = int.Parse(line);
                        crystal.Z = Z;

                        string symbol;
                        double charge;
                        Vector3 cart;
                        for (int i = 1; i <= atomsToRead; i++)
                        {
                            cart = new Vector3();
                            line = input.ReadLine();
                            Debug.WriteLine($"{lineNumber++}: {line}");
                            symbol = line.Substring(0, line.IndexOf(':'));
                            charge = double.Parse(line.Substring(line.IndexOf(':') + 1));
                            line = input.ReadLine();
                            Debug.WriteLine($"{lineNumber++}: {line}");
                            cart.X = double.Parse(line); // x
                            line = input.ReadLine();
                            Debug.WriteLine($"{lineNumber++}: {line}");
                            cart.Y = double.Parse(line); // y
                            line = input.ReadLine();
                            Debug.WriteLine($"{lineNumber++}: {line}");
                            cart.Z = double.Parse(line); // z
                            IAtom atom = file.Builder.CreateAtom(symbol);
                            atom.Charge = charge;
                            // convert cartesian coords to fractional
                            Vector3 frac = CrystalGeometryTools.CartesianToFractional(a, b, c, cart);
                            atom.FractionalPoint3D = frac;
                            crystal.Atoms.Add(atom);
                            Debug.WriteLine("Added atom: ", atom);
                        }

                        model.Crystal = crystal;
                        seq.Add(model);
                    }
                    else
                    {
                        Debug.WriteLine("Format seems broken. Skipping these lines:");
                        while (line != null && !line.StartsWith("frame:"))
                        {
                            line = input.ReadLine();
                            Debug.WriteLine($"{lineNumber++}: {line}");
                        }
                        Debug.WriteLine("Ok, resynched: found new frame");
                    }
                }
                file.Add(seq);
            }
            catch (Exception exception)
            {
                if (!(exception is IOException || exception is ArgumentException))
                    throw;
                string message = "Error while parsing CrystClust file: " + exception.Message;
                Trace.TraceError(message);
                Debug.WriteLine(exception);
                throw new CDKException(message, exception);
            }
            return file;
        }

        public override void Close()
        {
            input.Close();
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
