/* Copyright (C) 2004-2008  Rajarshi Guha <rajarshi.guha@gmail.com>
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
using NCDK.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCDK.Pharmacophore
{
    // @cdk.module test-pcore
    [TestClass()]
    public class PharmacophoreBondTest
    {
        [TestMethod()]
        public void TestGetBondLength()
        {
            PharmacophoreAtom patom1 = new PharmacophoreAtom("[CX2]N", "Amine", Vector3.Zero);
            PharmacophoreAtom patom2 = new PharmacophoreAtom("c1ccccc1", "Aromatic", new Vector3(1, 1, 1));
            PharmacophoreBond pbond = new PharmacophoreBond(patom1, patom2);
            Assert.AreEqual(1.732051, pbond.BondLength, 0.00001);
        }
    }
}
