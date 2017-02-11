/* Copyright (C) 2002-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Common.Mathematics;
using System;
using System.Collections.Generic;
using NCDK.Numerics;

namespace NCDK.Geometries
{
    /**
     * A set of static methods for working with crystal coordinates.
     *
     * @cdk.module standard
     * @cdk.githash
     *
     * @author  Egon Willighagen <egonw@sci.kun.nl>
     *
     * @cdk.keyword fractional coordinates, crystal
     */
    public class CrystalGeometryTools
    {

        /**
         * Inverts three cell axes.
         *
         * @return         a 3x3 matrix with the three Cartesian vectors representing
         *                 the unit cell axes. The a axis is the first row.
         */
        public static Vector3[] CalcInvertedAxes(Vector3 aAxis, Vector3 bAxis, Vector3 cAxis)
        {
            double det = aAxis.X * bAxis.Y * cAxis.Z - aAxis.X * bAxis.Z * cAxis.Y - aAxis.Y * bAxis.X * cAxis.Z + aAxis.Y
                    * bAxis.Z * cAxis.X + aAxis.Z * bAxis.X * cAxis.Y - aAxis.Z * bAxis.Y * cAxis.X;
            Vector3[] invaxes = new Vector3[3];
            invaxes[0] = new Vector3();
            invaxes[0].X = (bAxis.Y * cAxis.Z - bAxis.Z * cAxis.Y) / det;
            invaxes[0].Y = ((bAxis.Z * cAxis.X - bAxis.X * cAxis.Z) / det);
            invaxes[0].Z = ((bAxis.X * cAxis.Y - bAxis.Y * cAxis.X) / det);

            invaxes[1] = new Vector3();
            invaxes[1].X = ((aAxis.Z * cAxis.Y - aAxis.Y * cAxis.Z) / det);
            invaxes[1].Y = ((aAxis.X * cAxis.Z - aAxis.Z * cAxis.X) / det);
            invaxes[1].Z = ((aAxis.Y * cAxis.X - aAxis.X * cAxis.Y) / det);

            invaxes[2] = new Vector3();
            invaxes[2].X = ((aAxis.Y * bAxis.Z - aAxis.Z * bAxis.Y) / det);
            invaxes[2].Y = ((aAxis.Z * bAxis.X - aAxis.X * bAxis.Z) / det);
            invaxes[2].Z = ((aAxis.X * bAxis.Y - aAxis.Y * bAxis.X) / det);
            return invaxes;
        }

        /**
         * @cdk.dictref blue-obelisk:convertCartesianIntoFractionalCoordinates
         */
        public static Vector3 CartesianToFractional(Vector3 aAxis, Vector3 bAxis, Vector3 cAxis, Vector3 cartPoint)
        {
            Vector3[] invaxis = CalcInvertedAxes(aAxis, bAxis, cAxis);
            Vector3 frac = new Vector3();
            frac.X = invaxis[0].X * cartPoint.X + invaxis[0].Y * cartPoint.Y + invaxis[0].Z * cartPoint.Z;
            frac.Y = invaxis[1].X * cartPoint.X + invaxis[1].Y * cartPoint.Y + invaxis[1].Z * cartPoint.Z;
            frac.Z = invaxis[2].X * cartPoint.X + invaxis[2].Y * cartPoint.Y + invaxis[2].Z * cartPoint.Z;
            return frac;
        }

        /**
         * @cdk.dictref blue-obelisk:convertFractionIntoCartesianCoordinates
         */
        public static Vector3 FractionalToCartesian(Vector3 aAxis, Vector3 bAxis, Vector3 cAxis, Vector3 frac)
        {
            Vector3 cart = new Vector3();
            cart.X = frac.X * aAxis.X + frac.Y * bAxis.X + frac.Z * cAxis.X;
            cart.Y = frac.X * aAxis.Y + frac.Y * bAxis.Y + frac.Z * cAxis.Y;
            cart.Z = frac.X * aAxis.Z + frac.Y * bAxis.Z + frac.Z * cAxis.Z;
            return cart;
        }

        /**
         * Calculates Cartesian vectors for unit cell axes from axes lengths and angles
         * between axes.
         *
         * <p>To calculate Cartesian coordinates, it places the a axis on the x axes,
         * the b axis in the xy plane, making an angle gamma with the a axis, and places
         * the c axis to fulfill the remaining constraints. (See also
         * <a href="http://server.ccl.net/cca/documents/molecular-modeling/node4.html">the
         * CCL archive</a>.)
         *
         * @param alength   length of the a axis
         * @param blength   length of the b axis
         * @param clength   length of the c axis
         * @param alpha     angle between b and c axes in degrees
         * @param beta      angle between a and c axes in degrees
         * @param gamma     angle between a and b axes in degrees
         * @return          an array of Vector3 objects with the three Cartesian vectors representing
         *                  the unit cell axes.
         *
         * @cdk.keyword  notional coordinates
         * @cdk.dictref  blue-obelisk:convertNotionalIntoCartesianCoordinates
         */
        public static Vector3[] NotionalToCartesian(double alength, double blength, double clength, double alpha,
                double beta, double gamma)
        {
            Vector3[] axes = new Vector3[3];

            /* 1. align the a axis with x axis */
            axes[0] = new Vector3();
            axes[0].X = alength;
            axes[0].Y = 0;
            axes[0].Z = 0;

            double toRadians = Math.PI / 180.0;

            /* some intermediate variables */
            double cosalpha = Math.Cos(toRadians * alpha);
            double cosbeta = Math.Cos(toRadians * beta);
            double cosgamma = Math.Cos(toRadians * gamma);
            double singamma = Math.Sin(toRadians * gamma);

            /* 2. place the b is in xy plane making a angle gamma with a */
            axes[1] = new Vector3();
            axes[1].X = (blength * cosgamma);
            axes[1].Y = (blength * singamma);
            axes[1].Z = 0;

            /* 3. now the c axis, with more complex maths */
            axes[2] = new Vector3();
            double volume = alength
                    * blength
                    * clength
                    * Math.Sqrt(1.0 - cosalpha * cosalpha - cosbeta * cosbeta - cosgamma * cosgamma + 2.0 * cosalpha
                            * cosbeta * cosgamma);
            axes[2].X = (clength * cosbeta);
            axes[2].Y = (clength * (cosalpha - cosbeta * cosgamma) / singamma);
            axes[2].Z = (volume / (alength * blength * singamma));

            return axes;
        }

        /**
         * @cdk.dictref  blue-obelisk:convertCartesianIntoNotionalCoordinates
         */
        public static double[] CartesianToNotional(Vector3 aAxis, Vector3 bAxis, Vector3 cAxis)
        {
            double[] notionalCoords = new double[6];
            notionalCoords[0] = aAxis.Length();
            notionalCoords[1] = bAxis.Length();
            notionalCoords[2] = cAxis.Length();
            notionalCoords[3] = Vectors.Angle(bAxis, cAxis) * 180.0 / Math.PI;
            notionalCoords[4] = Vectors.Angle(aAxis, cAxis) * 180.0 / Math.PI;
            notionalCoords[5] = Vectors.Angle(aAxis, bAxis) * 180.0 / Math.PI;
            return notionalCoords;
        }

        /**
         * Determines if this model contains fractional (crystal) coordinates.
         *
         * @return  bool indication that 3D coordinates are available
         */
        public static bool HasCrystalCoordinates(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
                if (atom.FractionalPoint3D == null)
                    return false;
            return true;
        }

        /**
         * Creates Cartesian coordinates for all Atoms in the Crystal.
         */
        public static void FractionalToCartesian(ICrystal crystal)
        {
            Vector3 aAxis = crystal.A;
            Vector3 bAxis = crystal.B;
            Vector3 cAxis = crystal.C;
            foreach (var atom in crystal.Atoms)
            {
                Vector3? fracPoint = atom.FractionalPoint3D;
                if (fracPoint != null)
                {
                    atom.Point3D = FractionalToCartesian(aAxis, bAxis, cAxis, fracPoint.Value);
                }
            }
        }
    }
}
