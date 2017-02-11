/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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
    public class AlchemyFormat : AbstractResourceFormat, IChemFormat
    {
        private static IResourceFormat myself = null;

        public AlchemyFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new AlchemyFormat();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "Alchemy";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-alchemy";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override string[] NameExtensions { get; } = new string[] { "alc" };

        /// <inheritdoc/>
        public string ReaderClassName => null;

        /// <inheritdoc/>
        public string WriterClassName => null;

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public int SupportedDataFeatures => DataFeatures.None;

        /// <inheritdoc/>
        public int RequiredDataFeatures => DataFeatures.None;
    }
}
