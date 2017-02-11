/* Copyright (C) 2011  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Tools;
using System;

namespace NCDK.IO.Formats
{
    // @cdk.module ioformats
    // @cdk.githash
    // @cdk.set    io-formats
    public class MOPAC7InputFormat : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public MOPAC7InputFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new MOPAC7InputFormat();
                return myself;
            }
        }

        public override string FormatName => "MOPAC7 Input";
        public override string MIMEType => null;
        public override string PreferredNameExtension => null;
        public override string[] NameExtensions => Array.Empty<string>();
        public override string ReaderClassName => null;
        public override string WriterClassName => null;
        public override bool Matches(int lineNumber, string line) => false;
        public override bool IsXmlBased => false;
        public override int SupportedDataFeatures => DataFeatures.None;
        public override int RequiredDataFeatures => DataFeatures.None;
    }
}
