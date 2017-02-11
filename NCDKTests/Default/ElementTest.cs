﻿/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDK.Default
{
    /**
     * Checks the functionality of the Element class.
     *
     * @cdk.module test-data
     *
     * @see org.openscience.cdk.Element
     */
    [TestClass()]
    public class ElementTest : AbstractElementTest
    {
        public override IChemObject NewChemObject()
        {
            return new Element();
        }

        // test constructors

        [TestMethod()]
        public void TestElement()
        {
            IElement e = new Element();
            Assert.IsTrue(e is IChemObject);
        }

        [TestMethod()]
        public void TestElement_IElement()
        {
            IElement element = new Element();
            IElement e = new Element(element);
            Assert.IsTrue(e is IChemObject);
        }

        [TestMethod()]
        public void TestElement_String()
        {
            IElement e = new Element("C");
            Assert.AreEqual("C", e.Symbol);
        }

        [TestMethod()]
        public void TestElement_X()
        {
            IElement e = new Element("X");
            Assert.AreEqual("X", e.Symbol);
            // and it should not throw exceptions
            Assert.IsNotNull(e.AtomicNumber);
            Assert.AreEqual(0, e.AtomicNumber);
        }

        [TestMethod()]
        public void TestElement_String_Integer()
        {
            IElement e = new Element("H", 1);
            Assert.AreEqual("H", e.Symbol);
            Assert.AreEqual(1, e.AtomicNumber.Value);
        }

        [TestMethod()]
        public void CompareSymbol()
        {
            Element e1 = new Element("H", 1);
            Element e2 = new Element("H", 1);
            Assert.IsTrue(e1.Compare(e2));
        }

        [TestMethod()]
        public void CompareAtomicNumber()
        {
            Element e1 = new Element("H", 1);
            Element e2 = new Element("H", 1);
            Assert.IsTrue(e1.Compare(e2));
        }

        [TestMethod()]
        public void CompareDiffSymbol()
        {
            Element e1 = new Element("H", 1);
            Element e2 = new Element("C", 12);
            Assert.IsFalse(e1.Compare(e2));
        }

        [TestMethod()]
        public void CompareDiffAtomicNumber()
        {
            Element e1 = new Element("H", 1);
            Element e2 = new Element("H", null);
            Assert.IsFalse(e1.Compare(e2));
        }
    }
}
