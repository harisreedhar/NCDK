/* Copyright (C) 2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCDK.QSAR.Result
{
    /**
     * @cdk.module test-standard
     */
    [TestClass()]
    public class IntegerResultTest : CDKTestCase
    {

        public IntegerResultTest()
                : base()
        { }

        [TestMethod()]
        public void TestIntegerResult_int()
        {
            IntegerResult result = new IntegerResult(5);
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void TestToString()
        {
            IntegerResult result = new IntegerResult(5);
            Assert.AreEqual("5", result.ToString());
        }

        [TestMethod()]
        public void TestIntValue()
        {
            IntegerResult result = new IntegerResult(5);
            Assert.AreEqual(5, result.Value);
        }

        [TestMethod()]
        public void TestLength()
        {
            IntegerResult result = new IntegerResult(5);
            Assert.AreEqual(1, result.Length);
        }
    }
}
