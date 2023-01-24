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
*/

using PaintDotNet.Effects;
using PaintDotNet.Imaging;

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

        public bool RenderAutomatically
        {
            get;
            set;
        }

        public IBitmap<ColorBgra32> Output
        {
            get;
            set;
        }

        public ContentAwareFillConfigToken(int sampleSize,
                                           SampleSource sampleFrom,
                                           FillDirection fillDirection,
                                           bool renderAutomatically,
                                           IBitmap<ColorBgra32> output)
        {
            this.SampleSize = sampleSize;
            this.SampleFrom = sampleFrom;
            this.FillDirection = fillDirection;
            this.RenderAutomatically = renderAutomatically;
            this.Output = output;
        }

        private ContentAwareFillConfigToken(ContentAwareFillConfigToken cloneMe)
        {
            this.SampleSize = cloneMe.SampleSize;
            this.SampleFrom = cloneMe.SampleFrom;
            this.FillDirection = cloneMe.FillDirection;
            this.RenderAutomatically = cloneMe.RenderAutomatically;
            this.Output = cloneMe.Output;
        }

        public override object Clone()
        {
            return new ContentAwareFillConfigToken(this);
        }
    }
}
