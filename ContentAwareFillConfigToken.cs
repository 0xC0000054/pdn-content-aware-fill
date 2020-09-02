﻿/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020 Nicholas Hayes
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
*/

using PaintDotNet.Effects;

namespace ContentAwareFill
{
    public sealed class ContentAwareFillConfigToken : EffectConfigToken
    {
        public int SampleSize
        {
            get;
            set;
        }

        public SampleSource SampleFrom
        {
            get;
            set;
        }

        public FillDirection FillDirection
        {
            get;
            set;
        }

        public ContentAwareFillConfigToken(int sampleSize, SampleSource sampleFrom, FillDirection fillDirection)
        {
            SampleSize = sampleSize;
            SampleFrom = sampleFrom;
            FillDirection = fillDirection;
        }

        private ContentAwareFillConfigToken(ContentAwareFillConfigToken cloneMe)
        {
            SampleSize = cloneMe.SampleSize;
            SampleFrom = cloneMe.SampleFrom;
            FillDirection = cloneMe.FillDirection;
        }

        public override object Clone()
        {
            return new ContentAwareFillConfigToken(this);
        }
    }
}
