/* Copyright (C) 1997-2009  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
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
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace NCDK.Fingerprints
{
    // @cdk.module test
    [TestClass()]
    public abstract class AbstractFingerprinterTest : CDKTestCase
    {
        public virtual IFingerprinter GetBitFingerprinter()
        {
            throw new InvalidOperationException("This method should be overwritten " + "by subclasses unit tests");
        }

        /* override if method is implemented */
        [TestMethod()]
        public virtual void TestGetCountFingerprint()
        {
            try
            {
                GetBitFingerprinter().GetCountFingerprint(new Mock<IAtomContainer>().Object);
                Assert.Fail();
            }
            catch (NotSupportedException)
            { }
        }

        /* override if method is implemented */
        [TestMethod()]
        public virtual void TestGetRawFingerprint()
        {
            try
            {
                GetBitFingerprinter().GetRawFingerprint(new Mock<IAtomContainer>().Object);
                Assert.Fail();
            }
            catch (NotSupportedException)
            { }
        }
    }
}
