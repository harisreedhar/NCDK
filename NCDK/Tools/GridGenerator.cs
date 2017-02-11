/* Copyright (C) 2005-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Common.Collections;
using NCDK.Common.Mathematics;
using System;
using System.IO;
using NCDK.Numerics;

namespace NCDK.Tools
{
    /**
     * Generates a Grid of points in 3D space within given boundaries.
     *
     * @author cho
     * @cdk.githash
     * @cdk.created 2005-09-30
     */
    public class GridGenerator
    {
        public double LatticeConstant { get; set; } = 0.5;
        public double ExtendGrid { get; set; } = 2;
        public double[][][] Grid { get; set; } = null;
        public double[] GridArray { get; private set; } = null;
        public double MaxX { get; private set; } = 0;
        public double MaxY { get; private set; } = 0;
        public double MaxZ { get; private set; } = 0;
        public double MinX { get; private set; } = 0;
        public double MinY { get; private set; } = 0;
        public double MinZ { get; private set; } = 0;
        public int[] Dim { get; set; } = { 0, 0, 0 };

        public GridGenerator() { }

        public GridGenerator(double min, double max)
        {
            SetDimension(min, max);
            GenerateGrid();
        }

        /**
         * @param initialValue used as initial value for the Grid points
         */
        public GridGenerator(double min, double max, double initialValue)
        {
            SetDimension(min, max);
            GenerateGrid();
            InitializeGrid(initialValue);
        }

        public GridGenerator(double[] minMax, double initialValue, bool cubicGridFlag)
        {
            SetDimension(minMax, cubicGridFlag);
            GenerateGrid();
            InitializeGrid(initialValue);
        }

        /**
         * Method sets the maximal 3d dimensions to given min and max values.
         */
        public void SetDimension(double min, double max)
        {
            this.MinX = min;
            this.MaxX = max;
            this.MinY = min;
            this.MaxY = max;
            this.MinZ = min;
            this.MaxZ = max;
        }

        /**
         * Method sets the maximal 3d dimensions to given min and max values.
         */
        public void SetDimension(double[] minMax, bool cubicGridFlag)
        {
            if (cubicGridFlag)
            {
                double min = minMax[0];
                double max = minMax[0];
                for (int i = 0; i < minMax.Length; i++)
                {
                    if (minMax[i] < min)
                    {
                        min = minMax[i];
                    }
                    else if (minMax[i] > max)
                    {
                        max = minMax[i];
                    }
                }
                SetDimension(min, max);
            }
            else
            {
                this.MinX = minMax[0];
                this.MaxX = minMax[1];
                this.MinY = minMax[2];
                this.MaxY = minMax[3];
                this.MinZ = minMax[4];
                this.MaxZ = minMax[5];
            }
        }

        /**
         * Method sets the maximal 3d dimensions to given min and max values.
         */
        public void SetDimension(double minx, double maxx, double miny, double maxy, double minz, double maxz)
        {
            this.MinX = minx;
            this.MaxX = maxx;
            this.MinY = miny;
            this.MaxY = maxy;
            this.MinZ = minz;
            this.MaxZ = maxz;
        }

        /**
         * Main method creates a Grid between given boundaries (dimensions).
         * The Grid my be extended over the given boundaries with the
         * variable ExtendGrid.
         */
        public void GenerateGrid()
        {
            MinX = MinX - ExtendGrid;
            MaxX = MaxX + ExtendGrid;
            MinY = MinY - ExtendGrid;
            MaxY = MaxY + ExtendGrid;
            MinZ = MinZ - ExtendGrid;
            MaxZ = MaxZ + ExtendGrid;

            Dim[0] = (int)Math.Round(Math.Abs(MaxX - MinX) / LatticeConstant);
            Dim[1] = (int)Math.Round(Math.Abs(MaxY - MinY) / LatticeConstant);
            Dim[2] = (int)Math.Round(Math.Abs(MaxZ - MinZ) / LatticeConstant);

            Grid = Arrays.CreateJagged<double>(Dim[0] + 1, Dim[1] + 1, Dim[2] + 1);
        }

        /**
         * Method initialise the given Grid points with a value.
         */
        public void InitializeGrid(double value)
        {
            for (int i = 0; i < Grid.Length; i++)
            {
                for (int j = 0; j < Grid[0].Length; j++)
                {
                    for (int k = 0; k < Grid[0][0].Length; k++)
                    {
                        Grid[k][j][i] = value;
                    }
                }
            }
        }

        /**
         * Method initialise the given Grid points with a value.
         */
        public double[][][] InitializeGrid(double[][][] Grid, double value)
        {
            for (int i = 0; i < Grid.Length; i++)
            {
                for (int j = 0; j < Grid[0].Length; j++)
                {
                    for (int k = 0; k < Grid[0][0].Length; k++)
                    {
                        Grid[k][j][i] = value;
                    }
                }
            }
            return Grid;
        }

        /**
         * Method transforms the Grid to an array.
         */
        public double[] GridToGridArray(double[][][] Grid)
        {
            if (Grid == null)
            {
                Grid = this.Grid;
            }
            GridArray = new double[Dim[0] * Dim[1] * Dim[2] + 3];
            int dimCounter = 0;
            for (int z = 0; z < Grid[0][0].Length; z++)
            {
                for (int y = 0; y < Grid[0].Length; y++)
                {
                    for (int x = 0; x < Grid.Length; x++)
                    {
                        GridArray[dimCounter] = Grid[x][y][z];
                        dimCounter++;
                    }
                }
            }
            return GridArray;
        }

        /**
         * Method calculates coordinates from a given Grid point.
         */
        public Vector3 GetCoordinatesFromGridPoint(Vector3 gridPoint)
        {
            double dx = MinX + LatticeConstant * gridPoint.X;
            double dy = MinY + LatticeConstant * gridPoint.Y;
            double dz = MinZ + LatticeConstant * gridPoint.Z;
            return new Vector3(dx, dy, dz);
        }

        /**
         * Method calculates coordinates from a given Grid array position.
         */
        public Vector3 GetCoordinatesFromGridPoint(int gridPoint)
        {
            int dimCounter = 0;
            Vector3 point = Vector3.Zero;
            for (int z = 0; z < Grid[0][0].Length; z++)
            {
                for (int y = 0; y < Grid[0].Length; y++)
                {
                    for (int x = 0; x < Grid.Length; x++)
                    {
                        if (dimCounter == gridPoint)
                        {
                            point.X = (float)(MinX + LatticeConstant * x);
                            point.Y = (float)(MinY + LatticeConstant * y);
                            point.Z = (float)(MinZ + LatticeConstant * z);
                            return point;
                        }
                        dimCounter++;
                    }
                }
            }
            return point;
        }

        /**
         * Method calculates the nearest Grid point from given coordinates.
         */
        public Vector3 GetGridPointFrom3dCoordinates(Vector3 coord)
        {
            Vector3 gridPoint = new Vector3();

            if (coord.X >= MinX & coord.X <= MaxX)
            {
                gridPoint.X = (int)Math.Round(Math.Abs(MinX - coord.X) / LatticeConstant);
            }
            else
            {
                throw new Exception("CDKGridError: Given coordinates are not in Grid");
            }
            if (coord.Y >= MinY & coord.Y <= MaxY)
            {
                gridPoint.Y = (int)Math.Round(Math.Abs(MinY - coord.Y) / LatticeConstant);
            }
            else
            {
                throw new Exception("CDKGridError: Given coordinates are not in Grid");
            }
            if (coord.Z >= MinZ & coord.Z <= MaxZ)
            {
                gridPoint.Z = (int)Math.Round(Math.Abs(MinZ - coord.Z) / LatticeConstant);
            }
            else
            {
                throw new Exception("CDKGridError: Given coordinates are not in Grid");
            }

            return gridPoint;
        }

        /**
         * Method transforms the Grid into pmesh format.
         */
        public void WriteGridInPmeshFormat(string outPutFileName)
        {
            using (var srm = new FileStream(outPutFileName + ".pmesh", FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(srm))
            {
                int numberOfGridPoints = Grid.Length * Grid[0].Length * Grid[0][0].Length;
                writer.Write(numberOfGridPoints + "\n");
                for (int z = 0; z < Grid[0][0].Length; z++)
                {
                    for (int y = 0; y < Grid[0].Length; y++)
                    {
                        for (int x = 0; x < Grid.Length; x++)
                        {
                            Vector3 coords = GetCoordinatesFromGridPoint(new Vector3(x, y, z));
                            writer.Write(coords.X + "\t" + coords.Y + "\t" + coords.Z + "\n");
                        }
                    }
                }
            }
        }

        /**
         * Method transforms the Grid into pmesh format. Only Grid points
         * with specific value defined with cutoff are considered.
         * <pre>
         * cutoff <0, the values considered must be <=cutoff
         * cutoff >0, the values considered must be >=cutoff
         * </pre>
         */
        public void WriteGridInPmeshFormat(string outPutFileName, double cutOff)
        {
            using (var srm = new FileStream(outPutFileName + ".pmesh", FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(srm))
            {
                bool negative = false;
                if (cutOff < 0)
                {
                    negative = true;
                }
                else
                {
                    negative = false;
                }
                int numberOfGridPoints = 0;
                for (int z = 0; z < Grid[0][0].Length; z++)
                {
                    for (int y = 0; y < Grid[0].Length; y++)
                    {
                        for (int x = 0; x < Grid.Length; x++)
                        {
                            if (negative)
                            {
                                if (Grid[x][y][z] <= cutOff)
                                {
                                    numberOfGridPoints++;
                                }
                            }
                            else
                            {
                                if (Grid[x][y][z] >= cutOff)
                                {
                                    numberOfGridPoints++;
                                }
                            }
                        }
                    }
                }
                writer.Write(numberOfGridPoints + "\n");
                for (int z = 0; z < Grid[0][0].Length; z++)
                {
                    for (int y = 0; y < Grid[0].Length; y++)
                    {
                        for (int x = 0; x < Grid.Length; x++)
                        {
                            Vector3 coords = GetCoordinatesFromGridPoint(new Vector3(x, y, z));
                            if (negative)
                            {
                                if (Grid[x][y][z] <= cutOff)
                                {
                                    writer.Write(coords.X + "\t" + coords.Y + "\t" + coords.Z + "\n");
                                }
                            }
                            else
                            {
                                if (Grid[x][y][z] >= cutOff)
                                {
                                    writer.Write(coords.X + "\t" + coords.Y + "\t" + coords.Z + "\n");
                                }
                            }
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Dim:" + Dim + " SizeX:" + Grid.Length + " SizeY:" + Grid[0].Length + " SizeZ:" + Grid[0][0].Length
                    + "\nminx:" + MinX + " maxx:" + MaxX + "\nminy:" + MinY + " maxy:" + MaxY + "\nminz:" + MinZ + " maxz:"
                    + MaxZ;
        }
    }
}
