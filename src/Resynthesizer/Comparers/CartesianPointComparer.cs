/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022, 2023 Nicholas Hayes
*
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*
*
* This file incorporates work covered by the following copyright and
* permission notice:
*
* The Resynthesizer - A GIMP plug-in for resynthesizing textures
*
*  Copyright (C) 2010  Lloyd Konneker
*  Copyright (C) 2000 2008  Paul Francis Harrison
*  Copyright (C) 2002  Laurent Despeyroux
*  Copyright (C) 2002  David Rodríguez García
*
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*
*/

using PaintDotNet.Rendering;

namespace ContentAwareFill
{
    internal sealed class CartesianPointComparer : PointComparer
    {
        private readonly bool moreCartesian;

        public CartesianPointComparer(bool moreCartesian)
        {
            this.moreCartesian = moreCartesian;
        }

        public override int Compare(Point2Int32 point1, Point2Int32 point2)
        {
            int point1Cartesian = (point1.X * point1.X) + (point1.Y * point1.Y);
            int point2Cartesian = (point2.X * point2.X) + (point2.Y * point2.Y);

            if (this.moreCartesian)
            {
                return point2Cartesian.CompareTo(point1Cartesian);
            }
            else
            {
                return point1Cartesian.CompareTo(point2Cartesian);
            }
        }
    }
}
