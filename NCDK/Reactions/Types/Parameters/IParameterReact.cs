/* Copyright (C) 2008  Miquel Rojas Cherto <miguelrojasch@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
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
namespace NCDK.Reactions.Types.Parameters
{
    /**
     * Interface for classes that generate parameters used in reactions.
     *
     * @author      miguelrojasch
     * @cdk.module  reaction
     * @cdk.githash
     */
    public interface IParameterReact
    {
        /// <summary>
        /// the parameter to take account.
        /// </summary>
        bool IsSetParameter { get; set; }

        /// <summary>
        /// the value of the parameter.
        /// </summary>
        object Value { get; set; }
    }
}

