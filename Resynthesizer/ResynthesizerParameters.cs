/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018 Nicholas Hayes
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

using PaintDotNet;

namespace ContentAwareFill
{
    internal sealed class ResynthesizerParameters
    {
        public ResynthesizerParameters(bool tileHorizontal, bool tileVertical, MatchContextType matchContext, double mapWeight, double sensitivityToOutliers,
             uint neighbors, uint trys)
        {
            TileHorizontal = tileHorizontal;
            TileVertical = tileVertical;
            MatchContext = matchContext;
            MapWeight = mapWeight.Clamp(0.0, ResynthesizerConstants.MaxWeight);
            SensitivityToOutliers = sensitivityToOutliers;
            Neighbors = neighbors > ResynthesizerConstants.MaxNeighbors ? ResynthesizerConstants.MaxNeighbors : neighbors;
            Trys = trys > ResynthesizerConstants.MaxTriesPerPixel ? ResynthesizerConstants.MaxTriesPerPixel : trys;
        }

        public bool TileHorizontal
        {
            get;
        }

        public bool TileVertical
        {
            get;
        }

        public MatchContextType MatchContext
        {
            get;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public double MapWeight
        {
            get;
        }

        public double SensitivityToOutliers
        {
            get;
        }

        public uint Neighbors
        {
            get;
        }

        public uint Trys
        {
            get;
        }
    }
}
