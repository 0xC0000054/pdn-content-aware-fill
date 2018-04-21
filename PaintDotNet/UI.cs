/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////


using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ContentAwareFill
{


    /// <summary>
    /// Contains static methods related to the user interface.
    /// </summary>
    internal static class UI
    {
        private static bool initScales = false;
        private static float xScale;

        private static void InitScaleFactors(Control c)
        {
            if (c == null)
            {
                xScale = 1.0f;
            }
            else
            {
                using (Graphics g = c.CreateGraphics())
                {
                    xScale = g.DpiX / 96.0f;
                }
            }

            initScales = true;
        }

        public static void InitScaling(Control c)
        {
            if (!initScales)
            {
                InitScaleFactors(c);
            }
        }

        private static VisualStyleClass DetermineVisualStyleClassImpl()
        {
            VisualStyleClass vsClass;

            try
            {
                if (!VisualStyleInformation.IsSupportedByOS)
                {
                    vsClass = VisualStyleClass.Classic;
                }
                else if (!VisualStyleInformation.IsEnabledByUser)
                {
                    vsClass = VisualStyleClass.Classic;
                }
                else if (0 == string.Compare(VisualStyleInformation.Author, "MSX", StringComparison.InvariantCulture) &&
                         0 == string.Compare(VisualStyleInformation.DisplayName, "Aero style", StringComparison.InvariantCulture))
                {
                    vsClass = VisualStyleClass.Aero;
                }
                else if (0 == string.Compare(VisualStyleInformation.Company, "Microsoft Corporation", StringComparison.InvariantCulture) &&
                         0 == string.Compare(VisualStyleInformation.Author, "Microsoft Design Team", StringComparison.InvariantCulture))
                {
                    if (0 == string.Compare(VisualStyleInformation.DisplayName, "Windows XP style", StringComparison.InvariantCulture) ||  // Luna
                        0 == string.Compare(VisualStyleInformation.DisplayName, "Zune Style", StringComparison.InvariantCulture) ||        // Zune
                        0 == string.Compare(VisualStyleInformation.DisplayName, "Media Center style", StringComparison.InvariantCulture))  // Royale
                    {
                        vsClass = VisualStyleClass.Luna;
                    }
                    else
                    {
                        vsClass = VisualStyleClass.Other;
                    }
                }
                else
                {
                    vsClass = VisualStyleClass.Other;
                }
            }
            catch (Exception)
            {
                vsClass = VisualStyleClass.Other;
            }

            return vsClass;
        }

        public static VisualStyleClass VisualStyleClass
        {
            get
            {
                return DetermineVisualStyleClassImpl();
            }
        }

        public static float GetXScaleFactor()
        {
            if (!initScales)
            {
                throw new InvalidOperationException("Must call InitScaling() first");
            }

            return xScale;
        }

        public static int ScaleWidth(int width)
        {
            return (int)Math.Round((float)width * GetXScaleFactor());
        }

    }
}
