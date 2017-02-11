/* Copyright (C) 2011  Jonathan Alvarsson <jonalv@users.sf.net>
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
 */
using System;
using System.Linq;
using System.Collections.Generic;
using NCDK.Common.Collections;
using System.Runtime.Serialization;

namespace NCDK.Fingerprint
{
    /**
    // @author jonalv
    // @cdk.module     standard
    // @cdk.githash
     */
    [Serializable]
    public class IntArrayCountFingerprint : ICountFingerprint {
#if TEST
        public
#endif
        int[] hitHashes;
#if TEST
        public
#endif
        int[] numOfHits;
        private bool behaveAsBitFingerprint;

        public IntArrayCountFingerprint() {
            hitHashes = new int[0];
            numOfHits = new int[0];
            behaveAsBitFingerprint = false;
        }

        public IntArrayCountFingerprint(IDictionary<string, int> rawFingerprint) {
            IDictionary<int, int> hashedFP = new Dictionary<int, int>();
            foreach (var key in rawFingerprint.Keys) {
                int hashedKey = key.GetHashCode();
                int count;
                if (!hashedFP.TryGetValue(hashedKey, out count))
                    count = 0;
                hashedFP.Add(hashedKey, count + rawFingerprint[key]);
            }
            List<int> keys = new List<int>(hashedFP.Keys);
            keys.Sort();
            hitHashes = new int[keys.Count];
            numOfHits = new int[keys.Count];
            int i = 0;
            foreach (var key in keys) {
                hitHashes[i] = key;
                numOfHits[i] = hashedFP[key];
                i++;
            }
        }

        /**
        // Create an <code>IntArrayCountFingerprint</code> from a rawFingerprint
        // and if <code>behaveAsBitFingerprint</code> make it only return 0 or 1
        // as count thus behaving like a bit finger print.
         *
        // @param rawFingerprint
        // @param behaveAsBitFingerprint
         */
        public IntArrayCountFingerprint(IDictionary<string, int> rawFingerprint, bool behaveAsBitFingerprint)
            : this(rawFingerprint)
        {
            this.behaveAsBitFingerprint = behaveAsBitFingerprint;
        }

        public long Count => 4294967296L;

        public int GetCount(int index) {
            if (behaveAsBitFingerprint) {
                return numOfHits[index] == 0 ? 0 : 1;
            }
            return numOfHits[index];
        }

        public int GetHash(int index) {
            return hitHashes[index];
        }

        public int GetNumOfPopulatedbins() {
            return hitHashes.Length;
        }

        public void Merge(ICountFingerprint fp) {
            IDictionary<int, int> newFp = new Dictionary<int, int>();
            {
                for (int i = 0; i < hitHashes.Length; i++)
                {
                    newFp.Add(hitHashes[i], numOfHits[i]);
                }
            }
            {
                for (int i = 0; i < fp.GetNumOfPopulatedbins(); i++)
                {
                    int count;
                    if (!newFp.TryGetValue(fp.GetHash(i), out count))
                        count = 0;
                    newFp[fp.GetHash(i)] = count + fp.GetCount(i);
                }
            }
            List<int> keys = new List<int>(newFp.Keys);
            keys.Sort();
            hitHashes = new int[keys.Count];
            numOfHits = new int[keys.Count];
            {
                int i = 0;
                foreach (var key in keys)
                {
                    hitHashes[i] = key;
                    numOfHits[i++] = newFp[key];
                }
            }
        }


        public void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint) {
            this.behaveAsBitFingerprint = behaveAsBitFingerprint;
        }


        public bool HasHash(int hash) {
            return Array.BinarySearch(hitHashes, hash) >= 0;
        }


        public int GetCountForHash(int hash) {

            int index = Array.BinarySearch(hitHashes, hash);
            if (index >= 0) {
                return numOfHits[index];
            }
            return 0;
        }
    }
}
