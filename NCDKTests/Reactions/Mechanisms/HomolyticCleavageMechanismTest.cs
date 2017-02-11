/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@yahoo.es>
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCDK.Reactions.Mechanisms
{
    /**
     * Tests for HomolyticCleavageMechanism implementations.
     *
     * @cdk.module test-reaction
     */
    [TestClass()]
    public class HomolyticCleavageMechanismTest : ReactionMechanismTest
    {
        public HomolyticCleavageMechanismTest()
            : base()
        {
            SetMechanism(typeof(HomolyticCleavageMechanism));
        }

        [TestMethod()]
        public void TestHomolyticCleavageMechanism()
        {
            IReactionMechanism mechanism = new HomolyticCleavageMechanism();
            Assert.IsNotNull(mechanism);
        }

        /**
         * Junit test.
         * TODDO: REACT: add an example
         *
         * @throws Exception
         */
        [TestMethod()]
        public void TestInitiate_IAtomContainerSet_ArrayList_ArrayList()
        {
            IReactionMechanism mechanism = new HomolyticCleavageMechanism();

            Assert.IsNotNull(mechanism);
        }
    }
}
