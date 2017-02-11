















// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2015-2016  Kazuya Ujihara

using System;
using System.Text;
using System.Linq;

namespace NCDK.Default
{
    public class FragmentAtom 
        : PseudoAtom, IFragmentAtom
    {
		public FragmentAtom()
        {
            Fragment = Builder.CreateAtomContainer();
		}

		public virtual bool IsExpanded { get; set; }
		public virtual IAtomContainer Fragment { get; set; }

        public override double? ExactMass
        {
            get { return Fragment.Atoms.Select(atom => atom.ExactMass.Value).Sum(); }
            set { throw new InvalidOperationException($"Cannot set the mass of a {nameof(IFragmentAtom)}."); }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("FragmentAtom{").Append(GetHashCode());
            sb.Append(", A=").Append(base.ToString());
            if (Fragment != null) {
                sb.Append(", F=").Append(Fragment.ToString());
            }
            sb.Append('}');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            FragmentAtom clone = (FragmentAtom)base.Clone(map);
            clone.Fragment = (IAtomContainer)Fragment.Clone(map);
            clone.IsExpanded = IsExpanded;
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    public class FragmentAtom 
        : PseudoAtom, IFragmentAtom
    {
		public FragmentAtom()
        {
            Fragment = Builder.CreateAtomContainer();
		}

		public virtual bool IsExpanded { get; set; }
		public virtual IAtomContainer Fragment { get; set; }

        public override double? ExactMass
        {
            get { return Fragment.Atoms.Select(atom => atom.ExactMass.Value).Sum(); }
            set { throw new InvalidOperationException($"Cannot set the mass of a {nameof(IFragmentAtom)}."); }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("FragmentAtom{").Append(GetHashCode());
            sb.Append(", A=").Append(base.ToString());
            if (Fragment != null) {
                sb.Append(", F=").Append(Fragment.ToString());
            }
            sb.Append('}');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            FragmentAtom clone = (FragmentAtom)base.Clone(map);
            clone.Fragment = (IAtomContainer)Fragment.Clone(map);
            clone.IsExpanded = IsExpanded;
            return clone;
        }
    }
}
