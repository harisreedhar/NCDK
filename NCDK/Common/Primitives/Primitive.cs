﻿/*
 * Copyright (C) 2008 The Guava Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF Any KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

namespace NCDK.Common.Primitives
{
    public static class Primitive<T> where T : IComparable
    {
        public static IComparer<T[]> LexicographicalComparator = new LexicographicalComparatorImpl<T>();

        private class LexicographicalComparatorImpl<TT> 
            : IComparer<TT[]> where TT : IComparable
        {
            public int Compare(TT[] left, TT[] right)
            {
                int minLength = Math.Min(left.Length, right.Length);
                for (int i = 0; i < minLength; i++)
                {
                    int result = left[i].CompareTo(right[i]);
                    if (result != 0)
                        return result;
                }
                return left.Length - right.Length;
            }
        }
    }
}

