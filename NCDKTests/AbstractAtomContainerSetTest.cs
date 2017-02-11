/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Common.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK
{
    /**
     * Checks the functionality of {@link IAtomContainerSet} implementations.
     *
     * @cdk.module test-interfaces
     */
    [TestClass()]
    public abstract class AbstractAtomContainerSetTest<T>
        : AbstractChemObjectTest
        where T : IAtomContainer
    {
        public abstract T NewContainerObject();

        /**
		 * @cdk.bug 3093241
		 */
        [TestMethod()]
        public virtual void TestSortAtomContainers_Comparator_Null()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            IChemObjectBuilder builder = som.Builder;
            T con1 = NewContainerObject();
            con1.Add(builder.CreateAtom("C"));
            con1.Add(builder.CreateAtom("C"));
            T con2 = NewContainerObject();
            con2.Add(builder.CreateAtom("C"));
            som.Add(con1);
            som.Add(con2);
            Assert.IsNotNull(som[0]);
            Assert.IsNotNull(som[1]);

            var comparator = new AtomContainerComparator<T>();
            som.Sort(comparator);
            Assert.IsNotNull(som[0]);
            Assert.AreEqual(1, som[0].Atoms.Count);
            Assert.IsNotNull(som[1]);
            Assert.AreEqual(2, som[1].Atoms.Count);
        }

        /**
		 * ensure coefficients are sorted also
		 */
        [TestMethod()]
        public virtual void TestSort_Coefficients()
        {
            IAtomContainerSet<T> set = (IAtomContainerSet<T>)NewChemObject();
            
            IChemObjectBuilder builder = set.Builder;

            T a = NewContainerObject();
            T b = NewContainerObject();

            a.Add(builder.CreateAtom("C"));
            a.Add(builder.CreateAtom("C"));

            b.Add(builder.CreateAtom("C"));

            set.Add(a, 1);
            set.Add(b, 2);

            Assert.AreEqual(a, set[0]);
            Assert.AreEqual(1D, set.GetMultiplier(0));
            Assert.AreEqual(b, set[1]);
            Assert.AreEqual(2D, set.GetMultiplier(1));

            // sort by atom container count
            var orderedSet = set.OrderBy(n => n, new ComparerByCoefficients());

            AssertAreOrderLessEqual(
                new[] { a, b }.Cast<IChemObject>(), 
                set.Cast<IChemObject>());
            Assert.IsTrue(Compares.AreDeepEqual(
                new[] { 1D, 2D },
                new[] { set.GetMultiplier(0), set.GetMultiplier(1) }));
        }

        class ComparerByCoefficients : IComparer<T>
        {
            public int Compare(T o1, T o2)
            {
                int n = o1.Atoms.Count;
                int m = o2.Atoms.Count;
                if (n > m) return +1;
                if (n < m) return -1;
                return 0;
            }
        }

        /**
	 	 * Ensures that sort method of the AtomContainerSet does not include nulls
		 * in the comparator. This is tested using a comparator which sorts null
		 * values as low and thus to the start of an array. By adding two (non-null)
		 * values and sorting we should see that the first two values are not null
		 * despite giving a comparator which sorts null as low.
		 *
		 * @cdk.bug 1291
		 */
        [TestMethod()]
        public virtual void TestSort_BrokenComparator()
        {

            IAtomContainerSet<T> set = (IAtomContainerSet<T>)NewChemObject();

            IChemObjectBuilder builder = set.Builder;

            T a = NewContainerObject();
            T b = NewContainerObject();

            a.Add(builder.CreateAtom("C"));
            a.Add(builder.CreateAtom("C"));
            b.Add(builder.CreateAtom("C"));

            set.Add(a);
            set.Add(b);

            // this comparator is deliberately broken but serves for the test
            //  - null should be a high value (Interger.MAX)
            //  - also, avoid boxed primitives in comparators
            set.Sort(new ComparerByBroken());

            // despite null being low, the two atom containers should
            // still be in the first slot
            Assert.IsNotNull(set[0]);
            Assert.IsNotNull(set[1]);
            Assert.IsTrue(set.Count == 2 || set[2] == null);
        }

        class ComparerByBroken : IComparer<T>
        {
            public int Compare(T o1, T o2)
            {
                return CountAtoms(o1).CompareTo(CountAtoms(o2));
            }

            public int CountAtoms(T container)
            {
                return container == null ? int.MinValue : container.Atoms.Count;
            }
        }

        /**
		 * Ensure that sort is not called on an empty set. We mock the comparator
		 * and verify the compare method is never called
		 */
        [TestMethod()]
        public virtual void TestSort_empty()
        {
            IAtomContainerSet<T> set = (IAtomContainerSet<T>)NewChemObject();
            var comparator = new Mock<IComparer<T>>();
            set.Sort(comparator.Object);
            // verify the comparator was called 0 times
            comparator.Verify(n => n.Compare(It.IsAny<T>(), It.IsAny<T>()), Times.Never);
        }

        [TestMethod()]
        public virtual void TestGetAtomContainerCount()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.AreEqual(3, som.Count);
        }

        // iter.remove is not supported in .NET
        //[TestMethod()]
        //public virtual void TestAtomContainers()
        //{
        //    IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
        //    som.Add(NewContainerObject());
        //    som.Add(NewContainerObject());
        //    som.Add(NewContainerObject());

        //    Assert.AreEqual(3, som.Count);
        //    IEnumerator<IAtomContainer> iter = som.AtomContainers().GetEnumerator();
        //    int count = 0;
        //    while (iter.MoveNext())
        //    {
        //        iter.Next();
        //        ++count;
        //        iter.Remove();
        //    }
        //    Assert.AreEqual(0, som.Count);
        //    Assert.AreEqual(3, count);
        //    Assert.IsFalse(iter.MoveNext());
        //}

        [TestMethod()]
        public virtual void TestAdd_IAtomContainerSet()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            IAtomContainerSet<IAtomContainer> tested = som.Builder.CreateAtomContainerSet();
            Assert.AreEqual(0, tested.Count);
			foreach (var m in som)
                tested.Add(m);
            Assert.AreEqual(3, tested.Count);
        }

        [TestMethod()]
        public virtual void TestGetAtomContainer_int()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.IsNotNull(som[2]); // third molecule should exist
            Assert.IsTrue(som.Count == 3 || som[3] == null); // fourth molecule must not exist
        }

        [TestMethod()]
        public virtual void TestGetMultiplier_int()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());

            Assert.AreEqual(1.0, som.GetMultiplier(0).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestSetMultiplier_int_Double()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());

            Assert.AreEqual(1.0, som.GetMultiplier(0).Value, 0.00001);
            som.SetMultiplier(0, 2.0);
            Assert.AreEqual(2.0, som.GetMultiplier(0).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestSetMultipliers_arrayDouble()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T container = NewContainerObject();
            som.Add(container);
            T container2 = NewContainerObject();
            som.Add(container2);

            Assert.AreEqual(1.0, som.GetMultiplier(0).Value, 0.00001);
            Assert.AreEqual(1.0, som.GetMultiplier(1).Value, 0.00001);
            double?[] multipliers = new double?[2];
            multipliers[0] = 2.0;
            multipliers[1] = 3.0;
            som.SetMultipliers(multipliers);
            Assert.AreEqual(2.0, som.GetMultiplier(0).Value, 0.00001);
            Assert.AreEqual(3.0, som.GetMultiplier(1).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestSetMultiplier_IAtomContainer_Double()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T container = NewContainerObject();
            som.Add(container);

            Assert.AreEqual(1.0, som.GetMultiplier(container).Value, 0.00001);
            som.SetMultiplier(container, 2.0);
            Assert.AreEqual(2.0, som.GetMultiplier(container).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestGetMultipliers()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject(), 1.0);

            var multipliers = som.GetMultipliers();
            Assert.IsNotNull(multipliers);
            Assert.AreEqual(1, multipliers.Count);
        }

        [TestMethod()]
        public virtual void TestGetMultiplier_IAtomContainer()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());

            Assert.AreEqual(-1.0, som.GetMultiplier(NewContainerObject()).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestAddAtomContainer_IAtomContainer()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.AreEqual(5, som.Count);

            // now test it to make sure it properly grows the array
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.AreEqual(7, som.Count);
        }

        [TestMethod()]
        public virtual void TestAddAtomContainer_IAtomContainer_Double()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            som.Add(NewContainerObject(), 2.0);
            Assert.AreEqual(1, som.Count);
            Assert.AreEqual(2.0, som.GetMultiplier(0).Value, 0.00001);
        }

        [TestMethod()]
        public virtual void TestGrowAtomContainerArray()
        {
            // this test assumes that the growSize = 5 !
            // if not, there is need for the array to grow
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();

            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.AreEqual(7, som.Count);
        }

        [TestMethod()]
        public virtual void TestGetAtomContainers()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();

            Assert.AreEqual(0, som.Count);

            som.Add(NewContainerObject());
            som.Add(NewContainerObject());
            som.Add(NewContainerObject());

            Assert.AreEqual(3, som.Count);
            Assert.IsNotNull(som[0]);
            Assert.IsNotNull(som[1]);
            Assert.IsNotNull(som[2]);
        }

        [TestMethod()]
        public virtual void TestToString()
        {
            IAtomContainerSet<IAtomContainer> containerSet = (IAtomContainerSet<IAtomContainer>)NewChemObject();
            string description = containerSet.ToString();
            for (int i = 0; i < description.Length; i++)
            {
                Assert.IsTrue(description[i] != '\n');
                Assert.IsTrue(description[i] != '\r');
            }
        }

        [TestMethod()]
        public override void TestClone()
        {
            IAtomContainerSet<IAtomContainer> containerSet = (IAtomContainerSet<IAtomContainer>)NewChemObject();
            object clone = containerSet.Clone();
            Assert.IsTrue(clone is IAtomContainerSet<IAtomContainer>);
            Assert.AreNotSame(containerSet, clone);
        }

        [TestMethod()]
        public virtual void TestCloneDuplication()
        {
            IAtomContainerSet<T> containerSet = (IAtomContainerSet<T>)NewChemObject();
            containerSet.Add(NewContainerObject());
            object clone = containerSet.Clone();
            Assert.IsTrue(clone is IAtomContainerSet<T>);
            var clonedSet = (IAtomContainerSet<T>)clone;
            Assert.AreNotSame(containerSet, clonedSet);
            Assert.AreEqual(containerSet.Count, clonedSet.Count);
        }

        [TestMethod()]
        public virtual void TestCloneMultiplier()
        {
            IAtomContainerSet<T> containerSet = (IAtomContainerSet<T>)NewChemObject();
            containerSet.Add(NewContainerObject(), 2);
            object clone = containerSet.Clone();
            Assert.IsTrue(clone is IAtomContainerSet<T>);
            IAtomContainerSet<T> clonedSet = (IAtomContainerSet<T>)clone;
            Assert.AreNotSame(containerSet, clonedSet);
            Assert.AreEqual(2, containerSet.GetMultiplier(0).Value);
            Assert.AreEqual(2, clonedSet.GetMultiplier(0).Value);
        }

        [TestMethod()]
        public override void TestStateChanged_IChemObjectChangeEvent()
        {
            ChemObjectListenerImpl listener = new ChemObjectListenerImpl();
            IAtomContainerSet<T> chemObject = (IAtomContainerSet<T>)NewChemObject();
            chemObject.Listeners.Add(listener);

            chemObject.Add(NewContainerObject());
            Assert.IsTrue(listener.Changed);
        }

        [TestMethod()]
        public virtual void TestRemoveAtomContainer_IAtomContainer()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);
            som.Remove(ac1);
            Assert.AreEqual(1, som.Count);
            Assert.AreEqual(ac2, som[0]);
        }

        [TestMethod()]
        public virtual void TestRemoveAll()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);

            Assert.AreEqual(2, som.Count);
            som.Clear();
            Assert.AreEqual(0, som.Count);
        }

        [TestMethod()]
        public virtual void TestRemoveAtomContainer_int()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);
            som.RemoveAt(0);
            Assert.AreEqual(1, som.Count);
            Assert.AreEqual(ac2, som[0]);
        }

        /*
         * @cdk.bug 2679343
         */
        [TestMethod()]
        public virtual void TestBug2679343()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);
            som.Add(ac2);
            Assert.AreEqual(3, som.Count);
            som.Remove(ac2);
            Assert.AreEqual(1, som.Count);
        }

        [TestMethod()]
        public virtual void TestRePlaceAtomContainer_int_IAtomContainer()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            T ac3 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);
            Assert.AreEqual(ac2, som[1]);
            som[1] = ac3;
            Assert.AreEqual(ac3, som[1]);
        }

        [TestMethod()]
        public virtual void TestSortAtomContainers_Comparator()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            T ac2 = NewContainerObject();
            som.Add(ac1);
            som.Add(ac2);
            som.Sort(new EqualComparer());
            Assert.AreEqual(2, som.Count);
        }

        class EqualComparer : IComparer<T>
        {
            public int Compare(T o1, T o2) => 0;
        }

        [TestMethod()]
        public virtual void TestSortAtomContainers_WithMuliplier()
        {
            IAtomContainerSet<T> som = (IAtomContainerSet<T>)NewChemObject();
            T ac1 = NewContainerObject();
            som.Add(ac1, 2.0);
            ac1.SetProperty("multiplierSortCode", "2");
            T ac2 = NewContainerObject();
            som.Add(ac2, 1.0);
            ac2.SetProperty("multiplierSortCode", "1");
            som.Sort(new CodeComparer());
        	Assert.AreEqual(2, som.Count);
        	T newFirstAC = som[0];
            Assert.AreEqual(newFirstAC.GetProperty<object>("multiplierSortCode"), "1");
        	// OK, sorting worked as intended
        	// The multiplier should have been resorted too:
        	Assert.AreEqual(1.0, som.GetMultiplier(newFirstAC).Value, 0.00001);
        }

        class CodeComparer : IComparer<T>
        {
            public int Compare(T o1, T o2)
                => string.Compare(o1.GetProperty<string>("multiplierSortCode"), o2.GetProperty<string>("multiplierSortCode"), StringComparison.Ordinal);
        }

        protected class ChemObjectListenerImpl
            : IChemObjectListener
        {
            public bool Changed { get; set; }

            public ChemObjectListenerImpl()
            {
                Changed = false;
            }

            [TestMethod()]

            public void OnStateChanged(ChemObjectChangeEventArgs e)
            {
                Changed = true;
            }

            [TestMethod()]
            public void Reset()
            {
                Changed = false;
            }
        }


    //        	[TestMethod()]
    //public virtual void TestIsEmpty()
    //{

    //    IAtomContainerSet<IAtomContainer> set = (IAtomContainerSet<IAtomContainer>)NewChemObject();

    //    Assert.IsTrue(set.IsEmpty, "new container set should be empty");

    //    set.Add(set.Builder.CreateAtomContainer());

    //    Assert.IsFalse(set.IsEmpty, "container set with a single container should not be empty");

    //    set.RemoveAll();

    //    Assert.IsTrue(set.IsEmpty, "container set with all containers removed should be empty");

    //}
}
}